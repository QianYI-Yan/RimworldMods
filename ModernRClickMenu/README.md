# Modern RClick Menu

> MD3（Material Design 3）风格的分组右键菜单模组

选中小人后，右键一个存放了多种物品的**单格储物容器**时，弹出一个现代化的 MD3 风格悬浮窗，取代原版扁平的右键菜单：

- 物品按类型分组，每组标题显示**物品图标 + 名称 + 总数**
- 每组下方以子菜单展开操作：**穿戴 / 拾取 / 搬运**（衣物额外有强制穿戴）
- 悬浮窗**跟随鼠标**弹出，**支持滚动**，并设有**最大高度**限制

## 效果示意

```
┌─────────────────────────────┐
│  [图标] 皮帽 ×12            │  ← 物品分组标题
│    └ 强制穿戴：皮帽          │  ← 子菜单操作（缩进）
│    └ 拾取：皮帽              │
│    └ 搬运到储物区            │
│  [图标] 钢锭 ×80            │
│    └ 拾取全部：钢锭          │
│    └ 拾取若干…              │
│    └ 搬运到储物区            │
└─────────────────────────────┘
```

## 安装

将 `ModernRClickMenu` 文件夹放入游戏 `Mods` 目录，在游戏内启用即可。

## 构建

```bat
build.bat
```

构建产物自动复制到 `Assemblies/`，并部署到游戏 `Mods/ModernRClickMenu/`。

## 技术说明

- 框架：`.NET Framework 4.7.2` + `Krafs.Rimworld.Ref 1.6.4871` + `Lib.Harmony 2.3.6`
- Hook 点：`FloatMenuMakerMap.GetOptions`（Postfix）——清空原版选项后弹出自定义悬浮窗
- 操作项复用原版 `FloatMenuOptionProvider_Wear` / `FloatMenuOptionProvider_PickUpItem` 的选项逻辑，保证与原版行为一致（穿戴判定、负重判定等）
- 主题 Token 集中在 `MD3Theme`，所有颜色 / 圆角 / 间距均取自该处，为后续接入 CSS 解析器做外观自定义预留接口

## 当前范围

- 仅作用于**单格储物容器**（`Building_Storage` 且 `def.Size.Area == 1`）
- 其他右键目标（地面物品、建筑、小人等）保持原版菜单
- 多选小人场景暂不接管，保持原版行为

## 源码

[github.com/QianYI-Yan/RimworldMods/tree/main/ModernRClickMenu](https://github.com/QianYI-Yan/RimworldMods/tree/main/ModernRClickMenu)
