# GridOne / 行星统一电网

《戴森球计划》的Mod，将每颗星球的电力设施和用电建筑归入 1 号电网，清除逻辑连边与可见电线。

A mod for Dyson Sphere Program. Consolidates each planet's power nodes and electrical components into grid 1, removing logical connections and visible power lines.

<details>
<summary>Read me in English</summary>

Source code and issues: [GitHub](https://github.com/cat-yummi-dspmod/GridOne)
Install: r2modman

## Usage

1. Enable the mod. On a planet, build or remove a power node (power pole, solar panel, etc.) to trigger consolidation. Each planet is handled separately.
2. Consolidation pulls in existing consumers on that planet, including buildings that were not powered. After that, new consumers during the same loaded session join grid 1 without needing to be within node coverage.
3. The planet still needs at least one power node and enough generation. Removing the last node disconnects consumers.

After loading a save, consolidated membership is kept. Build or remove a power node on that planet again to re-enable distance-independent consumer handling for new buildings. Until then, new consumers follow vanilla coverage rules.

## Saves and vanilla compatibility

Save after consolidation. Membership uses the vanilla save format—no custom save fields and no enlarged coverage radii.

- After removing the mod, vanilla can load the save, restore grid 1, and power existing members.
- In vanilla, new consumers can join grid 1 if they are within the real coverage of grid 1's nodes.
- In vanilla, new power nodes may create wires or another network. Removing nodes can disconnect consumers and split the network.

Back up your save before first use. Multiplayer has not been tested.

## Mod compatibility

This mod only interacts with mods that change power or the power grid. Compatibility conflicts are possible.

Confirmed compatible with soarqin **Cheat Enabler**. GridOne can also relieve dismantle stutter caused by Cheat Enabler placing huge numbers of power poles.

## Performance and threading

For skip grid calculating, so much better performance then vanilla.

The first consolidation visits and sorts members and clears existing edges. Later changes update sorted lists on grid 1 directly, skipping proximity scans, wire layout, and connectivity walks. List insert/remove can still shift array elements.

Topology changes run synchronously at vanilla build/dismantle entry points. Power computation still uses the game's parallel scheduling. The mod adds no background tasks or extra power-tick locks.

## Performance testing

Because this mod was originally designed for laying out under extreme conditions to ease CPU load, so:

Tested with a blueprint made by TTenYX, which can product 60000 Universe Matrix per min from mineral on one planet.

Tested with a blueprint made by 难辞旧安, which can product 67200 Universe Matrix per min from mineral on one planet.

## Building from source

Install a .NET SDK targeting `net472`. Game APIs come from NuGet `DysonSphereProgram.GameLibs`; a local game install is not required.

```powershell
dotnet build -c Release
.\build_thunderstore.ps1
```

The Thunderstore package is written to `bin/Release/GridOne.zip` with `manifest.json`, `README.md`, `icon.png`, and `GridOne.dll` at the root.

## Contributing and license

Use [Conventional Commits](https://www.conventionalcommits.org/) (e.g. `fix: …`, `docs: …`, `build: …`). Bug reports should include game version, mod version, steps to reproduce, and relevant BepInEx logs.

MIT license. See `LICENSE`.

## Credits

- [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game
- [BepInEx](https://bepinex.dev/): Base modding framework

## Author

- ttrint
- cat yummi

</details>

<details>
<summary>中文说明</summary>

源码与问题反馈：[GitHub 仓库](https://github.com/cat-yummi-dspmod/GridOne)
安装: r2modman
## 使用

1. 安装并启用 Mod，在目标星球建造或拆除一个电力节点，例如电线杆或太阳能板，即可触发归并。每颗星球分别处理。
2. 归并会接入该星球已有的用电器，包括原本未接电的建筑。此后在本次加载期间新建的用电器也会加入电网 1，无需处于节点覆盖范围内。
3. 星球仍需至少一个电力节点和足够的发电能力。拆除最后一个节点会让用电器回到未接电状态。

读档后，归并结果会保留。再次建造或拆除一个电力节点，才会在该星球重新启用 Mod 的跨距离入网处理。在此之前，新建用电器遵循原版覆盖范围规则。

## 存档与原版兼容

归并后保存游戏。Mod 使用原版存档格式保存电网成员，不添加自定义存档字段，也不扩大供电半径。

- 卸载 Mod 后，原版读档能够恢复电网 1，现有成员仍可供电。
- 原版中新建的普通用电建筑，只要位于电网 1 节点的真实覆盖范围内，就能加入电网 1。
- 原版中新建电力节点可能生成连线或创建另一个电网。拆除电力节点可能导致用电器断电，并按连通性拆分电网。

首次使用前备份存档。多人联机兼容性尚未验证。

## Mod 兼容

本 Mod 只会与修改电力或电网的 Mod 产生交互，可能出现兼容冲突。

已确认与 soarqin的 **Cheat Enabler** 兼容；甚至可缓解因 Cheat Enabler 海量电线杆导致的拆除卡顿。

## 性能与线程

因为跳过了电网计算，性能比原版性能好的多。

首次归并需要遍历并排序电网成员、清除已有连线。归并后，新增节点或用电器直接更新电网 1 的有序成员列表，跳过节点间距离扫描、逐线布线和连通性遍历。成员列表插入和删除仍可能移动数组元素。

拓扑变更在原版建造与拆除入口同步执行。电力计算继续使用游戏的并行供电调度。Mod 不创建后台任务或额外的供电 Tick 锁。

## 性能测试

因为这个Mod最初就是为了极限工况下的铺设，缓解CPU计算压力，因此：

已经使用TTenYX的单球每分钟6万宇宙矩阵原矿黑盒蓝图进行验证。

已经使用难辞旧按的单球每分钟6.72万宇宙矩阵原矿黑盒蓝图进行验证。

## 从源码编译

准备 .NET SDK（`net472` 目标）即可编译；游戏 API 来自 NuGet `DysonSphereProgram.GameLibs`，无需本机游戏目录。

```powershell
dotnet build -c Release
.\build_thunderstore.ps1
```

Thunderstore 包输出为 `bin/Release/GridOne.zip`，根目录包含 `manifest.json`、`README.md`、`icon.png`、`GridOne.dll`。

## 贡献与许可

提交使用约定式提交格式，例如 `fix: preserve grid membership after node removal`、`docs: clarify vanilla save compatibility` 或 `build: update Thunderstore package metadata`。报告问题时附上游戏版本、Mod 版本、复现步骤和相关 BepInEx 日志。

MIT 许可。见 `LICENSE`。

## 鸣谢

- [戴森球计划](https://store.steampowered.com/app/1366540)：伟大的游戏
- [BepInEx](https://bepinex.dev/)：基础模组框架

## 制作人

- ttrint
- 流浪法师.悠米

</details>
