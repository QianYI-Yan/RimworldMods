# asrtylsUIMod-ZhCN — 项目笔记（AI 必读）

> 作者 astryls 的 UI 模组聚合简体中文汉化项目
> 处理本模组前必须先读此文件

## 项目记忆

- 汉化模组 packageId: `yintx.deepseek.astrylUImod.zhcn`（用户已确认，2026-08-02）
- 目录名按用户指定拼写 `asrtylsUIMod-ZhCN`（非 astryls）
- 聚合范围 18 个 UI/工具模组（2026-08-02 新增 True RPG Inventory/Backpacks 汉化）；排除 TailorMade、As Above So Below
- 汉化双手段：**Keyed 翻译 + Harmony patch DLL**（Transpiler 替换 ldstr）
- **硬编码汉化采用「Keyed + Translator」维护模式**（2026-08-02）：翻译文本放 `Keyed/HardcodedZhCN.xml`，C# 只维护「英文原文 → 键名」稳定映射；改翻译只改 XML 无需重编译 DLL
- 参考模板：工作区 `_templates/generic-zhcn-template/`（通用）、`_templates/special-hardcoded-patch/`（硬编码 patch）
- 反编译工具：`_tools/dnspyEx/dnSpy.Console.exe`（用法见 `.github/skills/dnspy-tool/SKILL.md`）
- 构建框架：net472 + Krafs.Rimworld.Ref 1.6.4871 + Lib.Harmony 2.3.6
- **构建部署统一用 `build.bat`**（编译 → 产物回流项目根 `Assemblies/` → 部署游戏 Mods），交互式运行会停在末尾 pause，用 `cmd /c "build.bat < NUL"` 可自动通过（2026-08-02 修复：之前 build.bat 未回流编译产物，直接跑会部署旧 DLL）

## ⚠️ 缺模组适配要求（重要）

聚合汉化覆盖 16 个模组，玩家可能只装其中一部分，**必须保证缺模组不报错**：

1. **About.xml**：用 `loadAfter` 列全部模组（缺失只影响排序，不报错）；**不要**用 `modDependencies` 强制依赖全部（否则缺一个整个汉化被禁用）——已确认当前 About.xml 符合
2. **硬编码 patch**：用专项模板 `special-hardcoded-patch` 的 `PatchIfPresent(harmony, 模组packageId, 类型全名, 方法名)` 手动注册，未激活 / 类型不存在 / 方法不存在时静默跳过——模板已更新支持
3. **Keyed / DefInjected**：RimWorld 语言系统本身安全（键缺失只显示键名，def 注入缺失被忽略），无需特殊处理

## 目录结构
- `About/` — 汉化模组元数据（About.xml + PublishedFileId.txt，工坊 ID 发布后填写）
- `Languages/ChineseSimplified/` — 汉化文本（`Keyed/` 15 文件含 HardcodedZhCN + `DefInjected/` 8 类）
- `Assemblies/` — 硬编码补丁 DLL（构建产物，由 build.bat 回流更新）
- `Source/AstrylsUIZhCN/` — 补丁源码（Patcher.cs + HardcodedStringReplacer.cs + AstrylsUIZhCNMod.cs）
- `OriginalMods/` — 16 个原模组完整副本（提取源/参考，**git 忽略不入库**）
- `OriginalSRC-SDK/` — 16 个反编译源码项目（含 .csproj + solution.sln，**git 忽略不入库**）
- `_AIcoding/` — 本笔记 + 调研扫描结果（以 `_` 开头的中间产物，允许入库，体积小）

## 模组清单（聚合范围：18 个）

