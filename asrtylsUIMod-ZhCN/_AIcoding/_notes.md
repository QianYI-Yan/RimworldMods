# asrtylsUIMod-ZhCN — 项目笔记（AI 必读）

> 作者 astryls 的 UI 模组聚合简体中文汉化项目
> 处理本模组前必须先读此文件

## 项目记忆

- 汉化模组 packageId: `yintx.deepseek.astrylUImod.zhcn`（用户已确认，2026-08-02）
- 目录名按用户指定拼写 `asrtylsUIMod-ZhCN`（非 astryls）
- 聚合范围 16 个 UI/工具模组；排除 True RPG 系列（「rpgtrue 背包」）、TailorMade、As Above So Below
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

## 模组清单（聚合范围：16 个）

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

## 排除的模组
- True RPG Inventory / True RPG Backpacks — 用户指定「rpgtrue 背包」不汉化
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
- [ ] （可选）补充其他模组的少量硬编码 patch：Modern Bio Tab / Colonist Bar / Faction Menu / Circinus / Modern CC
- [ ] 更新 About.xml 描述（翻译条数/版本/依赖/设置聚合说明）与 README.md
- [ ] 游戏内实测验证（设置聚合界面 + 各模组设置 + 缺模组不报错）
