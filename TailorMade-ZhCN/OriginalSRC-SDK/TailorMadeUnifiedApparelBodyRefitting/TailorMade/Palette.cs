using System;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000023 RID: 35
	public static class Palette
	{
		// Token: 0x060000BF RID: 191 RVA: 0x0000D0CE File Offset: 0x0000B2CE
		public static Color FromHex(int hex)
		{
			return new Color((float)((hex >> 16) & 255) / 255f, (float)((hex >> 8) & 255) / 255f, (float)(hex & 255) / 255f);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x0000D104 File Offset: 0x0000B304
		public static void DrawCard(Rect r)
		{
			Widgets.DrawBoxSolid(r, Palette.PanelBG);
			GUI.color = Palette.BGL;
			Widgets.DrawBox(r, 1, null);
			GUI.color = Color.white;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000D132 File Offset: 0x0000B332
		public static void DrawBackdrop(Rect r)
		{
			Widgets.DrawBoxSolid(r, Palette.BGD);
			GUI.color = Palette.BGL;
			Widgets.DrawBox(r, 1, null);
			GUI.color = Color.white;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x0000D160 File Offset: 0x0000B360
		public static void Header(Rect r, string label)
		{
			GameFont font = Text.Font;
			TextAnchor anchor = Text.Anchor;
			Text.Font = 1;
			Text.Anchor = 6;
			GUI.color = Palette.Accent;
			Widgets.Label(new Rect(r.x, r.y, r.width, r.height), label);
			GUI.color = new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.35f);
			Widgets.DrawLineHorizontal(r.x, r.yMax, r.width);
			GUI.color = Color.white;
			Text.Anchor = anchor;
			Text.Font = font;
		}

		// Token: 0x040000AA RID: 170
		public static readonly Color BG = Palette.FromHex(1382685);

		// Token: 0x040000AB RID: 171
		public static readonly Color BGL = Palette.FromHex(3093303);

		// Token: 0x040000AC RID: 172
		public static readonly Color BGD = Palette.FromHex(921619);

		// Token: 0x040000AD RID: 173
		public static readonly Color Stat = Palette.FromHex(14935011);

		// Token: 0x040000AE RID: 174
		public static readonly Color TextDim = new Color(0.62f, 0.65f, 0.7f);

		// Token: 0x040000AF RID: 175
		public static readonly Color Accent = new Color(0.45f, 0.75f, 1f);

		// Token: 0x040000B0 RID: 176
		public static readonly Color Warn = new Color(0.95f, 0.38f, 0.36f);

		// Token: 0x040000B1 RID: 177
		public static readonly Color PanelBG = Color.Lerp(Palette.BG, Palette.BGL, 0.22f);
	}
}