### Modern 系列（14 个）
| 模组 | packageId | Keyed 条目 | 硬编码 UI 情况 |
|---|---|---|---|
| Modern Pawn Tabs | astryl.ModernPawnTabs | 10 | 少（1 处：Locked...） |
| Modern Bio Tab | astryl.ModernBioTab | 52 | 较多（Mostly Het./Layout menu 等） |
| Modern Xenotype Tab | astryl.ModernXenotypeTab | 169 | 少（4 处） |
| Modern Social Tab | astryl.ModernSocialTab | **无 Keyed** | **严重（50+ 处，全硬编码）** |
| Modern Quest Menu | astryl.ModernQuestMenu | 84 | 少（2 处：Pin/Unpin quest） |
| Modern History Menu | astryl.ModernHistoryMenu | 100 | 少（1 处） |
| Modern Faction Menu | astryl.ModernFactionMenu | 32 | 较多（No settlements./Sort by/Pin to top 等） |
| Modern Ideology Menu | astryl.ModernIdeologyMenu | 97 | 少（1 处） |
| Modern Needs Tab | astryl.ModernNeedsTab | 87 | 无 |
| Modern Learning Menu | astryl.ModernLearningMenu | **无 Keyed** | **严重（50+ 处，全硬编码 + Defs）** |
| Modern Notifications | astryl.ModernNotifications | 325 | 几乎无（DevTools 测试文本为主） |
| Modern CC | astryl.ModernCC | 711 | 少（Unnamed pawn 等 5 处） |
| Modern Colonist Bar | astryl.ModernColonistBar | 216 | 较多（Bar controls/Targeted by/In transit. 等） |
| Modern Dev Tools | astryl.ModernDevTools | 340 | 几乎无（多为日志前缀） |

### 工具类（2 个）
| 模组 | packageId | Keyed 条目 | 硬编码 UI 情况 |
|---|---|---|---|
| Pillar Planner | astryl.PillarPlanner | 4 | 无 |
| Circinus | astryl.Circinus | 72 | **较多（Known issue/Present in % of runs 等 + DebugActions）** |

### True RPG 系列（2 个，2026-08-02 新增）
| 模组 | packageId | Keyed 条目 | 备注 |
|---|---|---|---|
| True RPG Inventory | astryl.TrueRPGInventory | 126 | 俄罗斯方块背包系统；Defs：JobDef×4 + WorkGiverDef×1 + KeyBinding×2 |
| True RPG Backpacks | astryl.TrueRPGBackpacks | 12 | 密封背包；Defs：ThingDef×1 |

## 排除的模组
- TailorMade — 已有 `TailorMade-ZhCN` 项目
- As Above So Below — 已有 `AsAboveSoBelow-ZhCN` 项目

## 关键结论

1. **汉化需要两种手段并存**：
   - `Languages/ChineseSimplified/`：Keyed XML 汉化（覆盖有语言文件的模组）
   - **Harmony patch DLL**：替换硬编码 UI 字符串（ModernSocialTab、ModernLearningMenu 最严重，无 Keyed 文件）

2. **ModernSocialTab 硬编码重点**（反编译 `SocialTabDrawer.cs` / `ModernSocialTabMod.cs`）：
   - 设置项：Appearance / Relation list / Social stats / Performance / Record standing history / Reset to defaults / Panel width / Sample interval / History kept / List refresh
   - 标签页内：Opinion / Relation / Standing / Recent interactions / Social stats / Your opinion of them / Their opinion of you / Pin to the top / Unpin / Break up with / Switch to Modern Social Tab / Switch to vanilla Social tab

3. **ModernLearningMenu 硬编码重点**（反编译 `DashboardDrawer.cs`）：
   - 面板标题：Colony Skills / Child Development / Growth Moments / Learning Summary / VSE Expertise / Active Training
   - 大量面板说明文字 + 提示语（Layout locked / Requires Biotech DLC / No colonists on map 等）

4. **ModernBioTab / ModernColonistBar / ModernFactionMenu / Circinus** 有中等量硬编码，Keyed + patch 结合

## 设置聚合界面（2026-08-02 新增）

用户需求：「把 16 个 UI 模组的设置界面隐藏掉，单独开一个界面分开打开这些 UI 模组」。

- **实现文件**：`Source/AstrylsUIZhCN/AstrylsUIZhCNMod.cs`（新）+ `Patcher.cs`（新增隐藏 patch）
- **聚合入口**：汉化模组 `AstrylsUIZhCNMod : Verse.Mod`，`SettingsCategory()` 返回「astryl UI 模组合集」；`DoSettingsWindowContents` 遍历 `LoadedModManager.ModHandles`，按 16 个 packageId（`AstrylsUIZhCNMod.AggregatedModNames` 字典）筛选已安装模组，点击 `Find.WindowStack.Add(new Dialog_ModSettings(mod))` 打开单个设置
- **隐藏原条目**：patch `RimWorld.Dialog_Options.PostOpen` Postfix，把 private 字段 `cachedModsWithSettings` 过滤掉 `IsAggregated` 的模组，使「选项 → Mod 设置」列表只显示汉化模组一个条目
- **关键源码结论**（RimWorld 1.6）：
  - `Dialog_ModSettings(Mod mod)` 直接可用，构造只需 Mod，`PreClose` 自动 `mod.WriteSettings()`
  - `Dialog_Options.PostOpen()` 用 `LoadedModManager.ModHandles` 构建 `cachedModsWithSettings`（`IEnumerable<Mod>`，private 字段）
  - `mod.Content.PackageId` 获取模组 ID；`Mod.SettingsCategory()` / `DoSettingsWindowContents(Rect)` / `WriteSettings()` 为虚方法
