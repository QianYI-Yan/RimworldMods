using System;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x02000009 RID: 9
	[StaticConstructorOnStartup]
	public static class TailorTex
	{
		// Token: 0x04000029 RID: 41
		public static readonly Texture2D Toggle = ContentFinder<Texture2D>.Get("UI/TailorMade_Toggle", false);

		// Token: 0x0400002A RID: 42
		public static readonly Texture2D ArrowUp = ContentFinder<Texture2D>.Get("UI/Tailor/ArrowUp", false);

		// Token: 0x0400002B RID: 43
		public static readonly Texture2D ArrowDown = ContentFinder<Texture2D>.Get("UI/Tailor/ArrowDown", false);

		// Token: 0x0400002C RID: 44
		public static readonly Texture2D ArrowLeft = ContentFinder<Texture2D>.Get("UI/Tailor/ArrowLeft", false);

		// Token: 0x0400002D RID: 45
		public static readonly Texture2D ArrowRight = ContentFinder<Texture2D>.Get("UI/Tailor/ArrowRight", false);

		// Token: 0x0400002E RID: 46
		public static readonly Texture2D Plus = ContentFinder<Texture2D>.Get("UI/Tailor/Plus", false);

		// Token: 0x0400002F RID: 47
		public static readonly Texture2D Minus = ContentFinder<Texture2D>.Get("UI/Tailor/Minus", false);

		// Token: 0x04000030 RID: 48
		public static readonly Texture2D ResetIco = ContentFinder<Texture2D>.Get("UI/Tailor/Reset", false);

		// Token: 0x04000031 RID: 49
		public static readonly Texture2D CopyIco = ContentFinder<Texture2D>.Get("UI/Tailor/Copy", false);

		// Token: 0x04000032 RID: 50
		public static readonly Texture2D HairIco = ContentFinder<Texture2D>.Get("UI/Tailor/Hair", false);

		// Token: 0x04000033 RID: 51
		public static readonly Texture2D EyeOpen = ContentFinder<Texture2D>.Get("UI/Tailor/EyeOpen", false);

		// Token: 0x04000034 RID: 52
		public static readonly Texture2D EyeClosed = ContentFinder<Texture2D>.Get("UI/Tailor/EyeClosed", false);

		// Token: 0x04000035 RID: 53
		public static readonly Texture2D RotL = ContentFinder<Texture2D>.Get("UI/Tailor/RotateLeft", false);

		// Token: 0x04000036 RID: 54
		public static readonly Texture2D RotR = ContentFinder<Texture2D>.Get("UI/Tailor/RotateRight", false);

		// Token: 0x04000037 RID: 55
		public static readonly Texture2D Conform = ContentFinder<Texture2D>.Get("UI/Tailor/Conform", false);

		// Token: 0x04000038 RID: 56
		public static readonly Texture2D LinkIco = ContentFinder<Texture2D>.Get("UI/Tailor/Link", false);

		// Token: 0x04000039 RID: 57
		public static readonly Texture2D SaveIco = ContentFinder<Texture2D>.Get("UI/Tailor/Save", false);

		// Token: 0x0400003A RID: 58
		public static readonly Texture2D Close = ContentFinder<Texture2D>.Get("UI/Tailor/Close", false);

		// Token: 0x0400003B RID: 59
		public static readonly Texture2D[] Face = new Texture2D[]
		{
			ContentFinder<Texture2D>.Get("UI/Tailor/FaceN", false),
			ContentFinder<Texture2D>.Get("UI/Tailor/FaceE", false),
			ContentFinder<Texture2D>.Get("UI/Tailor/FaceS", false),
			ContentFinder<Texture2D>.Get("UI/Tailor/FaceW", false)
		};
	}
}
