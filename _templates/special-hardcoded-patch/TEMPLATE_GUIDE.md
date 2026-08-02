# 专项模板：硬编码字符串 Harmony 汉化补丁

当原模组的 UI 文本**硬编码在 DLL 中**（没有语言文件，反编译后才能看到）时，用本模板写一个 Harmony 补丁 DLL，在运行时把英文字符串替换为中文。

## 使用步骤

1. **反编译定位**：用 dnSpy 反编译目标 DLL（见 `.github/skills/dnspy-tool/SKILL.md`），找到包含硬编码 UI 字符串的方法
   - 重点看 `Widgets.Label(...)`、`TooltipHandler.TipRegion(...)`、`FloatMenuOption(...)`、`CheckboxLabeled(...)` 等调用处的字符串
2. **复制模板**：`special-hardcoded-patch/` → 汉化项目的 `Source/HardcodedZhCN/`
3. **填翻译表**（推荐 Keyed + Translator 模式）：
   - 新建 `Languages/ChineseSimplified/Keyed/HardcodedZhCN.xml` 存放「键名 → 中文」翻译
   - 在 `HardcodedStringReplacer.cs` 的 `KeyForString` 字典里填「英文字符串 → 键名」
   - 字符串必须与反编译源码中的字面量**逐字符一致**（含大小写、标点）
   - 含 `{0}` 占位符的字符串可整体翻译（占位符保留）
4. **注册方法**：用 `Patcher.cs` 的 `PatchIfPresent(harmony, 模组packageId, 类型全名, 方法名)` 为每个含硬编码字符串的方法注册
5. **编译**：`dotnet build Source/HardcodedZhCN/HardcodedZhCN.csproj -c Release`
   - 产物自动复制到 `Assemblies/HardcodedZhCN.dll`
6. **About.xml**：汉化模组的 `modDependencies` 增加 [Harmony](steamcommunity.com/sharedfiles/filedetails/?id=2009463077)，`loadAfter` 加 Harmony 与原模组

## 维护模式（重要）

硬编码汉化的翻译表有两种存放方式，**推荐 Keyed + Translator 模式**：

| 模式 | 翻译存放 | 改翻译时 | 适用 |
|------|---------|---------|------|
| **Keyed + Translator**（推荐） | `Keyed/HardcodedZhCN.xml`（标准语言文件） | **只改 XML，无需重编译 DLL**；玩家可直接编辑；缺键自动回退英文不报错 | 长期维护、发布汉化 |
| 字典写死 C# | `HardcodedStringReplacer.cs` 值直接填中文 | 每次重编译 DLL | 临时/原型 |

`HardcodedStringReplacer.cs` 的 Transpiler 已同时支持两种模式（值以 `HardcodedZhCN.` 开头视为键名走 Translator，否则直接替换）。

## 关键点

- **Transpiler 替换 ldstr**：只替换「字符串字面量」指令，不改逻辑，兼容性最好
- **性能**：Transpiler 只影响被补丁方法，方法运行时的开销可忽略
- **Harmony ID 唯一**：`Patcher.cs` 中的 ID 不要与其他模组冲突
- **版本兼容**：原模组更新后若字符串变化，翻译表可能失效，需复查

## ⚠️ 聚合汉化多个模组时的适配（重要）

当汉化模组同时覆盖多个原模组、且玩家可能只装其中一部分时，**必须做模组存在性检查**，否则缺模组会直接报错：

1. **About.xml**：用 `loadAfter` 列出全部原模组即可（缺失只影响排序提示，不报错）；**不要**用 `modDependencies` 强制依赖全部，否则缺一个整个汉化被禁用
2. **Patch 注册**：用 `Patcher.cs` 中的 `PatchIfPresent(harmony, 模组packageId, 类型全名, 方法名)` 手动注册，它会在模组未激活 / 类型不存在 / 方法不存在时静默跳过
3. **Keyed / DefInjected**：RimWorld 语言系统本身安全——Keyed 键缺失只显示键名，DefInjected 只注入存在的 def，都不会报错

## 适用场景对照

| 场景 | 方案 |
|------|------|
| 有语言文件（Keyed） | 通用模板，直接翻译 Keyed |
| 无语言文件、文本硬编码 | 本专项模板（Harmony Transpiler + Keyed 维护模式） |
| Def 的 label/description | 通用模板的 DefInjected 注入 |
| 设置项有 Keyed 但部分提示硬编码 | 两者结合 |
