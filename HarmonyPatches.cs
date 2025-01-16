using BepInEx;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Skills;
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
				if (!Concentric.TryGetAssetFromObject(skill.skillFamily, out ISkillFamily asset))
					return s;
				var nameToken = asset.GetNameToken(skill);
				return nameToken.IsNullOrWhiteSpace() ? s : nameToken;
			});
		}

		#region Viewables/Saving/Loading Overrides

		[HarmonyILManipulator]
		[HarmonyPatch(typeof(Loadout), nameof(Loadout.GenerateViewables))]
		public static void ViewableNameOverride(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchCallOrCallvirt<GenericSkill>("get_" + nameof(GenericSkill.skillFamily)));
			var lastIndex = c.Index;
			c.GotoPrev(x => x.MatchLdloc(out _), x => x.MatchLdloc(out _));
			var firstIndex = c.Index;
			c.GotoNext(MoveType.After,
				x => x.MatchCallOrCallvirt(typeof(SkillCatalog), nameof(SkillCatalog.GetSkillFamilyName)));

			for (var i = firstIndex; i < lastIndex; i++)
			{
				var instr = c.Instrs[i];
				c.Emit(instr.OpCode, instr.Operand);
			}

			c.EmitDelegate<Func<string, GenericSkill, string>>(NodeNameOverride);
		}

		private static string NodeNameOverride(string s, GenericSkill genericSkill)
		{
			if (!Concentric.TryGetAssetFromObject(genericSkill.skillFamily, out ISkillFamily asset)) return s;
			var nameToken = asset.GetViewableNameOverride(genericSkill);
			return nameToken.IsNullOrWhiteSpace() ? s : nameToken;
		}

		[HarmonyILManipulator]
		[HarmonyPatch(typeof(Loadout.BodyLoadoutManager.BodyLoadout), nameof(Loadout.BodyLoadoutManager.BodyLoadout.ToXml))]
		public static void ViewableNameOverrideToXml(ILContext il)
		{
			var c = new ILCursor(il);
			var skillCatalog = typeof(SkillCatalog);
			c.GotoNext(x => x.MatchCallOrCallvirt(skillCatalog, nameof(SkillCatalog.GetSkillFamily)));
			var ptrIndex = -1;
			var iIndex = -1;
			c.GotoPrev(x => x.MatchLdloc(out ptrIndex),
				x => x.MatchLdfld<Loadout.BodyLoadoutManager.BodyInfo>(nameof(Loadout.BodyLoadoutManager.BodyInfo
					.skillFamilyIndices)), x => x.MatchLdloc(out iIndex));

			c.GotoNext(MoveType.After,
				x => x.MatchCallOrCallvirt(skillCatalog, nameof(SkillCatalog.GetSkillFamilyName)));
			c.Emit(OpCodes.Ldloc, ptrIndex);
			c.Emit(OpCodes.Ldfld,
				typeof(Loadout.BodyLoadoutManager.BodyInfo).GetField(nameof(Loadout.BodyLoadoutManager.BodyInfo
					.prefabSkillSlots)));
			c.Emit(OpCodes.Ldloc, iIndex);
			c.EmitDelegate<Func<string, GenericSkill[], int, string>>((s, info, i) => NodeNameOverride(s, info[i]));
		}

		[HarmonyILManipulator]
		[HarmonyPatch(typeof(Loadout.BodyLoadoutManager.BodyLoadout), nameof(Loadout.BodyLoadoutManager.BodyLoadout.FromXml))]
		public static void ViewableNameOverrideFromXml(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x =>
				x.MatchLdsfld(typeof(Loadout.BodyLoadoutManager), nameof(Loadout.BodyLoadoutManager.allBodyInfos)));
			var ptrIndex = -1;
			c.GotoNext(x => x.MatchStloc(out ptrIndex));

			c.GotoNext(MoveType.After,x => x.MatchBrfalse(out _), x => x.MatchLdloc(out _), x => x.MatchLdloca(out _), x => true, x => x.MatchStloc(out _));
			var emitPoint = c.Index - 1;
			var familyTextIndex = -1;
			c.GotoPrev(x => x.MatchLdloc(out familyTextIndex), x => x.MatchLdloca(out _));
			c.Index = emitPoint;
			c.Emit(OpCodes.Ldloc, familyTextIndex);
			c.Emit(OpCodes.Ldloc, ptrIndex);
			c.EmitDelegate(FromXmlNodeOverride);
		}

		private static int FromXmlNodeOverride(int i, string s, ref Loadout.BodyLoadoutManager.BodyInfo info)
		{
			var k = -1;
			foreach (var skill in info.prefabSkillSlots)
			{
				k++;
				var over = NodeNameOverride("", skill);
				if (over.IsNullOrWhiteSpace()) continue;
				if (s == over) return k;
			}

			return i;
		}

		#endregion


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
			foreach (var overlay in Concentric.Overlays.Where(overlay =>
				         overlay.CheckEnabled(__instance) &&
				         __instance.activeOverlayCount < CharacterModel.maxOverlays))
			{
				__instance.currentOverlays[__instance.activeOverlayCount++] = Concentric.OverlayMaterials[overlay];
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
				var swappedMaterial = Concentric.MaterialSwaps
					.Where(overlay => overlay.CheckEnabled(characterModel, baseRenderer))
					.OrderBy(x => x.Priority).FirstOrDefault();
				if (swappedMaterial == null) return;
				characterModel.baseRendererInfos[i].renderer.material =
					Concentric.MaterialSwapMaterials[swappedMaterial];
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
			c.EmitDelegate<Func<GenericSkill, bool>>(skill =>
				Concentric.TryGetAssetFromObject(skill.skillFamily, out ISkillFamily asset) &&
				asset.HiddenFromCharacterSelect);
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
			foreach (var overlay in Concentric.Overlays)
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