- 兼容性：Dialog_Options 是游戏本体类，无需 PatchIfPresent 存在性检查（但仍判空）
- 汉化模组自己的设置条目会正常出现在 Mod 设置列表（SettingsCategory 非空）
- 玩家仍可从模组管理页（Page_ModsConfig）进单个模组设置，未封堵
- **⚠️ Bug 修复（2026-08-02）**：聚合列表空白——根因 `AggregatedModNames` 字典用大写 packageId（`astryl.ModernBioTab`），而 RimWorld 内部（`ModsConfig.xml` / `ModContentPack.PackageId`）统一**小写**存储（`astryl.modernbiotab`）→ `TryGetValue` 永远不匹配 → 列表空白。修复：字典 key 改小写 + 匹配处 `pid.ToLowerInvariant()`（`IsAggregated` 隐藏逻辑同步修复）。**教训：RimWorld packageId 一律小写，比对必须 ToLowerInvariant**

## Modern Colonist Bar 硬编码补充（2026-08-02）

用户实测反馈：命令中心菜单（View:/Auto-hide 等）、悬停按钮（Draft/Jump/Health 等）仍显示英文。已补充：

- **反编译注意**：`OriginalSRC-SDK/ModernColonistBar` 的反编译结果**搜不到**这些字符串（可能版本过旧/不完整），本次以重新反编译 `OriginalMods/ModernColonistBar/Assemblies/ModernColonistBar.dll`（→ `D:\temp\mcb_decompiled`）为准
- **新增 11 个方法 patch**（命名空间 `ModernColonistBar`）：
  - `BarControls.Draw`（控制按钮 tooltip）/ `OpenMenu`（命令中心菜单）/ `BuildViewMenu`（Everyone/Squad:）
  - `Patch_HandleClicks_RightClickMenu.OpenInteractionMenu`（右键菜单 Command center.../Modern Colonist Bar）/ `BuildViewMenu`
  - `FloatMenuOptionSub` 构造函数（右键子菜单 tooltip）—— **PatchIfPresent 新增 `.ctor` 支持**（AccessTools.Constructor）
  - `HoverPopout.BuildActions`（Draft/Undraft/Jump/Health/Bio/Social/Gear/Rename 按钮）
  - `BarSquads.ViewLabel`（Everyone/Squad:）/ `WarbandHotbar.Draw`（Draft/Undraft/Selected）/ `Module_Loadouts.Label`（属性 getter，Gear）
- **新增 25 条 Keyed**：`HardcodedZhCN.ColonistBar.*`
- **重要经验（Transpiler）**：替换的 ldstr 若恰是分支/switch 跳转目标，必须把原指令 labels 转移到新指令（`newLoad.labels.AddRange(code.labels)`），否则报 `Label #N is not marked in method`（SortModeLabel getter 曾触发）
- **第二次补充（状态显示/右键菜单/小队管理/跟随镜头）**：全量反编译扫描（`D:\temp\mcb_strings.txt` 633 条，UI 过滤 309 条）后补充：
  - 新增 15 个方法：`AwayIndicator.LocationTip` / `AggroRadar.Draw` / `PawnStatusUtil.Gather` / `Patch_DrawColonist_Overlay.DrawWeaponIcon+DrawBpDevice+MedicalTooltip` / `FollowCam.OnGUI+Toggle+Stop` / `Dialog_ManageSquads.DoWindowContents` / `Dialog_RenameSquad.DoWindowContents` / `BarSquads.NameOfHidden` / `Patch_HandleClicks_RightClickMenu.BuildModMenu+BuildPoliciesMenu+BuildBarManageMenu`
  - 新增约 70 条 Keyed（`HardcodedZhCN.ColonistBar.*`）：离队/位置状态（In transit/With caravan/On another map）、仇恨/状态徽章（Targeted by/Break/Inspired/Danger sense/Engagement）、装备/医疗状态（in hand/sidearm/Forming caravan/Medical rest/Blood pressure/Bleeding/Needs treatment）、跟随镜头（Following/Select a pawn/Stopped following）、小队管理对话框（Bar squads/New squad/Rename or recolor）、条控制（隐藏囚犯/奴隶区域/New view/Rename/Delete）、右键菜单（Bar mode/Bar view/Policies/Outfit/Food/Drugs/Unhide all 等）
  - `BarControls.Draw` / `BuildViewMenu` 为已注册方法，直接补充字典即可
