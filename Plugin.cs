using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using PowerNetworkStructures;

namespace GridOne
{
    [BepInPlugin("dspdev.gridone", "GridOne", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private void Awake() => new Harmony("dspdev.gridone").PatchAll(typeof(Plugin).Assembly);
    }

    [HarmonyPatch]
    internal static class GridPatches
    {
        // Topology edits run in the game's construction/removal phase. Power workers
        // keep using the vanilla tick, with no plugin locks or background mutations.
        private static readonly ConditionalWeakTable<PowerSystem, GridState> States =
            new ConditionalWeakTable<PowerSystem, GridState>();

        private sealed class GridState
        {
            public Node ConsumerOwner;
        }

        private static readonly IComparer<Node> NodeOrder =
            Comparer<Node>.Create((left, right) => left.id.CompareTo(right.id));

        [HarmonyPrefix, HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.OnNodeAdded))]
        private static bool AddNode(PowerSystem __instance, int nodeId,
            int[] ___netRecycle, int ___netRecycleCursor)
        {
            PowerSystem system = __instance;
            GridState state = States.GetValue(system, _ => new GridState());
            PowerNetwork grid = EnsureGrid(system, state, ___netRecycle, ___netRecycleCursor);

            PowerNodeComponent component = system.nodePool[nodeId];
            EntityData entity = system.factory.entityPool[component.entityId];
            var position = entity.pos / (entity.alt / (system.factory.planet.realRadius + 0.2f));
            var node = new Node(nodeId)
            {
                x = position.x,
                y = position.y,
                z = position.z,
                connDistance2 = component.connectDistance * component.connectDistance,
                coverRadius2 = component.coverRadius * component.coverRadius,
                genId = entity.powerGenId,
                accId = entity.powerAccId,
                excId = entity.powerExcId
            };

            grid.nodes.Insert(~grid.nodes.BinarySearch(node, NodeOrder), node);
            system.nodePool[nodeId].networkId = 1;
            if (node.genId > 0)
            {
                Insert(grid.generators, node.genId);
                system.genPool[node.genId].networkId = 1;
            }
            if (node.accId > 0)
            {
                Insert(grid.accumulators, node.accId);
                system.accPool[node.accId].networkId = 1;
            }
            if (node.excId > 0)
            {
                Insert(grid.exchangers, node.excId);
                system.excPool[node.excId].networkId = 1;
            }
            if (state.ConsumerOwner == null)
            {
                state.ConsumerOwner = node;
                node.consumers.AddRange(grid.consumers);
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.OnConsumerAdded))]
        private static bool AddConsumer(PowerSystem __instance, int consumerId)
        {
            if (!States.TryGetValue(__instance, out GridState state) || state.ConsumerOwner == null)
                return true;

            Insert(__instance.netPool[1].consumers, consumerId);
            Insert(state.ConsumerOwner.consumers, consumerId);
            __instance.consumerPool[consumerId].networkId = 1;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.OnConsumerRemoving))]
        private static bool RemoveConsumer(PowerSystem __instance, int consumerId)
        {
            if (!States.TryGetValue(__instance, out GridState state) || state.ConsumerOwner == null)
                return true;

            Remove(__instance.netPool[1].consumers, consumerId);
            Remove(state.ConsumerOwner.consumers, consumerId);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.OnNodeRemoving))]
        private static bool RemoveNode(PowerSystem __instance, int nodeId,
            int[] ___netRecycle, int ___netRecycleCursor)
        {
            PowerSystem system = __instance;
            GridState state = States.GetValue(system, _ => new GridState());
            PowerNetwork grid = EnsureGrid(system, state, ___netRecycle, ___netRecycleCursor);
            int index = grid.nodes.BinarySearch(new Node { id = nodeId }, NodeOrder);
            Node node = grid.nodes[index];
            grid.nodes.RemoveAt(index);
            Remove(grid.generators, node.genId);
            Remove(grid.accumulators, node.accId);
            Remove(grid.exchangers, node.excId);

            if (grid.nodes.Count == 0)
            {
                List<int> unpowered = system.netPool[0].consumers;
                foreach (int consumerId in grid.consumers)
                    system.consumerPool[consumerId].networkId = 0;
                unpowered.AddRange(grid.consumers);
                unpowered.Sort();
                state.ConsumerOwner = null;
                system.RemoveNetwork(1);
            }
            else if (ReferenceEquals(state.ConsumerOwner, node))
            {
                state.ConsumerOwner = grid.nodes[0];
                state.ConsumerOwner.consumers = node.consumers;
            }
            return false;
        }

        private static PowerNetwork EnsureGrid(PowerSystem system, GridState state,
            int[] recycled, int recycleCount)
        {
            if (state.ConsumerOwner != null)
                return system.netPool[1];

            if (system.netCursor == 1)
            {
                system.NewNetwork();
            }
            else if (system.netPool[1]?.id != 1)
            {
                // Move slot 1 to the top of vanilla's free-slot stack before allocating.
                int slot = Array.IndexOf(recycled, 1, 0, recycleCount);
                if (slot < 0)
                    throw new InvalidOperationException("Power network slot 1 is missing from the recycle stack.");
                recycled[slot] = recycled[recycleCount - 1];
                recycled[recycleCount - 1] = 1;
                system.NewNetwork();
            }

            PowerNetwork grid = system.netPool[1];
            grid.consumers.AddRange(system.netPool[0].consumers);
            system.netPool[0].consumers.Clear();

            for (int id = 2; id < system.netCursor; id++)
            {
                PowerNetwork other = system.netPool[id];
                if (other == null || other.id != id)
                    continue;

                grid.nodes.AddRange(other.nodes);
                grid.consumers.AddRange(other.consumers);
                grid.generators.AddRange(other.generators);
                grid.accumulators.AddRange(other.accumulators);
                grid.exchangers.AddRange(other.exchangers);
                system.RemoveNetwork(id);
            }

            grid.nodes.Sort(NodeOrder);
            grid.consumers.Sort();
            grid.generators.Sort();
            grid.accumulators.Sort();
            grid.exchangers.Sort();

            foreach (Node node in grid.nodes)
            {
                node.conns.Clear();
                node.lines.Clear();
                node.consumers.Clear();
                system.nodePool[node.id].networkId = 1;
            }
            foreach (int id in grid.consumers)
                system.consumerPool[id].networkId = 1;
            foreach (int id in grid.generators)
                system.genPool[id].networkId = 1;
            foreach (int id in grid.accumulators)
                system.accPool[id].networkId = 1;
            foreach (int id in grid.exchangers)
                system.excPool[id].networkId = 1;

            if (grid.nodes.Count > 0)
            {
                state.ConsumerOwner = grid.nodes[0];
                state.ConsumerOwner.consumers.AddRange(grid.consumers);
            }
            return grid;
        }

        private static void Insert(List<int> members, int id)
        {
            int index = members.BinarySearch(id);
            if (index < 0)
                members.Insert(~index, id);
        }

        private static void Remove(List<int> members, int id)
        {
            int index = members.BinarySearch(id);
            if (index >= 0)
                members.RemoveAt(index);
        }
    }
}
