# NiceResearchTab-ZhCN 项目笔记

## 项目信息
- 原模组：Nice Research Tab（Andromeda.NiceResearchTab，Steam 3773496046）
- 汉化 ID：`yintx.deepseek.niceresearchtab.zhcn`
- 原模组依赖：Harmony + MilkyWay（andromeda.milkyway）；兼容世界科技等级、半随机研究、Discoveries 等

## 汉化范围
- Keyed：原模组 `Languages/English/Keyed/Common.xml` 共 34 键（设置/队列/按钮/提示）
- DefInjected：
  - `NiceResearchTab.ExtraCategoryDef` × 4（护甲/电力/炮塔/武器）
  - `MainButtonDef/Research`（label+description，双保险）
- 硬编码（Harmony Transpiler，3 个方法 8 处字符串）：
  - `NiceResearchTab.Settings.DrawPerformance`：High/Medium/Low/Custom
  - `NiceResearchTab.DebugOptionsWidget..ctor`：Finish now/Apply techprint/Unhide（DEV 调试按钮）
  - `NiceResearchTab.CurrentResearchWidget.Draw`：Remaining time: N/A

## 关键技术点
- 原模组 MainButtonDef Research 的 label=research 走原版 Keyed 翻译；description 需 DefInjected 注入
- `UnlocksLabel` 硬编码 "Unlocks" 会被 CacheDescription 里 `Translator.Translate("Unlocks")` 覆盖，无需补丁
- 游戏 Core 仅英文数据，玩家中文来自创意工坊汉化；我们的 DefInjected 按 defName 注入不受影响

## 待办
- [x] 项目骨架 + Keyed + DefInjected + 硬编码补丁
- [ ] 游戏内实测检查漏译
- [ ] 发布工坊后填 About/PublishedFileId.txt

## 维护提醒
- 原模组更新后：重新提取 `Languages/English/Keyed/Common.xml` 对照，检查硬编码字符串是否变化
- 反编译参考目录：`D:\temp\nrt_decompiled`