- **尚未补充（大界面，待确认）**：`Dialog_CommandCenter`（指挥中心界面 ~40 条）、`Module_Loadouts`/`Module_Armory`（装备/军械库 ~40 条）、`Module_Overview`/`Module_Stats`/`MCBStats`（统计模块 ~40 条）、`Warband`（战斗条）、`CCOrders`（订单禁用原因）、`DestinationGhost` 等
- **第三次补充（全部大界面完成，2026-08-02）**：
  - 新增约 50 个方法：`Dialog_CommandCenter`（DoWindowContents/DrawRoster/DrawSquadHeader/SubLine/DrawDetail/DrawPillar/DpsChip/DrawActionBar/PolicyBtn/OpenAssignMenu/OpenSortMenu/OpenSquadMenu）、`Module_Loadouts`（Draw/DrawGearPanel/DrawMapList/OpenMapRowMenu/OrderGear/DrawSlot/DrawManage/DrawReqRow/KitToOutfit/QueueBills/Snapshot）、`Module_Armory`（Draw/OpenRowMenu/WhereLabel）、`Module_Overview`（Draw/DrawRows/DrawGraph）、`Module_Stats`（Draw/OpenColumnsMenu/BuildAddMenu）、`MCBStats`（ReadinessTip/TechName/EffectiveDps/RangedEffDps/Resolve/RangedDpsTip/KeyLabel/StatMenu）、`Warband`（DrawCell/DrawBelt/BeltLabel/UseBeltItem/DrawPips/IsChampion）、`CCOrders.Blocker`
  - 新增约 170 条 Keyed（`HardcodedZhCN.ColonistBar.*`）：指挥中心（搜索/排序/小队/护甲/DPS/政策）、装备模块（套装/制作清单/装备到）、军械库（列头/武器）、概览（战斗准备度/属性/副武器）、统计、战斗统计 tooltip（MCBStats）、战斗条（腰带/治疗/领袖）、订单禁用原因（CCOrders）
  - **排除项**：内部 key（`__health__` 等）、纯符号（`: `、`▾` 等）、模块名（Armory/Combat/Stats）、DPS 等通用缩写
  - **验证**：字典 389 键 ↔ XML 389 键完全对应，无缺键；编译 0 错误

## True RPG 汉化 + 模组更新检查（2026-08-02）

用户要求新增 True RPG 汉化，并检查部分模组更新后的缺译：

- **全量对比**：遍历 18 个模组创意工坊英文 Keyed（2435 key）vs 已汉化（2285 key）→ 缺 150 个
  - True RPG 新模组：138 个（MRPG_*/NITRPG_*/RPGBP_*）
  - Modern Colonist Bar 更新新增 3 个（`MCB_Setting_FaRate*` 面部动画刷新频率）
  - Circinus 更新新增 9 个（`Circ.AutoProfile*` / `Circ.Settings.AutoProfile` / `Background` 自动分析）
- **新增文件**：`Keyed/TrueRPGInventory.xml`（126 条）+ `Keyed/TrueRPGBackpack.xml`（12 条）+ DefInjected 5 个（JobDef/WorkGiverDef/KeyBindingCategoryDef/KeyBindingDef/ThingDef）
- **补充更新**：ModernColonistBar.xml +3、Circinus.xml +9
- **聚合入口**：`AggregatedModNames` 加 astryl.truerpginventory / astryl.truerpgbackpacks（小写）；About.xml 覆盖列表/loadAfter/README 同步 16→18
- **验证**：缺失 key 归零；编译 0 错误；已部署（Keyed 16 文件 + DefInjected 11 类）
- **注意**：16 个旧模组均在 08-01 更新过；本次只补了新增 key，若原 key 的英文文本被改动，需按 key 重新比对

