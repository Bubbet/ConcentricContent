using BepInEx;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
// ReSharper disable SuspiciousTypeConversion.Global

namespace ConcentricContent
{
	[HarmonyPatch]
	public class HarmonyPatches
	{
		[HarmonyILManipulator]
		[HarmonyPatch(typeof(LoadoutPanelController.Row), nameof(LoadoutPanelController.Row.FromSkillSlot))]
		public static void FromSkillSlot(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchNewobj<LoadoutPanelController.Row>()
			);
			c.Emit(OpCodes.Ldarg_3);
			c.EmitDelegate<Func<string, GenericSkill, string>>((s, skill) =>
			{
				if (!Asset.TryGetAssetFromObject(skill.skillFamily, out ISkillFamily asset))
					return s;
				var nameToken = asset.GetNameToken(skill);
				return nameToken.IsNullOrWhiteSpace() ? s : nameToken;
			});
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateOverlayStates))]
		// ReSharper disable twice InconsistentNaming
		private static void CharacterModelUpdateOverlayStates(CharacterModel __instance, ref bool __result)
		{
			__result |= __instance.gameObject.GetOrAddComponent<ExtraOverlayTracker>().UpdateRequired(__instance);
		}
		
		
		[HarmonyPostfix, HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateOverlays))]
		// ReSharper disable once InconsistentNaming
		private static void CharacterModelUpdateOverlays(CharacterModel __instance)
		{
			foreach (var overlay in Asset.Overlays.Where(overlay =>
				         overlay.CheckEnabled(__instance) &&
				         __instance.activeOverlayCount < CharacterModel.maxOverlays))
			{
				__instance.currentOverlays[__instance.activeOverlayCount++] = Asset.OverlayMaterials[overlay];
			}
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateMaterials))]
		public static void InjectMaterial(ILContext il)
		{
			var c = new ILCursor(il);
			if (!c.TryGotoNext(MoveType.After,
				    x => x.MatchCallOrCallvirt<CharacterModel>(nameof(CharacterModel.UpdateRendererMaterials))))
			{
				LOG.LogError("Failed to match il in character model inject material.");
				return;
			}
			var injectionIndex = c.Index;
			var iIndex = -1;
			if (!c.TryGotoPrev(x => x.MatchLdloc(out iIndex))) return;
			c.Index = injectionIndex;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, iIndex);
			c.EmitDelegate<Action<CharacterModel, int>>((characterModel, i) =>
			{
				var baseRenderer = characterModel.baseRendererInfos[i];
				var swappedMaterial = Asset.MaterialSwaps.Where(overlay => overlay.CheckEnabled(characterModel, baseRenderer))
					.OrderBy(x => x.Priority).FirstOrDefault();
				if (swappedMaterial == null) return;
				characterModel.baseRendererInfos[i].renderer.material = Asset.MaterialSwapMaterials[swappedMaterial];
			});
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(LoadoutPanelController), nameof(LoadoutPanelController.Rebuild))]
		public static void FixLoadOutPanelControllerShowingHiddenSkillsInLoadOutTab(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel? brTarget = null;
			if (!c.TryGotoNext(MoveType.After, x => x.MatchBr(out brTarget), x => x.MatchLdloc(out _),
				    x => x.MatchLdloc(out _), x => x.MatchCallOrCallvirt(out _), x => x.MatchStloc(out _)))
			{
				LOG.LogError("Failed to match il in load out panel hidden skills fix.");
				return;
			}

			
			c.Index--;
			c.Emit(OpCodes.Dup);
			c.Index++;
			c.Emit(OpCodes.Ldfld, typeof(GenericSkill).GetField(nameof(GenericSkill.hideInCharacterSelect)));
			var jumpTarget = c.DefineLabel();
			c.Emit(OpCodes.Brtrue, jumpTarget); // jump to where the index increases
			c.Goto(brTarget!.Target); // goto end of loop
			c.GotoPrev(x => x.MatchLdloc(out _), x => x.MatchLdcI4(1), x => x.MatchAdd(), x => x.MatchStloc(out _));
			c.MarkLabel(jumpTarget);
		}
	}

	public class ExtraOverlayTracker : MonoBehaviour
	{
		private readonly Dictionary<IOverlay, bool> wasEnabled = new Dictionary<IOverlay, bool>();

		public bool UpdateRequired(CharacterModel model)
		{
			var shouldUpdate = false;
			foreach (var overlay in Asset.Overlays)
			{
				var overlayEnabled = overlay.CheckEnabled(model);
				if (wasEnabled.TryGetValue(overlay, out var value) && overlayEnabled == value) continue;
				wasEnabled[overlay] = overlayEnabled;
				shouldUpdate = true;
			}
			return shouldUpdate;
		}
	}
}