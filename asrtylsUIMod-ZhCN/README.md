# astryl UI 模组合集 简体中文汉化

将作者 [**astryl**](https://steamcommunity.com/profiles/76561198052137536) 的 18 个 UI / 工具模组聚合汉化为简体中文，一个模组搞定全部。

## 覆盖模组

| 模组 | 说明 | Steam 工坊 |
|------|------|-----------|
| Modern Pawn Tabs | 角色标签页 | [3740575493](https://steamcommunity.com/sharedfiles/filedetails/?id=3740575493) |
| Modern Bio Tab | 生物标签页 | [3740688691](https://steamcommunity.com/sharedfiles/filedetails/?id=3740688691) |
| Modern Xenotype Tab | 异种人标签页 | [3740688962](https://steamcommunity.com/sharedfiles/filedetails/?id=3740688962) |
| Modern Social Tab | 社交标签页 | [3740700588](https://steamcommunity.com/sharedfiles/filedetails/?id=3740700588) |
| Modern Quest Menu | 任务菜单 | [3742906203](https://steamcommunity.com/sharedfiles/filedetails/?id=3742906203) |
| Modern History Menu | 历史菜单 | [3742925193](https://steamcommunity.com/sharedfiles/filedetails/?id=3742925193) |
| Modern Faction Menu | 派系菜单 | [3742926690](https://steamcommunity.com/sharedfiles/filedetails/?id=3742926690) |
| Modern Ideology Menu | 意识形态菜单 | [3743026027](https://steamcommunity.com/sharedfiles/filedetails/?id=3743026027) |
| Modern Needs Tab | 需求标签页 | [3743615382](https://steamcommunity.com/sharedfiles/filedetails/?id=3743615382) |
| Modern Learning Menu | 学习菜单 | [3743640778](https://steamcommunity.com/sharedfiles/filedetails/?id=3743640778) |
| Modern Notifications | 通知 | [3752932665](https://steamcommunity.com/sharedfiles/filedetails/?id=3752932665) |
| Modern CC | 角色创建 | [3762126187](https://steamcommunity.com/sharedfiles/filedetails/?id=3762126187) |
| Modern Colonist Bar | 殖民者栏 | [3766830289](https://steamcommunity.com/sharedfiles/filedetails/?id=3766830289) |
| Modern Dev Tools | 开发者工具 | [3771602203](https://steamcommunity.com/sharedfiles/filedetails/?id=3771602203) |
| Pillar Planner | 屋顶支撑规划 | [3768069893](https://steamcommunity.com/sharedfiles/filedetails/?id=3768069893) |
| Circinus | 模组性能分析 | [3773680130](https://steamcommunity.com/sharedfiles/filedetails/?id=3773680130) |
| True RPG Inventory | 俄罗斯方块背包系统 | [3744201621](https://steamcommunity.com/sharedfiles/filedetails/?id=3744201621) |
| True RPG Backpacks | 可开启的密封背包 | [3744208438](https://steamcommunity.com/sharedfiles/filedetails/?id=3744208438) |

## 翻译方式

1. **语言文件** — 使用 RimWorld 内置本地化系统，汉化各模组的界面文本与 Def（`Languages/ChineseSimplified/`）
2. **运行时补丁** — 对硬编码在 DLL 中的文本（如 Modern Social Tab、Modern Learning Menu、Modern CC、Circinus、Modern Colonist Bar），通过 Harmony Transpiler 直接替换为中文字面量（翻译内嵌补丁 DLL，不读语言文件），不修改原模组文件

## 设置聚合

为避免「选项 → Mod 设置」列表被十几个条目刷屏，本汉化隐藏了 18 个 UI 模组的设置条目，改为在 Mod 设置中只显示一个「astryl UI 模组合集」入口，点进去即可分开打开任意已安装模组的设置界面（未安装的不会显示）。

## 缺汉化反馈

本汉化已覆盖所有已支持模组的界面文本与硬编码字符串。若仍发现未汉化的英文界面，请截图（包含整块界面，便于定位）后反馈；不截图则无法确定具体位置，不予处理。

## 依赖

- 上述 18 个原模组（可选择性安装，装了哪个就汉化哪个）
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)（运行时补丁需要）

## 安装

1. 将本模组放入 `RimWorld/Mods/` 目录
2. 在游戏中启用（置于原模组之后加载）

## 构建与部署

运行 `build.bat` 自动部署到游戏 `Mods` 目录（编译 → 产物回流 `Assemblies/` → 部署，自动化可用 `cmd /c "build.bat < NUL"`）。

## 目录结构

| 目录 | 说明 |
|------|------|
| `About/` | 模组元数据（About.xml、工坊 ID） |
| `Languages/ChineseSimplified/` | 汉化文本（`Keyed/` 界面字符串 + `DefInjected/` Def 注入） |
| `Assemblies/` | 硬编码补丁 DLL（构建产物） |
| `Source/AstrylsUIZhCN/` | 补丁源码（Harmony Transpiler + 聚合设置界面） |
| `OriginalMods/` | 16 个原模组完整副本（本地提取参考，不入库） |
| `OriginalSRC-SDK/` | 16 个反编译源码（本地参考，不入库） |
| `_AIcoding/` | AI 协作笔记与调研中间产物 |

## 链接

- [GitHub 源码](https://github.com/QianYI-Yan/RimworldMods/tree/main/asrtylsUIMod-ZhCN)