## 2026-08-02 第二轮修复（用户实测反馈）

### 1. Circinus 主窗口汉化（用户：完全没翻译）
- **原因**：Circinus 主界面（导航/按钮/状态）是**硬编码**，不在 Keyed 里（80 个 Keyed 全是设置/消息类）
- **新增**：`CircinusView` 6 个方法（DrawHeader/DrawSweepButton/DrawRail/Shorten/DrawSourceBanner/**.cctor** 静态数组 TabLabels）+ 35 条字面量（Live/Profiler/Stress/Runs/.../Start run/Speed sweep/No run selected/Recording 等）
- **未做**：11 个 Tab_* 内部（Tab_Cohorts 60+ 条等），量大待后续

### 2. ModernColonistBar tooltip 失效 + settings...
- **根因（重要）**：`AccessTools.Constructor(type, null, ...)` 的 `null` 参数数组被当作 `Type.EmptyTypes` → 只找**无参构造函数** → 有参的 `FloatMenuOptionSub(string, Func<>)` 找不到 → patch 跳过 → 右键子菜单 tooltip 英文
- **修复**：`.ctor` 改用 `targetType.GetConstructors(BindingFlags.Instance|Public|NonPublic).FirstOrDefault()`；`.cctor` 用 `GetConstructor(BindingFlags.Static|..., System.Type.EmptyTypes, null)`（注意 `Type` 需 `System.Type` 全名）
- **补翻译**：`"Modern Colonist Bar settings..."`（带省略号，在 BuildModMenu 里）——之前只有不带点的版本

### 3. Modern Faction Menu world map tooltip
- `CapitalPreview.Draw` / `Window_ModernFactions.DrawTerritorySection`+`DrawEmpireSection` / `Window_ModernEmpireSettlements.DrawRow`
- 3 条：`"\n\nClick to view on the world map."` / `"View on the world map"` / `"\n\nView on the world map."`

### 4. 聚合菜单 MD3 化（用户要求）
- **复制** `MD3Theme.cs`（120 行）+ `MD3Widgets.cs`（393 行）从 ModernExpandMenu → 本项目 `Source/AstrylsUIZhCN/Theme/` + `UI/`
- **改造**：MD3Theme 去掉 `ModernExpandMenuMod.Settings?.` 依赖（改用默认水影蓝色板）；命名空间 `AstrylsUIZhCN.Theme` / `AstrylsUIZhCN.UI`；`Theme.MD3Theme` 相对引用无需改
- **AstrylsUIZhCNMod**：MD3 卡片背景 + MD3 按钮行 + MD3 滚动条；点模组叠加打开 `Dialog_ModSettings`，关闭后返回聚合
- **复制脚本**：`D:\temp\copy_md3.ps1`

### 5. 范围说明
- 「Not loaded — install/enable to see its data here.」「Cybranian — Rim Education」**不在 astryl 模组**（属 VSE/种族/教育类第三方模组），不在聚合汉化范围

## 2026-08-02 聚合菜单卡片块 + MD3 UI 库

- **聚合界面改 MD3 卡片块网格**：4 列卡片（中文名 + 英文原名，hover 高亮），点击叠加打开 `Dialog_ModSettings`；顶部加提示「关闭设置后返回本聚合界面」
- **返回机制**：聚合界面是 `Dialog_ModSettings` 宿主窗口（选项 → Mod 设置 → astryl UI 模组合集），点卡片 `Find.WindowStack.Add(子设置)` 为**叠加**（宿主保持在下层），子设置关闭后自动回到聚合。若仍不返回，需用户描述具体操作路径（入口/关闭方式）
- **MD3 UI 库提炼**：`_templates/md3-ui-lib/`（MD3Theme.cs + MD3Widgets.cs 解耦版 + TEMPLATE_GUIDE.md），与 `md3-ui-style/`（规范文档）互补；RimWorld 模组间不能引用 DLL，共享 UI 库只能源码复制（独立命名空间）

## ⚠️ 方案变更：直接字面量替换（2026-08-02，重要）

用户反馈「依旧缺汉化」→ 查日志定位根因并重构：

- **根因**：`HardcodedStringReplacer` 静态构造函数崩溃 → `Patcher` 全部 patch 未注册 → 所有硬编码汉化失效
  - 崩溃原因：字典**重复 key** `" selected"`（第一次补充加 `SelectedSuffix`，批1 指挥中心又加 `CcSelected`）→ `ArgumentException: An item with the same key has already been added`
  - 教训：**字典禁止重复英文 key**，批量添加映射前必须查重
- **新方案（废弃 Keyed + Translator 运行时查表）**：
  - Transpiler 把原模组 `ldstr 英文` 直接替换为 `ldstr 中文`（**380 条翻译内嵌 DLL**），不再调用 `Translator.Translate`、不读取任何语言文件
  - 零运行时依赖、绝不缺键；代价：改翻译需重新编译 DLL
  - `HardcodedZhCN.xml` 已删除（DLL 不再使用）
- **生成脚本**：`D:\temp\gen_hardcoded_direct.ps1`——读取旧字典（英文→键名）+ XML（键名→中文）合并去重生成新字典，自动处理 `\n`/`\"` 转义，可复用
- **注意**：脚本文件需纯 ASCII（PowerShell 5.1 按 ANSI 读 .ps1，中文会乱码），中文注释会破坏脚本

## 后续待办
- [x] 项目初始化（About.xml / README / build.bat / Languages 骨架）— 2026-08-01
- [x] 创建通用 + 专项模板（工作区 `_templates/`）— 2026-08-01
- [x] 提取英文 Keyed 到 `Languages/ChineseSimplified/Keyed/` — 2026-08-01
- [x] 翻译 Keyed 14/14 全部完成（约 2300 条）— 2026-08-02
  - 含 ModernCC（711 条）、ModernDevTools（340 条）
  - 残留检查：仅剩术语/专有名词与占位符格式（应保留），无遗漏
- [x] 缺模组适配：About.xml 用 loadAfter 非强制依赖、专项模板加入模组存在性检查 — 2026-08-02
- [x] 确认 mod ID：`yintx.deepseek.astrylUImod.zhcn` — 2026-08-02
- [x] DefInjected 翻译完成（约 120 label + 94 desc）— 2026-08-02
  - MCE.EditorModuleDef（30 个编辑器模块）、MainButtonDef（3 个主按钮）
  - KeyBindingCategoryDef（1 个类别）+ KeyBindingDef（5 个按键）
  - ModernDevTools.ErrorModuleDef（13 个分析模块）+ KnownIssueDef（48 个：5 良性 + 43 已知问题）
  - HistoryAutoRecorderDef（14 个）+ HistoryAutoRecorderGroupDef（6 个组）
- [x] 编写 Harmony patch DLL（Social Tab + Learning Menu 全硬编码，已编译 0 警告 0 错误）— 2026-08-02
  - 项目：`Source/AstrylsUIZhCN/`，产物 `Assemblies/AstrylsUIZhCN.dll`，Harmony ID `yintx.deepseek.astrylUImod.zhcn.hardcoded`
  - 含模组存在性检查（PatchIfPresent），缺模组不报错
  - Modern Social Tab：17 个方法（设置项/标签页绘制/位置状态）
  - Modern Learning Menu：13 个方法（面板标题/说明/绘制/设置）
  - 采用 Keyed + Translator 维护模式：翻译在 `Keyed/HardcodedZhCN.xml`（85 条），C# 只留稳定键映射
- [x] build.bat 部署验证（编译 + 部署到游戏 Mods）— 2026-08-02
- [x] About.xml 覆盖模组列表加 Steam 工坊跳转链接（16 个模组）— 2026-08-02
- [x] 设置聚合界面（隐藏原 16 个模组 Mod 设置条目 + 汉化模组聚合入口分开打开）— 2026-08-02
- [ ] （可选）补充其他模组的少量硬编码 patch：Modern Bio Tab / Faction Menu / Circinus / Modern CC（Colonist Bar 已全量补完）
- [ ] 更新 About.xml 描述（翻译条数/版本/依赖/设置聚合说明）与 README.md
- [ ] 游戏内实测验证（设置聚合界面 + 各模组设置 + 缺模组不报错）
