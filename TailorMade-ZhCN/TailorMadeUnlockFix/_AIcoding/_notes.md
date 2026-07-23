# TailorMadeUnlockFix 笔记

## 方案：提前拦截 XML 补丁
- Mod 子类构造函数在 XML Patch 前安装 Harmony 补丁
- 拦截 `PatchOperationRemove.ApplyWorker` — 阻止删除 `apparelList`
- 拦截 `PatchOperationReplace.ApplyWorker` — 阻止修改 `onlyUseRaceRestrictedApparel`
- 直接按 XPath 特征拦截，不区分调用者（调用栈无法追溯来源模组）
- HAR 原始限制数据完好保留
- `unlockRestrictedApparel = ON`: 运行时覆盖 HAR 拒绝（放行一切）
- `unlockRestrictedApparel = OFF`: 不干预，HAR 原始限制生效
