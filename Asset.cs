using System.Reflection;
using BepInEx;
using EntityStates;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CodedAssets
{
	[HarmonyPatch]
	public abstract class Asset
	{
		public static Dictionary<string, object> Objects = new Dictionary<string, object>();
		public static Dictionary<Type, Asset> Assets = new Dictionary<Type, Asset>();
		public static Dictionary<object, Asset> ObjectToAssetMap = new Dictionary<object, Asset>();
		public static List<IOverlay> Overlays = new List<IOverlay>();
		public static List<IMaterialSwap> MaterialSwaps = new List<IMaterialSwap>();

		public static ContentPack BuildContentPack()
		{
			var result = new ContentPack();

			var assets = Assembly.GetCallingAssembly().GetTypes()
				.Where(x => typeof(Asset).IsAssignableFrom(x) && !x.IsAbstract);
			Assets = assets.ToDictionary(x => x, x => (Asset)Activator.CreateInstance(x));
			foreach (var asset in Assets.Values) asset.Initialize();

			var instances = Assets.Values;
			Overlays.AddRange(instances.Where(x => x is IOverlay).Cast<IOverlay>());
			MaterialSwaps.AddRange(instances.Where(x => x is IMaterialSwap).Cast<IMaterialSwap>());
			var entityStates = instances.Where(x => x is IEntityStates).SelectMany(x =>
				(Type[])Objects.GetOrSet(x.GetType().Assembly.FullName + "_" + x.GetType().FullName + "_EntityStates",
					() => ((IEntityStates)x).GetEntityStates()));

			result.unlockableDefs.Add(instances.Where(x => x is IUnlockable).Select(x => (UnlockableDef)x).ToArray());
			result.itemDefs.Add(instances.Where(x => x is IItem).Select(x => (ItemDef)x).ToArray());
			result.buffDefs.Add(instances.Where(x => x is IBuff).Select(x => (BuffDef)x).ToArray());
			result.skillDefs.Add(instances.Where(x => x is ISkill).Select(x => (SkillDef)x).ToArray());
			result.entityStateTypes.Add(instances.Where(x => x is ISkill)
				.SelectMany(x =>
					(Type[])Objects[
						x.GetType().Assembly.FullName + "_" + x.GetType().FullName + "_" + nameof(ISkill) +
						"_EntityStates"])
				.Concat(entityStates).Distinct().ToArray());
			result.skillFamilies.Add(instances.Where(x => x is ISkillFamily).Select(x => (SkillFamily)x).ToArray());
			result.networkedObjectPrefabs.Add(instances.Where(x => x is INetworkedObject)
				.Select(x => (GameObject)GetObjectOrThrow<INetworkedObject>(x))
				.ToArray());
			result.bodyPrefabs.Add(instances.Where(x => x is IBody).Select(x => (GameObject)GetObjectOrThrow<IBody>(x))
				.ToArray());
			result.survivorDefs.Add(instances.Where(x => x is ISurvivor).Select(x => (SurvivorDef)x).ToArray());
			result.projectilePrefabs.Add(instances.Where(x => x is IProjectile)
				.Select(x => (GameObject)GetObjectOrThrow<IProjectile>(x)).ToArray());
			result.effectDefs.Add(instances.Where(x => x is IEffect)
				.Select(x => new EffectDef((GameObject)GetObjectOrThrow<IEffect>(x))).ToArray());
			result.masterPrefabs.Add(instances.Where(x => x is IMaster)
				.Select(x => (GameObject)GetObjectOrThrow<IMaster>(x))
				.ToArray());
			result.musicTrackDefs.Add(instances.Where(x => x is IMusicTrack).Select(x => (MusicTrackDef)x).ToArray());

			/* TODO
			 * gameModePrefabs
			 * sceneDefs
			 * itemTierDefs
			 * itemRelationshipProviders
			 * itemRelationshipTypes
			 * equipmentDefs
			 * eliteDefs
			 * artifactDefs
			 * surfaceDefs
			 * networkSoundEventDefs
			 * gameEndingDefs
			 * entityStateConfigurations // kinda pointless?
			 * expansionDefs
			 * entitlementDefs // also kinda pointless?
			 * miscPickupDefs
			 */
			
			return result;
		}

		public static bool TryGetAsset<T>(out T asset) where T : Asset
		{
			if (Assets.TryGetValue(typeof(T), out var foundAsset))
			{
				asset = (T)foundAsset;
				return true;
			}

			asset = default!;
			return false;
		}

		public static bool TryGetAssetFromObject<T>(object obj, out T asset)
		{
			var found = ObjectToAssetMap.TryGetValue(obj, out var assetObj);
			asset = assetObj is T ? (T)(object)assetObj : default!;
			return found;
		}

		public static bool TryGetAsset<T, T2>(out T asset) where T : Asset, T2 => TryGetAsset(out asset);

		public static T GetAsset<T>() where T : Asset => (T)GetAsset(typeof(T));

		public static T GetAsset<T, T2>() where T : Asset, T2 => GetAsset<T>();

		public static object GetAsset(Type assetType) =>
			// TODO do some warning about the type not being in assets instead of blowing up without saying why
			Assets[assetType];

		public static bool TryGetGameObject<T, T2>(out GameObject asset) where T2 : IGameObject where T : Asset, T2
		{
			try
			{
				asset = (GameObject)GetObjectOrThrow<T2>(Assets[typeof(T)]);
				return true;
			}
			catch (Exception)
			{
				asset = default!;
				return false;
			}
		}

		public static GameObject GetGameObject<T, T2>() where T2 : IGameObject where T : Asset, T2 =>
			GetGameObject(typeof(T), typeof(T2));

		internal static GameObject GetGameObject(Type callingType, Type targetType) =>
			(GameObject)GetObjectOrThrow(Assets[callingType], targetType);

		public static object GetObjectOrThrow<T>(Asset asset) => GetObjectOrThrow(asset, typeof(T));

		private static object GetObjectOrThrow(Asset asset, Type targetType)
		{
			var assetType = asset.GetType();
			var name = assetType.FullName;
			var targetTypeName = targetType.Name;
			var key = assetType.Assembly.FullName + "_" + name + "_" + targetTypeName;
			var notOfType = new AssetTypeInvalidException($"{name} is not of type {targetTypeName}");
			if (Objects.TryGetValue(key, out var result))
			{
				return result!;
			}

			object? returnedObject = null;
			switch (targetTypeName)
			{
				case nameof(IUnlockable):
					var unlockable = (asset as IUnlockable)?.BuildObject() ?? throw notOfType;
					unlockable.cachedName = name + nameof(UnlockableDef);
					returnedObject = unlockable;
					break;
				case nameof(IItem):
					var item = (asset as IItem)?.BuildObject() ?? throw notOfType;
					item.name = name + nameof(ItemDef);
					returnedObject = item;
					break;
				case nameof(ISkill):
					var skill = (asset as ISkill)?.BuildObject() ?? throw notOfType;
					var entityStates = ((ISkill)asset).GetEntityStates();
					Objects[key + "_EntityStates"] = entityStates;
					// ObjectToAssetMap ??
					skill.skillName = name + nameof(SkillDef);
					skill.activationState = new SerializableEntityStateType(entityStates[0]);
					returnedObject = skill;
					break;
				case nameof(ISurvivor):
					var survivor = (asset as ISurvivor)?.BuildObject() ?? throw notOfType;
					survivor.cachedName = name + nameof(SurvivorDef);
					returnedObject = survivor;
					break;
				case nameof(IEffect):
					var effect = (asset as IEffect)?.BuildObject() ?? throw notOfType;
					if (!effect.GetComponent<VFXAttributes>())
					{
						var attributes = effect.AddComponent<VFXAttributes>();
						attributes.vfxPriority = VFXAttributes.VFXPriority.Always;
						attributes.DoNotPool = true;
					}

					if (!effect.GetComponent<EffectComponent>())
					{
						var comp = effect.AddComponent<EffectComponent>();
						comp.applyScale = false;
						comp.parentToReferencedTransform = true;
						comp.positionAtReferencedTransform = true;
					}

					returnedObject = effect;
					break;
				case nameof(IVariant):
					var variant = (asset as IVariant)?.BuildObject();
					if (variant is null)
					{
						var skillDef = (SkillDef)asset;
						variant = new SkillFamily.Variant
						{
							skillDef = skillDef, viewableNode = new ViewablesCatalog.Node(skillDef.skillName, false)
						};
					}

					returnedObject = variant;
					break;
				case nameof(ISkillFamily):
					var familyAsset = asset as ISkillFamily ?? throw notOfType;
					var family = familyAsset.BuildObject();
					// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
					if (family is null)
					{
						family = ScriptableObject.CreateInstance<SkillFamily>();
						family.variants = familyAsset.GetSkillAssets()
							.Select(x => (SkillFamily.Variant)Assets[x]).ToArray();
					}

					returnedObject = family;
					break;
				case nameof(IModel):
					var modelAsset = asset as IModel ?? throw notOfType;
					returnedObject = modelAsset.BuildObject();
					Objects[key] = returnedObject;
					ObjectToAssetMap[returnedObject] = asset;
					var skinController = ((GameObject)returnedObject).GetOrAddComponent<ModelSkinController>();
					skinController.skins = modelAsset.GetSkins().Select(x => (SkinDef)x).ToArray();
					break;
				default:
					returnedObject = targetType.GetMethod("BuildObject")?.Invoke(asset, null) ?? throw notOfType;
					break;
				/*
				case nameof(ISkillFamily):
				    var skillFamily = (asset as ISkillFamily)?.BuildObject() ?? throw notOfType;
				    returnedObject = skillFamily;
				    break;
				case nameof(IVariant):
				    var variant = (asset as IVariant)?.BuildObject() ?? throw notOfType;
				    returnedObject = variant;
				    break;
				case nameof(INetworkedObject):
				    var networkedObject = (asset as INetworkedObject)?.BuildObject() ?? throw notOfType;
				    returnedObject = networkedObject;
				    break;
				case nameof(IProjectile):
				    var projectile = (asset as IProjectile)?.BuildObject() ?? throw notOfType;
				    returnedObject = projectile;
				    break;
				case nameof(IProjectileGhost):
				    var ghost = (asset as IProjectileGhost)?.BuildObject() ?? throw notOfType;
				    returnedObject = ghost;
				    break;
				case nameof(IModel):
				    var model = (asset as IModel)?.BuildObject() ?? throw notOfType;
				    returnedObject = model;
				    break;
				case nameof(IBodyDisplay):
				    var display = (asset as IBodyDisplay)?.BuildObject() ?? throw notOfType;
				    returnedObject = display;
				    break;
				case nameof(IBody):
				    var body = (asset as IBody)?.BuildObject() ?? throw notOfType;
				    returnedObject = body;
				    break;
				    */
			}

			var returnedType = returnedObject.GetType();
			var scriptableObject = typeof(ScriptableObject);
			var cachedName = returnedType.GetProperty("cachedName");
			var nameProperty = scriptableObject.GetProperty("name")!.GetSetMethod();
			var objectName = (assetType.Name  + "_" + targetTypeName).Replace(".", "_");
			cachedName?.GetSetMethod().Invoke(returnedObject, new object[] { objectName });
			if (cachedName is null && scriptableObject.IsAssignableFrom(returnedType))
			{
				nameProperty.Invoke(returnedObject, new object[] { objectName });
			}

			Objects[key] = returnedObject ??
			               throw new InvalidOperationException(
				               $"How did you get here, maybe {targetTypeName} isn't a asset?");
			ObjectToAssetMap[returnedObject] = asset;
			return returnedObject;
		}

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
				if (!TryGetAssetFromObject(skill.skillFamily, out ISkillFamily asset))
					return s;
				var nameToken = asset.GetNameToken(skill);
				return nameToken.IsNullOrWhiteSpace() ? s : nameToken;
			});
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateOverlays))]
		private static void CharacterModelUpdateOverlays(CharacterModel __instance)
		{
			foreach (var overlay in Overlays.Where(overlay =>
				         overlay.CheckEnabled(__instance) &&
				         __instance.activeOverlayCount < CharacterModel.maxOverlays))
			{
				__instance.currentOverlays[__instance.activeOverlayCount++] =
					(Material)GetObjectOrThrow<IOverlay>((Asset)overlay);
			}
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.UpdateMaterials))]
		public static void InjectMaterial(ILContext il)
		{
			var c = new ILCursor(il);
			if (!c.TryGotoNext(MoveType.After,
				    x => x.MatchCallOrCallvirt<CharacterModel>(nameof(CharacterModel.UpdateRendererMaterials))))
			{
				log.LogError("Failed to match il in charactermodel inject material.");
				return;
			}
			var injectionIndex = c.Index;
			var iIndex = -1;
			if (!c.TryGotoPrev(x => x.MatchLdloc(out iIndex))) return;
			c.Index = injectionIndex;
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc, iIndex);
			c.EmitDelegate<Action<CharacterModel, int>>((__instance, i) =>
			{
				var baseRenderer = __instance.baseRendererInfos[i];
				var swappedMaterial = MaterialSwaps.Where(overlay => overlay.CheckEnabled(__instance, baseRenderer))
					.OrderBy(x => x.Priority).FirstOrDefault();
				if (swappedMaterial != null)
					__instance.baseRendererInfos[i].renderer.material =
						(Material)GetObjectOrThrow<IMaterialSwap>((Asset)swappedMaterial);
			});
		}

		[HarmonyILManipulator, HarmonyPatch(typeof(LoadoutPanelController), nameof(LoadoutPanelController.Rebuild))]
		public static void FixLoadOutPanelControllerShowingHiddenSkillsInLoadOutTab(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel? jumpTarget = null;
			if (!c.TryGotoNext(MoveType.After, x => x.MatchBr(out jumpTarget), x => x.MatchLdloc(out _),
				    x => x.MatchLdloc(out _), x => x.MatchCallOrCallvirt(out _), x => x.MatchStloc(out _)))
			{
				log.LogError("Failed to match il in loadout panel hidden skills fix.");
				return;
			}

			c.Index--;
			c.Emit(OpCodes.Dup);
			c.Index++;
			c.Emit(OpCodes.Ldfld, typeof(GenericSkill).GetField(nameof(GenericSkill.hideInCharacterSelect)));
			c.Emit(OpCodes.Brtrue,
				jumpTarget!.Target.Previous.Previous.Previous.Previous); // jump to where the index increases
		}

		public static explicit operator ItemDef(Asset asset) => (ItemDef)GetObjectOrThrow<IItem>(asset);
		public static implicit operator ItemIndex(Asset asset) => ((ItemDef)GetObjectOrThrow<IItem>(asset)).itemIndex;

		public static implicit operator UnlockableDef(Asset asset) =>
			(UnlockableDef)GetObjectOrThrow<IUnlockable>(asset);

		public static explicit operator BuffDef(Asset asset) => (BuffDef)GetObjectOrThrow<IBuff>(asset);
		public static implicit operator BuffIndex(Asset asset) => ((BuffDef)GetObjectOrThrow<IBuff>(asset)).buffIndex;

		public static implicit operator BodyIndex(Asset asset) =>
			((GameObject)GetObjectOrThrow<IBody>(asset)).GetComponent<CharacterBody>().bodyIndex;

		public static implicit operator Material(Asset asset) => (Material)GetObjectOrThrow<IMaterial>(asset);

		public static implicit operator SurvivorDef(Asset asset) => (SurvivorDef)GetObjectOrThrow<ISurvivor>(asset);

		public static implicit operator SkinDef(Asset asset) => (SkinDef)GetObjectOrThrow<ISkin>(asset);

		public static implicit operator SkillDef(Asset asset) => (SkillDef)GetObjectOrThrow<ISkill>(asset);

		public static implicit operator MusicTrackDef(Asset asset) =>
			(MusicTrackDef)GetObjectOrThrow<IMusicTrack>(asset);


		public static implicit operator SkillFamily(Asset asset) => (SkillFamily)GetObjectOrThrow<ISkillFamily>(asset);

		public static implicit operator SkillFamily.Variant(Asset asset) =>
			(SkillFamily.Variant)GetObjectOrThrow<IVariant>(asset);

		public virtual void Initialize() { }
	}

	public class AssetTypeInvalidException : Exception
	{
		public AssetTypeInvalidException(string message) : base(message)
		{
		}
	}

	public static class AssetExtensionMethods
	{
		public static GameObject GetNetworkedObject<T>(this T obj) where T : Asset, INetworkedObject =>
			Asset.GetGameObject(obj.GetType(), typeof(INetworkedObject));

		public static GameObject GetProjectile<T>(this T obj) where T : Asset, IProjectile =>
			Asset.GetGameObject(obj.GetType(), typeof(IProjectile));

		public static GameObject GetGhost<T>(this T obj) where T : Asset, IProjectileGhost =>
			Asset.GetGameObject(obj.GetType(), typeof(IProjectileGhost));

		public static GameObject GetEffect<T>(this T obj) where T : Asset, IEffect =>
			Asset.GetGameObject(obj.GetType(), typeof(IEffect));

		public static GameObject GetMaster<T>(this T obj) where T : Asset, IMaster =>
			Asset.GetGameObject(obj.GetType(), typeof(IMaster));

		public static GameObject GetBody<T>(this T obj) where T : Asset, IBody =>
			Asset.GetGameObject(obj.GetType(), typeof(IBody));

		public static GameObject GetBodyDisplay<T>(this T obj) where T : Asset, IBodyDisplay =>
			Asset.GetGameObject(obj.GetType(), typeof(IBodyDisplay));

		public static GameObject GetModel<T>(this T obj) where T : Asset, IModel =>
			Asset.GetGameObject(obj.GetType(), typeof(IModel));
	}

	public interface IGameObject
	{
		public virtual GameObject BuildObject() => null!;
	}

	public interface INetworkedObject : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IProjectile : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IProjectileGhost : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IEffect : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IMaster : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IBody : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IBodyDisplay : IGameObject
	{
		public new abstract GameObject BuildObject();
	}

	public interface IModel : IGameObject
	{
		public new abstract GameObject BuildObject();
		public abstract Asset[] GetSkins();
	}

	public interface ISurvivor
	{
		public abstract SurvivorDef BuildObject();
	}

	public interface ISkin
	{
		public abstract SkinDef BuildObject();

		public static void AddDefaults(ref SkinDef skinDef)
		{
			skinDef.baseSkins ??= Array.Empty<SkinDef>();
			skinDef.gameObjectActivations ??= Array.Empty<SkinDef.GameObjectActivation>();
			skinDef.meshReplacements ??= Array.Empty<SkinDef.MeshReplacement>();
			skinDef.minionSkinReplacements ??= Array.Empty<SkinDef.MinionSkinReplacement>();
			skinDef.projectileGhostReplacements ??= Array.Empty<SkinDef.ProjectileGhostReplacement>();
		}
	}

	public interface IItem
	{
		public abstract ItemDef BuildObject();
	}

	public interface IMaterial
	{
		public abstract Material BuildObject();
	}

	public interface IOverlay
	{
		public abstract Material BuildObject();
		public abstract bool CheckEnabled(CharacterModel model);
	}

	public interface IMaterialSwap
	{
		public abstract Material BuildObject();
		public abstract bool CheckEnabled(CharacterModel model, CharacterModel.RendererInfo targetRendererInfo);
		public abstract int Priority { get; }
	}

	public interface IUnlockable
	{
		public abstract UnlockableDef BuildObject();
	}

	public interface IMusicTrack
	{
		public abstract MusicTrackDef BuildObject();
	}

	public interface IBuff
	{
		public abstract BuffDef BuildObject();
	}

	public interface ISkillFamily
	{
		public virtual SkillFamily BuildObject() => null!;

		public abstract Type[] GetSkillAssets();

		public virtual string GetNameToken(GenericSkill skill) => "";
	}

	public interface ISkill
	{
		public abstract SkillDef BuildObject();

		public abstract Type[] GetEntityStates();
	}

	public interface IEntityStates
	{
		public abstract Type[] GetEntityStates();
	}

	public interface IVariant
	{
		public abstract SkillFamily.Variant BuildObject();
	}
}