using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ConcentricContent
{
	[HarmonyPatch]
	public static class ExtraHealthBarSegments
	{
		internal static List<Type> _barDataTypes = new();

		public static void AddType<T>() where T : BarData, new()
		{
			_barDataTypes.Add(typeof(T));
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Awake))]
		// ReSharper disable once InconsistentNaming
		public static void AddTracker(HealthBar __instance)
		{
			__instance.gameObject.AddComponent<ExtraHealthBarInfoTracker>();
		}

		[HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.CheckInventory))]
		// ReSharper disable once InconsistentNaming
		public static void CheckInventory(HealthBar __instance)
		{
			var tracker = __instance.GetComponent<ExtraHealthBarInfoTracker>();
			if (!tracker) return;
			var source = __instance.source;
			if (!source) return;
			var body = source.body;
			if (!body) return;
			var inv = body.inventory;
			if (!inv) return;
			tracker.CheckInventory(inv, body, source);
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.UpdateBarInfos))]
		// ReSharper disable once InconsistentNaming
		public static void UpdateInfos(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.After,
				x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.GetHealthBarValues)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<HealthComponent.HealthBarValues, HealthBar, HealthComponent.HealthBarValues>>(
				(values, bar) =>
				{
					var tracker = bar.GetComponent<ExtraHealthBarInfoTracker>();
					return tracker.UpdateInfo(ref values);
				});
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.ApplyBars))]
		public static void ApplyBar(ILContext il)
		{
			var c = new ILCursor(il);

			var cls = -1;
			FieldReference? fld = null;
			c.GotoNext(
				x => x.MatchLdloca(out cls),
				x => x.MatchLdcI4(0),
				x => x.MatchStfld(out fld)
			);

			c.GotoNext(MoveType.After,
				x => x.MatchCallOrCallvirt<HealthBar.BarInfoCollection>(nameof(HealthBar.BarInfoCollection
					.GetActiveCount))
			);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, HealthBar, int>>((i, bar) =>
			{
				var tracker = bar.GetComponent<ExtraHealthBarInfoTracker>();
				i += tracker.BarInfos.Count(x => x.Info.enabled);
				return i;
			});
			c.Index = il.Instrs.Count - 2;
			c.Emit(OpCodes.Ldloca, cls);
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloca, cls);
			c.Emit(OpCodes.Ldfld, fld);
			c.EmitDelegate<Func<HealthBar, int, int>>((bar, i) =>
			{
				var tracker = bar.GetComponent<ExtraHealthBarInfoTracker>();
				tracker.ApplyBar(ref i);
				return i;
			});
			c.Emit(OpCodes.Stfld, fld);
		}

		[RequireComponent(typeof(HealthBar))]
		public class ExtraHealthBarInfoTracker : MonoBehaviour
		{
			public List<BarData> BarInfos = null!;

			private HealthBar? _healthBar;
			public HealthBar HealthBar
			{
				get
				{
					if (_healthBar == null || !_healthBar) _healthBar = GetComponent<HealthBar>();
					return _healthBar;
				}
			}

			private Material? _defaultMaterial;
			private Material DefaultMaterial
			{
				get
				{
					if (_defaultMaterial == null || !_defaultMaterial)
						_defaultMaterial = HealthBar.barAllocator.elementPrefab.GetComponent<Image>().material;
					return _defaultMaterial;
				}
			}

			public void CheckInventory(Inventory inv, CharacterBody characterBody, HealthComponent healthComponent)
			{
				foreach (var barInfo in BarInfos)
				{
					barInfo.CheckInventory(ref barInfo.Info, inv, characterBody, healthComponent);
				}
			}

			public HealthComponent.HealthBarValues UpdateInfo(ref HealthComponent.HealthBarValues barValues)
			{
				if (!HealthBar.source) return barValues;
				foreach (var barInfo in BarInfos) barInfo.UpdateInfo(ref barInfo.Info, ref barValues);
				return barValues;
			}

			public void ApplyBar(ref int i)
			{
				foreach (var image in HealthBar.barAllocator.elements)
				{
					if (image.material != DefaultMaterial)
						image.material = DefaultMaterial;
				}

				foreach (var barInfo in BarInfos)
				{
					ref var info = ref barInfo.Info;
					if (!info.enabled) continue;

					var image = HealthBar.barAllocator.elements[i];
					barInfo.ApplyBar(ref barInfo.Info, image, ref i);
				}
			}

			public void Awake()
			{
				BarInfos = _barDataTypes
					.Select(dataType => ((BarData)Activator.CreateInstance(dataType)).Init(this)).ToList();
			}
		}
	}

	public abstract class BarData
	{
		public ExtraHealthBarSegments.ExtraHealthBarInfoTracker Tracker = null!;
		public HealthBar.BarInfo Info;
		public HealthBarStyle.BarStyle? CachedStyle;

		public abstract HealthBarStyle.BarStyle GetStyle();

		public virtual void UpdateInfo(ref HealthBar.BarInfo inf, ref HealthComponent.HealthBarValues healthBarValues)
		{
			CachedStyle ??= GetStyle();
			var style = CachedStyle.Value;

			inf.enabled &= style.enabled;
			inf.color = style.baseColor;
			inf.imageType = style.imageType;
			inf.sprite = style.sprite;
			inf.sizeDelta = style.sizeDelta;
		}

		public virtual void CheckInventory(ref HealthBar.BarInfo inf, Inventory inventory, CharacterBody characterBody,
			HealthComponent healthComponent)
		{
		}

		public virtual void ApplyBar(ref HealthBar.BarInfo inf, Image image, ref int i)
		{
			image.type = inf.imageType;
			image.sprite = inf.sprite;
			image.color = inf.color;

			var rectTransform = (RectTransform)image.transform;
			rectTransform.anchorMin = new Vector2(inf.normalizedXMin, 0f);
			rectTransform.anchorMax = new Vector2(inf.normalizedXMax, 1f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.sizeDelta = new Vector2(inf.sizeDelta * 0.5f + 1f, inf.sizeDelta + 1f);

			i++;
		}

		public virtual BarData Init(ExtraHealthBarSegments.ExtraHealthBarInfoTracker extraHealthBarInfoTracker)
		{
			Tracker = extraHealthBarInfoTracker;
			return this;
		}
	}
}