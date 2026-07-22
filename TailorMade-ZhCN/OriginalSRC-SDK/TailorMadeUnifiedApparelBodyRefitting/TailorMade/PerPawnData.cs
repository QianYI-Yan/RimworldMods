using System;
using System.Collections.Generic;
using Verse;

namespace TailorMade
{
	// Token: 0x02000028 RID: 40
	public class PerPawnData : IExposable
	{
		// Token: 0x060000D8 RID: 216 RVA: 0x0000DBEC File Offset: 0x0000BDEC
		public TailorAdjust Get(int pawnId, string defName)
		{
			bool flag = pawnId == 0 || GenText.NullOrEmpty(defName);
			TailorAdjust tailorAdjust;
			if (flag)
			{
				tailorAdjust = null;
			}
			else
			{
				Dictionary<string, TailorAdjust> dictionary;
				TailorAdjust tailorAdjust2;
				tailorAdjust = ((this.map.TryGetValue(pawnId, out dictionary) && dictionary.TryGetValue(defName, out tailorAdjust2)) ? tailorAdjust2 : null);
			}
			return tailorAdjust;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000DC34 File Offset: 0x0000BE34
		public TailorAdjust GetOrAdd(int pawnId, string defName, TailorAdjust seed)
		{
			bool flag = pawnId == 0 || GenText.NullOrEmpty(defName);
			TailorAdjust tailorAdjust;
			if (flag)
			{
				tailorAdjust = null;
			}
			else
			{
				Dictionary<string, TailorAdjust> dictionary;
				bool flag2 = !this.map.TryGetValue(pawnId, out dictionary);
				if (flag2)
				{
					dictionary = new Dictionary<string, TailorAdjust>();
					this.map[pawnId] = dictionary;
				}
				TailorAdjust tailorAdjust2;
				bool flag3 = !dictionary.TryGetValue(defName, out tailorAdjust2);
				if (flag3)
				{
					tailorAdjust2 = ((seed != null) ? seed.Clone() : new TailorAdjust());
					dictionary[defName] = tailorAdjust2;
				}
				tailorAdjust = tailorAdjust2;
			}
			return tailorAdjust;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000DCB8 File Offset: 0x0000BEB8
		public void Remove(int pawnId, string defName)
		{
			Dictionary<string, TailorAdjust> dictionary;
			bool flag = this.map.TryGetValue(pawnId, out dictionary);
			if (flag)
			{
				dictionary.Remove(defName);
				bool flag2 = dictionary.Count == 0;
				if (flag2)
				{
					this.map.Remove(pawnId);
				}
			}
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000DCFC File Offset: 0x0000BEFC
		public void ClearAll()
		{
			this.map.Clear();
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000DD0C File Offset: 0x0000BF0C
		public void ExposeData()
		{
			bool flag = Scribe.mode == 1;
			if (flag)
			{
				this.scribe = new List<PerPawnEntry>();
				foreach (KeyValuePair<int, Dictionary<string, TailorAdjust>> keyValuePair in this.map)
				{
					foreach (KeyValuePair<string, TailorAdjust> keyValuePair2 in keyValuePair.Value)
					{
						bool flag2 = keyValuePair2.Value != null;
						if (flag2)
						{
							this.scribe.Add(new PerPawnEntry
							{
								pawnId = keyValuePair.Key,
								defName = keyValuePair2.Key,
								adj = keyValuePair2.Value
							});
						}
					}
				}
			}
			Scribe_Collections.Look<PerPawnEntry>(ref this.scribe, "entries", 2, Array.Empty<object>());
			bool flag3 = Scribe.mode == 2;
			if (flag3)
			{
				this.map.Clear();
				bool flag4 = this.scribe != null;
				if (flag4)
				{
					foreach (PerPawnEntry perPawnEntry in this.scribe)
					{
						bool flag5 = ((perPawnEntry != null) ? perPawnEntry.adj : null) == null || GenText.NullOrEmpty(perPawnEntry.defName) || perPawnEntry.pawnId == 0;
						if (!flag5)
						{
							Dictionary<string, TailorAdjust> dictionary;
							bool flag6 = !this.map.TryGetValue(perPawnEntry.pawnId, out dictionary);
							if (flag6)
							{
								dictionary = new Dictionary<string, TailorAdjust>();
								this.map[perPawnEntry.pawnId] = dictionary;
							}
							dictionary[perPawnEntry.defName] = perPawnEntry.adj;
						}
					}
				}
				this.scribe = null;
			}
		}

		// Token: 0x040000C8 RID: 200
		private Dictionary<int, Dictionary<string, TailorAdjust>> map = new Dictionary<int, Dictionary<string, TailorAdjust>>();

		// Token: 0x040000C9 RID: 201
		private List<PerPawnEntry> scribe;
	}
}
