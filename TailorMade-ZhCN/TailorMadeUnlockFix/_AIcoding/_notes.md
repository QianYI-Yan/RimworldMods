# TailorMadeUnlockFix 笔记

## 问题
TailorMade 的 XML Patch 在加载阶段无条件删除 HAR 种族的 `apparelList`，导致 `unlockRestrictedApparel` 设置关不掉跨种族穿衣。

## 修复策略（当前：仅放行，不阻止）
- ~~XML Patch 恢复 `onlyUseRaceRestrictedApparel`~~ → 已删除（会导致 HAR 全拒）
- 从磁盘读取原始 XML 恢复 `apparelList` 数据
- Harmony Postfix 只用来 ALLOW 外星种族衣服，不用来 BLOCK
- 仅对非外星种族（人类）执行 BLOCK

## 关键 API
- `HarSupport.IsAlienRace(ThingDef)` — 判断 HAR 外星种族
- `HarSupport.IsRaceRestrictedApparel(ThingDef)` — 检查 HAR 限制集合
- `TailorMadeMod.Settings.unlockRestrictedApparel` — TailorMade 设置
- `EquipmentUtility.CanEquip(Thing, Pawn, ref string, bool)` — 装备检查
- `AlienRace.RaceRestrictionSettings.CanWear(ThingDef, ThingDef)` — HAR 穿戴检查
