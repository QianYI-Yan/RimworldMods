# TailorMadeUnlockFix 笔记

## 方案（当前：提前拦截 XML 补丁）
- Mod 子类构造函数在 XML Patch 前执行
- Harmony 拦截 `PatchOperationRemove` + `PatchOperationReplace`
- TailorMade 的删除 `apparelList` 和修改 `onlyUseRaceRestrictedApparel` 被阻止
- HAR 原始限制数据完好保留
- `unlockRestrictedApparel = ON`: 运行时覆盖 HAR 拒绝（放行一切）
- `unlockRestrictedApparel = OFF`: 不干预，HAR 原始限制生效

## 关键 API
- `HarSupport.IsAlienRace(ThingDef)` — 判断 HAR 种族
- `TailorMadeMod.Settings.unlockRestrictedApparel` — 设置项
- `EquipmentUtility.CanEquip(Thing, Pawn, ref string, bool)` — 装备检查
- `AlienRace.RaceRestrictionSettings.CanWear(ThingDef, ThingDef)` — HAR 穿戴检查
- `PatchOperationRemove.ApplyWorker(XmlDocument)` — 拦截点
- `PatchOperationReplace.ApplyWorker(XmlDocument)` — 拦截点
