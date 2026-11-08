# GridOne / 行星统一电网

《戴森球计划》的 BepInEx 5 + Harmony Mod，将每颗星球的电力设施和用电建筑归入 1 号电网，清除逻辑连边与可见电线。

A BepInEx 5 + Harmony mod for Dyson Sphere Program. Consolidates each planet's power nodes and electrical components into grid 1, removing logical connections and visible power lines.

源码与问题反馈见 [GitHub 仓库](https://github.com/tripleInt/GridOne)。

Source code and issue tracking are available on [GitHub](https://github.com/tripleInt/GridOne).

## 使用 / Usage

1. 安装并启用 Mod，在目标星球建造或拆除一个电力节点，例如电线杆或太阳能板，即可触发归并。每颗星球分别处理。
2. 归并会接入该星球已有的用电器，包括原本未接电的建筑。此后在本次加载期间新建的用电器也会加入电网 1，无需处于节点覆盖范围内。
3. 星球仍需至少一个电力节点和足够的发电能力。拆除最后一个节点会让用电器回到未接电状态。

Build or remove a power node, such as a power pole or solar panel, to consolidate that planet. Existing consumers, including unpowered buildings, join grid 1. Subsequent consumers join regardless of distance during that loaded session. At least one power node and sufficient generation are still required. Removing the last node disconnects the consumers.

读档后，归并结果会保留。再次建造或拆除一个电力节点，才会在该星球重新启用 Mod 的跨距离入网处理。在此之前，新建用电器遵循原版覆盖范围规则。

Saved membership survives loading. After each load, build or remove a power node on the planet to reactivate the mod's distance-independent consumer handling. Until then, new consumers use vanilla coverage rules.

## 安装 / Installation

- **Thunderstore / r2modman** — 本地测试时，通过设置中的 **Import local mod** 导入 `bin/Release/GridOne.zip`。使用的配置需已安装 `xiaoye97-BepInEx-5.4.17` 或兼容的 BepInEx 5。依赖已声明在 `manifest.json` 中。
- **手动安装** — 关闭游戏，将包内 `BepInEx/plugins/GridOne/GridOne.dll` 放入实际启动配置对应的同一路径。

For local testing, import the ZIP through **Import local mod** in the mod manager settings and ensure the profile already has BepInEx 5 installed. For manual installation, close the game and copy the DLL into the active installation or profile's `BepInEx/plugins/GridOne/` directory.

从旧名称 SinglePowerGrid 升级时，先卸载旧包或删除旧的 `BepInEx/plugins/SinglePowerGrid/SinglePowerGrid.dll`，再安装 GridOne，避免同时加载两个版本。

When upgrading from SinglePowerGrid, uninstall the old package or remove `BepInEx/plugins/SinglePowerGrid/SinglePowerGrid.dll` before installing GridOne to avoid loading both versions.

卸载时关闭游戏，在管理器中移除本 Mod，或删除手动安装的 DLL。存档行为见下一节。

To uninstall, close the game and remove the mod through the manager or delete the manually installed DLL. Saved-grid behavior is described below.

## 存档与原版兼容 / Saves and vanilla compatibility

归并后保存游戏。Mod 使用原版存档格式保存电网成员，不添加自定义存档字段，也不扩大供电半径。

Save after consolidation. Membership uses the vanilla save format without custom save fields or enlarged coverage radii.

- 卸载 Mod 后，原版读档能够恢复电网 1，现有成员仍可供电。
- 原版中新建的普通用电建筑，只要位于电网 1 节点的真实覆盖范围内，就能加入电网 1。
- 原版中新建电力节点可能生成连线或创建另一个电网。拆除电力节点可能导致用电器断电，并按连通性拆分电网。

Vanilla can restore the saved grid and power its existing members. New vanilla consumers can join within the actual coverage of grid 1's nodes. Adding power nodes in vanilla may generate wires or another network. Removing power nodes can disconnect consumers and split the network.

首次使用前备份存档。修改相同电网拓扑方法的其他 Mod 可能发生冲突。多人联机兼容性尚未验证。

Back up the save before first use. Other mods that replace the same topology methods may conflict. Multiplayer compatibility has not been verified.

## 性能与线程 / Performance and threading

首次归并需要遍历并排序电网成员、清除已有连线。归并后，新增节点或用电器直接更新电网 1 的有序成员列表，跳过节点间距离扫描、逐线布线和连通性遍历。成员列表插入和删除仍可能移动数组元素。

The first consolidation visits and sorts the members and clears existing edges. Subsequent changes update sorted member lists directly, skipping proximity scans, wire arrangement, and connectivity traversal. List insertion and removal can still shift elements.

拓扑变更在原版建造与拆除入口同步执行。电力计算继续使用游戏的并行供电调度。Mod 不创建后台任务或额外的供电 Tick 锁。

Topology changes execute synchronously at vanilla construction and removal entry points. Power computation uses the game's existing parallel scheduling. The mod creates no background tasks or additional power-tick locks.

## 从源码编译 / Building from source

准备 .NET SDK（`net472` 目标）即可编译；游戏 API 来自 NuGet `DysonSphereProgram.GameLibs`，无需本机游戏目录。

Install a .NET SDK and run `dotnet build` or `.\build_thunderstore.ps1`. Game assemblies come from the BepInEx NuGet feed, not a local install.

```powershell
dotnet build -c Release
.\build_thunderstore.ps1
```

Thunderstore 包输出为 `bin/Release/GridOne.zip`，根目录包含 `manifest.json`、`README.md`、`icon.png`、`GridOne.dll`。

## 验证范围 / Validation

已完成本机程序集编译及独立冒烟检查，覆盖 Harmony 补丁加载、电网 1 槽位回收、成员增删、存档写入与读回，以及原版覆盖范围内的用电器入网。存档检查使用构造的电力系统数据，并替代了需要 Unity 环境的物品资源加载。

Compilation and a standalone smoke check covered Harmony patch registration, reuse of grid slot 1, member changes, save round trips, and vanilla in-range consumer assignment. The save check used constructed power-system data and substituted Unity-dependent item resource loading.

尚未完成 Unity 游戏内运行、多线程压力场景或帧耗时验证。

In-game operation, multithreaded stress scenarios, and frame-time performance have not been validated.

## 贡献与许可 / Contributing and license

提交使用约定式提交格式，例如 `fix: preserve grid membership after node removal`、`docs: clarify vanilla save compatibility` 或 `build: update Thunderstore package metadata`。报告问题时附上游戏版本、Mod 版本、复现步骤和相关 BepInEx 日志。

Use Conventional Commits, such as `fix: ...`, `docs: ...`, or `build: ...`. Include the game version, mod version, reproduction steps, and relevant BepInEx logs in bug reports.

MIT license. See `LICENSE`.
