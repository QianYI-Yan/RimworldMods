# TailorMade Unlock Fix

[![Steam 工坊](https://img.shields.io/badge/Steam-%E5%B7%A5%E5%9D%8A-blue?style=flat&logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3769650943)

## 简介

修复 TailorMade 的 **unlockRestrictedApparel**（解锁跨种族服装限制）开关无效的问题。

**作者：** yintx, deepseek

## 问题

TailorMade 的 `Patches/TailorMade_UnlockRaceRestrictedApparel.xml` 在 XML 加载阶段无条件执行，在游戏开始前就删除了 HAR 种族的服装限制数据。导致设置中的「解锁跨种族服装限制」开关形同虚设——关了跟没关一样。

## 解决方案

本模组在 XML 补丁阶段就拦截了 TailorMade 的删除操作，HAR 的原始服装限制数据完好保留。

- `unlockRestrictedApparel = ON`：运行时覆盖 HAR 拒绝逻辑，允许所有种族穿所有服装
- `unlockRestrictedApparel = OFF`：不干预，HAR 原始限制正常生效

## 依赖

- [**TailorMade: Unified Apparel & Body Refitting**](https://steamcommunity.com/sharedfiles/filedetails/?id=3014568497)
- **Humanoid Alien Races (HAR)**

## 加载顺序

必须排在 TailorMade **之后**加载。

## 构建

执行 `build.bat` 即可编译并部署到游戏 Mods 目录。

## 链接

- [Steam 工坊页面](https://steamcommunity.com/sharedfiles/filedetails/?id=3769650943)
- [源模组 TailorMade Steam 工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3014568497)
- [GitHub 源码](https://github.com/QianYI-Yan/RimworldMods/tree/main/TailorMade-ZhCN/TailorMadeUnlockFix)
