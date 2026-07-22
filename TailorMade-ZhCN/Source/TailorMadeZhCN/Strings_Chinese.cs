using System.Collections.Generic;

namespace TailorMadeZhCN
{
    /// <summary>
    /// 英文→简体中文 翻译字典
    /// 键 = 源模组中的原始英文字符串
    /// 值 = 中文翻译
    /// </summary>
    internal static class Strings_Chinese
    {
        public static readonly Dictionary<string, string> Map = new()
        {
            // ═══════════════════════════════════════════
            // TailorMadeMod.cs — SettingsCategory()
            // ═══════════════════════════════════════════
            ["TailorMade"] = "TailorMade 裁缝大师",

            // ═══════════════════════════════════════════
            // TailorMadeMod.cs — DoSettingsWindowContents()
            // ═══════════════════════════════════════════

            // --- 主开关 ---
            ["Enabled"] = "启用",
            ["Master switch. Disable to render all apparel vanilla-style."]
                = "总开关。关闭后所有服装按原版方式渲染。",

            // --- 贴合设置 ---
            ["Fitting"] = "贴合设置",
            ["Automatic mask fitting"] = "自动蒙版贴合",
            ["Derive offsets and scaling from the apparel art and body silhouette automatically so apparel always fills the body mask. Disable to only use explicit TailorPatternDefs."]
                = "从服装贴图和身体轮廓自动推导偏移和缩放，使服装始终贴合身体蒙版。关闭后仅使用显式定义的 TailorPatternDef。",
            ["Preserve aspect ratio"] = "保持宽高比",
            ["On: uniform scaling (art may overflow the mask slightly on one axis, gets clipped).\nOff: stretch each axis so the art exactly fills the mask."]
                = "开启：统一缩放（贴图可能在某一方向上略微溢出蒙版，被裁剪）。\n关闭：分别拉伸各轴使贴图恰好填满蒙版。",
            ["Max baked texture resolution: "] = "最大烘焙纹理分辨率：",
            ["Trilinear filtering"] = "三线性过滤",
            ["Smoother scaling of baked textures. Slightly blurrier up close."]
                = "烘焙纹理缩放更平滑。近距离略微模糊。",
            ["Unlock texture resolution"] = "解锁纹理分辨率",
            ["Bake apparel at its native texture resolution instead of clamping to the slider above. Preserves full detail from high-resolution gear mods (which the cap would otherwise downscale). Uses more VRAM and a longer first bake."]
                = "以服装原始纹理分辨率烘焙，不受上方滑块限制。保留高清装备模组的所有细节（上限会降级）。消耗更多显存且首次烘焙更久。",
            ["Sharp resampling (bicubic)"] = "锐利重采样（双三次）",
            ["Bicubic instead of bilinear sampling when art is scaled up — visibly crisper edges. Costs noticeably more CPU on the one-time bake. No substitute for higher-res source art (there is no AI upscaling in-engine)."]
                = "贴图放大时使用双三次而非双线性采样——边缘明显更锐利。一次性烘焙消耗更多 CPU。无法替代更高分辨率源贴图（引擎内无 AI 放大功能）。",
            ["Unlock body texture resolution"] = "解锁身体纹理分辨率",
            ["Bake body art (auto-resized alien bodies, forced body-mod textures, scaled heads) at its native resolution instead of clamping to the slider above — the same unlock as apparel, but for bodies. Keeps high-resolution body packs crisp. Uses more VRAM and a longer first bake."]
                = "以原始分辨率烘焙身体贴图（自动缩放的外星人身体、强制身体模组纹理、缩放头部），不受上方滑块限制——与服装解锁相同，但作用于身体。保持高分辨率身体包的清晰度。消耗更多显存且首次烘焙更久。",
            ["Conform side views (east/west)"] = "适配侧面视图（东/西）",
            ["Fit apparel to the body on the east/west facings too, automatically (on by default). Uses a stable bounding-box fit on the sides (no per-row warp). Turn off for exactly-vanilla side rendering. Front/back are always refitted."]
                = "自动将服装适配到东/西朝向的身体（默认开启）。侧面使用稳定的包围盒适配（无逐行扭曲）。关闭以获得完全原版的侧面渲染效果。正面/背面始终贴合。",
            ["Outline refit apparel"] = "轮廓描边",
            ["Draw a thin dark outline around masked apparel so it matches the body's stylized outline. On by default."]
                = "在蒙版服装周围绘制细暗色轮廓，使其与身体风格化轮廓匹配。默认开启。",

            // --- 轮廓详细设置 ---
            ["Outline thickness: "] = "轮廓粗细：",
            ["px"] = "像素",
            ["Side (east/west) outline boost: x"] = "侧面（东/西）轮廓增强：x",

            // --- 女性体型 ---
            ["Force Female body type on female pawns"] = "女性角色强制使用女性体型",
            ["Adult female humanlikes always use the Female body type, CB2's primary silhouette. Applies to newly generated pawns immediately and to existing pawns on load / settings change. HAR races are not affected."]
                = "成年女性类人角色始终使用 Female 体型，即 CB2 的主要轮廓。立即应用于新生成的角色，并在加载/设置更改时应用于现有角色。不影响 HAR 种族。",

            // --- 裁剪 ---
            ["Clip apparel to body silhouette"] = "按身体轮廓裁剪服装",
            ["    Auto (recommended) clips a body type only when a body retexture is detected for it — so Male starts clipping automatically once a male body mod (Nal, CB2, WrelicK, ...) is active. Set per-body overrides in the Body types list below."]
                = "    自动（推荐）仅在检测到某体型有身体重制贴图时才进行裁剪——因此一旦男性身体模组（Nal、CB2、WrelicK 等）激活，Male 体型自动开始裁剪。可在下方的「体型」列表中逐个体型覆盖设置。",

            // --- 头发 ---
            ["Keep hair visible under body apparel"] = "身体服装下保持头发可见",
            ["Stops full-body suits and armor (common in HAR races) from blanking hair, beard and eyes just because they list head body-part-groups for damage coverage. Only genuinely head-worn items (hats, helmets, masks that don't also cover the torso/legs) hide hair."]
                = "防止全身服和装甲（常见于 HAR 种族）仅仅因为列出了头部身体部位组作为伤害覆盖就隐藏头发、胡须和眼睛。只有真正的头部穿戴物品（帽子、头盔、不覆盖躯干/腿的面具）才隐藏头发。",

            // --- 服装检测 ---
            ["Apparel detection (THIGAPPE-style)"] = "服装检测（THIGAPPE 风格）",
            ["Detect apparel coverage"] = "检测服装覆盖范围",
            ["Classify apparel by what it covers (boots, pants, bodysuits, armor) using THIGAPPE's detection rules and any THIGAPPE_ tags. Partial garments are then fit to their body region instead of the whole silhouette, so boot and pants textures land in the right place. On by default."]
                = "使用 THIGAPPE 的检测规则和 THIGAPPE_ 标签，按覆盖范围对服装进行分类（靴子、裤子、紧身衣、装甲）。部分服装将适配到对应的身体区域而非整个轮廓，使靴子和裤子纹理落在正确位置。默认开启。",
            ["    Boots (fit to the feet)"] = "    靴子（适配到脚部）",
            ["Footwear that only covers the feet is fit to the feet region of the body instead of being stretched over the whole silhouette."]
                = "仅覆盖脚部的鞋类适配到身体的脚部区域，而不是拉伸到整个轮廓。",
            ["    Pants (fit to the legs)"] = "    裤子（适配到腿部）",
            ["Legwear that only covers the legs is fit to the legs region of the body instead of being stretched over the whole silhouette."]
                = "仅覆盖腿部的裤装适配到身体的腿部区域，而不是拉伸到整个轮廓。",
            ["    Chest pieces (fit to the torso)"] = "    胸甲（适配到躯干）",
            ["Torso-only armor and tops whose texture is drawn just for the chest (breastplates, cuirasses, tunics from mods like Medieval Overhaul) are fit to the upper-body region instead of being stretched over the whole silhouette. Only kicks in when the worn texture is actually torso-only; long coats and capes that drape down the body are left full-body."]
                = "仅覆盖躯干的装甲和上衣（胸甲、护胸甲、中世纪 overhaul 等模组的长袍）适配到上半身区域，而不是拉伸到整个轮廓。仅在穿戴的纹理确实是仅限躯干时才生效；长外套和披风等垂坠至身体下方的保持全身适配。",
            ["    Bodysuits (full-body, on skin)"] = "    紧身衣（全身、贴身）",
            ["Torso-and-legs skinsuits are treated as full-body garments (whole-body fit)."]
                = "覆盖躯干和腿部的紧身服被视为全身服装（全身适配）。",
            ["    Armor (full-body protective)"] = "    装甲（全身防护）",
            ["Torso-and-legs armor and protective suits are treated as full-body garments (whole-body fit)."]
                = "覆盖躯干和腿部的装甲和防护服被视为全身服装（全身适配）。",

            // --- Sized Apparel ---
            ["Sized Apparel for RJW"] = "Sized Apparel（用于 RJW）",
            [" (not loaded)"] = "（未加载）",
            ["Defer to Sized Apparel"] = "交由 Sized Apparel 处理",
            ["Apparel that Sized Apparel resizes, and bodies it manages, are left entirely to it — its hand-drawn size variants and body-part rendering stay untouched. TailorMade still fits everything Sized Apparel doesn't cover: unsupported apparel, races it skips, and gear with missing body-type textures. Turning this off lets TailorMade re-bake Sized Apparel's swapped art too (not recommended — it distorts art hand-aligned to Sized Apparel's bodies)."]
                = "Sized Apparel 调整尺寸的服装及其管理的身体完全交由它处理——其手绘尺寸变体和身体部位渲染保持不变。TailorMade 仍然适配 Sized Apparel 未覆盖的所有内容：不支持的服装、跳过的种族以及缺少体型纹理的装备。关闭此选项会让 TailorMade 也重新烘焙 Sized Apparel 的替换贴图（不推荐——会扭曲针对 Sized Apparel 身体手动对齐的贴图）。",
            ["    Sized Apparel (OTYOTY.SizedApparel) is not active — nothing to defer to."]
                = "    Sized Apparel（OTYOTY.SizedApparel）未激活——无需交办。",

            // --- Apparel Paper Pattern ---
            ["Apparel Paper Pattern"] = "Apparel Paper Pattern",
            ["Defer to Apparel Paper Pattern"] = "交由 Apparel Paper Pattern 处理",
            ["Apparel that APP re-renders through its pattern defs (including THIGAPPE's pattern packs) is left entirely to it. TailorMade still fits everything APP doesn't cover: races and layers with no pattern def, and apparel with missing body-type textures (which APP can't handle). To hand a specific pawn/apparel combination to TailorMade instead, exempt it in APP's own tuner — exempted items are picked up by TailorMade automatically. Turning this off makes TailorMade re-fit APP's apparel from the original art too (APP still spends VRAM rendering its version first — prefer exempting items in APP's tuner instead)."]
                = "APP 通过其模式定义（包括 THIGAPPE 的模式包）重新渲染的服装完全交由它处理。TailorMade 仍然适配 APP 未覆盖的所有内容：没有模式定义的种族和图层，以及缺少体型纹理的服装（APP 无法处理）。如需将特定角色/服装组合交给 TailorMade，请在 APP 的调谐器中豁免——豁免的项目会被 TailorMade 自动接管。关闭此选项会使 TailorMade 也从原始贴图重新适配 APP 的服装（APP 仍然会消耗显存先渲染其版本——建议改为在 APP 调谐器中豁免项目）。",
            ["Open APP tuner (User Tuner)"] = "打开 APP 调谐器（用户调谐器）",
            ["    APP's tuner opens in-game. There is also an 'APP tuner' button in the TailorMade editor window."]
                = "    APP 的调谐器在游戏中打开。TailorMade 编辑窗口中也有一个「APP 调谐器」按钮。",
            ["    Apparel Paper Pattern (nalsnoir.ApparelPaperPattern) is not active — nothing to defer to."]
                = "    Apparel Paper Pattern（nalsnoir.ApparelPaperPattern）未激活——无需交办。",

            // --- FemaleBodyVariants ---
            ["FemaleBodyVariants"] = "FemaleBodyVariants",
            ["Fit apparel to female body variants"] = "适配服装到女性身体变体",
            ["FemaleBodyVariants draws non-male pawns with '_Female' variant body textures (Naked_Thin_Female, Naked_Fat_Female, Naked_Hulk_Female) when a body texture mod ships them. With this on, TailorMade bakes apparel against that same female silhouette — and bodies TailorMade renders itself (forced providers, auto-resized alien races) prefer the provider's own female art too. Tip: with a body pack that ships these variants you can turn OFF 'Force Female body type' above and keep Thin/Fat/Hulk builds on female pawns."]
                = "当身体纹理模组提供时，FemaleBodyVariants 使用 '_Female' 变体身体纹理（Naked_Thin_Female、Naked_Fat_Female、Naked_Hulk_Female）绘制非男性角色。开启后，TailorMade 针对相同的女性轮廓烘焙服装——且 TailorMade 自身渲染的身体（强制提供者、自动缩放的外星种族）也优先使用提供者自己的女性贴图。提示：使用提供这些变体的身体包时，你可以关闭上方的「强制女性体型」并保留女性角色的 Thin/Fat/Hulk 体型。",
            ["    FemaleBodyVariants (tiagocc0.FemaleBodyVariants) is not active — female Thin/Fat/Hulk pawns use the unisex body art."]
                = "    FemaleBodyVariants（tiagocc0.FemaleBodyVariants）未激活——女性 Thin/Fat/Hulk 角色使用通用身体贴图。",

            // --- HAR ---
            ["Humanoid Alien Races"] = "Humanoid Alien Races",
            [" (HAR not loaded)"] = "（HAR 未加载）",
            ["Off — leave alien bodies untouched"] = "关闭——保持外星人身体不变",
            ["Auto-resize — refit alien body art to fill the body-mod silhouette"] = "自动缩放——重新适配外星人身体贴图以填充身体模组轮廓",
            ["Force body texture — replace alien bodies with an installed body texture mod"] = "强制身体纹理——用已安装的身体纹理模组替换外星人身体",
            ["    No body texture mod detected — forcing would just use the plain vanilla Core bodies. Install a body retexture (CB2, Nal's, WrelicK, ...) to use this mode as intended."]
                = "    未检测到身体纹理模组——强制模式只会使用普通的原版 Core 身体。安装身体重制贴图（CB2、Nal's、WrelicK 等）以按预期使用此模式。",
            ["    Auto — last-loaded body texture ("] = "    自动——最后加载的身体纹理（",
            [")"] = "）",
            ["Unlock race-restricted apparel for vanilla pawns"] = "为原版角色解锁种族限制的服装",
            ["HAR lets races mark apparel as race-only, which normally blocks vanilla pawns from wearing it. Since TailorMade refits any apparel to any body, this lifts that lock for non-alien pawns. Alien races keep their own restrictions."]
                = "HAR 允许种族将服装标记为仅限种族，这通常阻止原版角色穿戴。由于 TailorMade 可以将任何服装适配到任何身体，此选项为非外星人角色解除此锁定。外星种族保留其自身限制。",

            // --- 每种族身体映射 ---
            ["Per-race body mapping (overrides the global mode above for that race)"] = "逐种族身体映射（覆盖上方的全局模式）",
            [" ("] = "（",
            ["Default -> Off -> Auto-resize -> Force per installed body mod. Default follows the global mode above."]
                = "默认 → 关闭 → 自动缩放 → 按已安装身体模组强制。默认遵循上方的全局模式。",
            ["Vanilla-bodied race: Default follows the global body mod shadowing; Force renders a specific installed body mod's textures for this race."]
                = "原版身体种族：默认遵循全局身体模组阴影；强制为本种族渲染特定已安装身体模组的纹理。",

            // --- MapTokenLabel ---
            ["Default"] = "默认",
            ["Off"] = "关闭",
            ["Auto-resize"] = "自动缩放",
            ["Force: Auto"] = "强制：自动",
            ["Force: "] = "强制：",
            [" (missing)"] = "（缺失）",

            // --- 服装图层 ---
            ["Apparel layers (which clothing layers get refitted)"] = "服装图层（哪些服装层会被重新适配）",

            // --- 体型 ---
            ["Body types (untick to exclude; the button sets per-body clipping)"] = "体型（取消勾选以排除；按钮设置逐体型裁剪）",
            ["Clip: default"] = "裁剪：默认",
            ["Clip: Always"] = "裁剪：始终",
            ["Clip: Never"] = "裁剪：从不",
            ["Clip: Auto"] = "裁剪：自动",
            ["Effective clip: "] = "实际裁剪：",
            [" -> clipping (retexture detected)"] = " → 裁剪中（检测到重制贴图）",
            [" -> not clipping (no retexture detected)"] = " → 未裁剪（未检测到重制贴图）",

            // --- 备份与分享 ---
            ["Backup & sharing"] = "备份与分享",
            ["    Export writes your per-item adjustments (fit, scale, render order, conform, hidden, keep-hair) as TailorPatternDef XML you can back up, share, or submit to be baked into the mod. Import merges such a file back in."]
                = "    导出将你的逐物品调整（贴合、缩放、渲染顺序、贴合开关、隐藏、保留头发）写入 TailorPatternDef XML，你可以备份、分享或提交以烘焙到模组中。导入将这样的文件合并回来。",
            ["Export adjustments ("] = "导出调整（",
            ["No adjustments to export yet."] = "尚无调整可导出。",
            ["Exported "] = "已导出 ",
            [" item(s) to "] = " 个物品到 ",
            ["Import adjustments"] = "导入调整",
            ["No export files in "] = "导出文件夹中无文件：",
            ["Imported "] = "已导入 ",
            [" item(s)."] = " 个物品。",
            ["Import failed: "] = "导入失败：",
            ["Open export folder"] = "打开导出文件夹",

            // ═══════════════════════════════════════════
            // Window_Tailor.cs
            // ═══════════════════════════════════════════

            // --- 关闭按钮备用 ---
            ["x"] = "x",

            // --- APP 调谐器 ---
            ["APP tuner"] = "APP 调谐器",
            ["Open Apparel Paper Pattern's tuner window (the User Tuner THIGAPPE uses). Red items in the worn list are controlled by THIGAPPE/APP. Exempt them there to hand them to TailorMade."]
                = "打开 Apparel Paper Pattern 的调谐器窗口（THIGAPPE 使用的用户调谐器）。穿戴列表中的红色物品由 THIGAPPE/APP 控制。在那里豁免它们以交给 TailorMade。",

            // --- 窗口标题 ---
            ["Editing "] = "正在编辑 ",
            ["Select a single colonist."] = "请选择一个殖民者。",
            ["Select a single humanlike colonist to edit their apparel masks."] = "请选择一个类人殖民者以编辑其服装蒙版。",

            // --- 穿戴列表 ---
            ["WORN APPAREL (top = rendered on top)"] = "已穿戴服装（顶部 = 渲染在最上层）",

            // --- 排序按钮 ---
            ["Render one step up"] = "渲染上移一层",
            ["Render one step down"] = "渲染下移一层",

            // --- 可见/隐藏 ---
            ["Visible — click to hide on the pawn"] = "可见——点击在角色上隐藏",
            ["Hidden on the pawn — click to show"] = "在角色上已隐藏——点击显示",
            ["O"] = "O",
            ["-"] = "-",

            // --- APP 控制提示 ---
            ["Controlled by Apparel Paper Pattern (THIGAPPE), so TailorMade is deferring to its re-render. Exempt it in APP's tuner to hand it to TailorMade."]
                = "由 Apparel Paper Pattern（THIGAPPE）控制，TailorMade 正在交由它重新渲染。在 APP 调谐器中豁免它以交给 TailorMade。",

            // --- 右侧面板 ---
            ["Pick an apparel item on the left."] = "请在左侧选择一个服装物品。",

            // --- 偏移控制 ---
            ["Move up"] = "上移",
            ["Move down"] = "下移",
            ["Move left"] = "左移",
            ["Move right"] = "右移",
            ["Smaller"] = "缩小",
            ["Larger"] = "放大",
            ["X "] = "X ",
            ["   Y "] = "   Y ",
            ["   ×"] = "   ×",

            // --- 旋转 ---
            ["Rotate left"] = "向左旋转",
            ["Rotate right"] = "向右旋转",
            ["Rotate  "] = "旋转 ",
            ["°"] = "°",

            // --- 功能按钮 ---
            ["Always keep hair visible with this item"] = "始终在此物品下保持头发可见",
            ["Conform to body (ON). Turn OFF to keep protruding armor parts — pauldrons, hoods, flared coats — with no body warp or silhouette clip; only your manual nudge applies."]
                = "贴合身体（开启）。关闭以保留突出的装甲部件——肩甲、兜帽、展开的外套——不进行身体扭曲或轮廓裁剪；仅应用手动微调。",
            ["Link opposite facings (applies when you edit). ON: edits to this view also set the opposite view - north↔south, east↔west - as a mirror image (X offset and rotation flip sign; height and scale copy)."]
                = "关联相对朝向（编辑时生效）。开启：对此视图的编辑也设置相对视图——北↔南、东↔西——作为镜像（X 偏移和旋转翻转符号；高度和缩放复制）。",
            ["Reset THIS pawn's fit for this item (revert to the shared default / automatic)"]
                = "重置此角色对此物品的贴合（恢复为共享默认值/自动）",
            ["Copy as TailorPatternDef XML (clipboard)"] = "复制为 TailorPatternDef XML（剪贴板）",
            ["TailorPatternDef XML copied to clipboard."] = "TailorPatternDef XML 已复制到剪贴板。",
            ["Apply to ALL pawns: promote this pawn's fit to the shared default for this apparel (persists across saves)"]
                = "应用到所有角色：将此角色的贴合提升为此服装的共享默认值（跨存档持久化）",
            ["Applied to every pawn wearing "] = "已应用到每个穿戴",
            [" (saved as the shared default)."] = " 的角色（已保存为共享默认值）。",
            ["Hair·Conform·Link·Reset·Copy·All"] = "头发·贴合·关联·重置·复制·全部",

            // --- 重置所有 ---
            ["Reset all items"] = "重置所有物品",
            ["Reset every apparel adjustment (fit, scale, render order, conform) back to automatic? This affects all pawns and can't be undone."]
                = "将所有服装调整（贴合、缩放、渲染顺序、贴合开关）重置为自动？这将影响所有角色且无法撤销。",
            ["All apparel adjustments reset to automatic."] = "所有服装调整已重置为自动。",

            // --- 轮廓滑块 ---
            ["Clothing outline: "] = "服装轮廓：",
            ["Off"] = "关闭",

            // --- 底部提示 ---
            ["Drag to move, scroll to scale. Esc or X closes."] = "拖动以移动，滚动以缩放。Esc 或 X 关闭。",

            // --- IconBtn/IconToggle 备用 ---
            ["?"] = "？",
            ["H"] = "发",

            // --- BuildXml ---
            ["Human"] = "人类",

            // --- APP 控制的面板 ---
            ["Open APP tuner"] = "打开 APP 调谐器",

            // ═══════════════════════════════════════════
            // Patch_PlaySettings_TailorToggle.cs
            // ═══════════════════════════════════════════
            ["TailorMade: adjust apparel masks per item"] = "TailorMade：逐物品调整服装蒙版",
        };
    }
}
