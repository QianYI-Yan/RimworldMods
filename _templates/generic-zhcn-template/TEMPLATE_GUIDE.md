# 通用汉化项目模板使用指南

这是一套适用于任何 RimWorld 汉化项目的通用模板。复制整个 `generic-zhcn-template/` 目录到工作区，改名为 `【模组名】-ZhCN`，然后按以下步骤使用。

## 文件清单

| 文件 | 用途 |
|------|------|
| `About/About.xml` | 汉化模组元数据（必填占位符见文件内注释） |
| `README.md` | 项目说明（占位符见文件内注释） |
| `build.bat` | 部署到游戏 Mods 目录的脚本 |
| `Languages/ChineseSimplified/Keyed/Template.xml` | Keyed 翻译文件格式参考 |
| `Languages/ChineseSimplified/DefInjected/ThingDef/Template.xml` | DefInjected 翻译文件格式参考 |

## 使用步骤

1. **复制并改名**：`generic-zhcn-template/` → `模组名-ZhCN/`
2. **填写 About.xml**：替换全部【】占位符
   - mod ID 规则：`作者名.原模组id.zhcn`；**创建新模组前先问用户 mod ID**
   - description 必须附带 GitHub 仓库链接（工作区硬性习惯）
3. **提取英文原文**：从原模组 `Languages/English/` 获取英文 Keyed / DefInjected 文件作为翻译工作底稿。**每次检查或更新翻译时，都要重新从原模组提取最新原文，不能依赖旧的 `OriginalMods/` 副本。**
4. **翻译**：
   - Keyed：键名保持与英文完全一致，值替换为中文
   - DefInjected：defName 与原模组 Def 完全一致，注入 label/description
5. **build.bat 部署**：运行后自动复制到游戏 `Mods/` 目录
6. **更新 About.xml 描述**：翻译完成后更新条数、版本、依赖等信息（工作区硬性习惯）

## 项目标准结构（完整形态）

```
模组名-ZhCN/
├── _AIcoding/               ← AI 协作笔记（_notes.md 必读）
├── About/
│   ├── About.xml
│   └── PublishedFileId.txt  ← 发布工坊后填写 ID
├── Languages/
│   └── ChineseSimplified/
│       ├── Keyed/           ← UI 文本翻译
│       └── DefInjected/     ← Def 注入翻译
├── OriginalMods/            ← 原模组副本（参考/提取源）
├── OriginalSRC-SDK/         ← 反编译源码（排查硬编码用）
├── Assemblies/              ← 需要 DLL 时（硬编码 patch）
├── Source/                  ← patch 源码
├── README.md
└── build.bat
```

## 硬编码文本处理

如果原模组 UI 文本硬编码在 DLL 中（无语言文件），需要：
1. 用 dnSpy 反编译 DLL（见 `.github/skills/dnspy-tool/SKILL.md`）
2. 使用专项模板 `special-hardcoded-patch/` 编写 Harmony 字符串替换补丁
