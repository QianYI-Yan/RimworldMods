using System;
using UnityEngine;
using Verse;

namespace TailorMade
{
	// Token: 0x0200000E RID: 14
	public static class FlatScroll
	{
		// Token: 0x0600004D RID: 77 RVA: 0x00006932 File Offset: 0x00004B32
		public static float ViewWidth(Rect outRect, float contentH)
		{
			return (contentH > outRect.height) ? (outRect.width - 10f) : outRect.width;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00006954 File Offset: 0x00004B54
		public static void Begin(Rect outRect, ref Vector2 scroll, Rect viewRect)
		{
			Widgets.BeginScrollView(outRect, ref scroll, viewRect, false);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00006960 File Offset: 0x00004B60
		public static void End(Rect outRect, ref Vector2 scroll, Rect viewRect, int id)
		{
			Widgets.EndScrollView();
			float height = viewRect.height;
			bool flag = height <= outRect.height;
			if (flag)
			{
				bool flag2 = FlatScroll._dragId == id;
				if (flag2)
				{
					FlatScroll._dragging = false;
					FlatScroll._dragId = -1;
				}
				scroll.y = 0f;
			}
			else
			{
				float num = outRect.xMax - 10f + 3f;
				float num2 = height - outRect.height;
				float num3 = Mathf.Max(24f, outRect.height * (outRect.height / height));
				float num4 = ((num2 > 0f) ? Mathf.Clamp01(scroll.y / num2) : 0f);
				float num5 = outRect.y + num4 * (outRect.height - num3);
				Rect rect;
				rect..ctor(num - 3f, outRect.y, 10f, outRect.height);
				Rect rect2;
				rect2..ctor(num, num5, 4f, num3);
				Event current = Event.current;
				bool flag3 = Mouse.IsOver(rect);
				bool flag4 = FlatScroll._dragging && FlatScroll._dragId == id;
				bool flag5 = !flag4 && flag3 && current.type == null && current.button == 0 && !FlatScroll._dragging;
				if (flag5)
				{
					bool flag6 = rect2.Contains(current.mousePosition);
					if (flag6)
					{
						FlatScroll._dragOffset = current.mousePosition.y - num5;
					}
					else
					{
						float num6 = Mathf.Clamp(current.mousePosition.y - num3 * 0.5f, outRect.y, outRect.yMax - num3);
						scroll.y = (num6 - outRect.y) / Mathf.Max(1f, outRect.height - num3) * num2;
						FlatScroll._dragOffset = num3 * 0.5f;
					}
					FlatScroll._dragging = true;
					FlatScroll._dragId = id;
					flag4 = true;
					current.Use();
				}
				bool flag7 = flag4;
				if (flag7)
				{
					bool flag8 = current.type == 3;
					if (flag8)
					{
						float num7 = Mathf.Clamp(current.mousePosition.y - FlatScroll._dragOffset, outRect.y, outRect.yMax - num3);
						scroll.y = (num7 - outRect.y) / Mathf.Max(1f, outRect.height - num3) * num2;
						current.Use();
					}
					else
					{
						bool flag9 = current.type == 1;
						if (flag9)
						{
							FlatScroll._dragging = false;
							FlatScroll._dragId = -1;
							current.Use();
						}
					}
					num4 = ((num2 > 0f) ? Mathf.Clamp01(scroll.y / num2) : 0f);
					num5 = outRect.y + num4 * (outRect.height - num3);
					rect2..ctor(num, num5, 4f, num3);
				}
				Widgets.DrawBoxSolid(new Rect(num, outRect.y, 4f, outRect.height), new Color(1f, 1f, 1f, 0.05f));
				float num8 = (flag4 ? 0.45f : (flag3 ? 0.32f : 0.16f));
				Widgets.DrawBoxSolid(rect2, new Color(1f, 1f, 1f, num8));
			}
		}

		// Token: 0x04000051 RID: 81
		public const float BarW = 10f;

		// Token: 0x04000052 RID: 82
		private static bool _dragging;

		// Token: 0x04000053 RID: 83
		private static int _dragId = -1;

		// Token: 0x04000054 RID: 84
		private static float _dragOffset;
	}
}
