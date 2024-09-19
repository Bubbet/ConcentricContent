using System.Reflection;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace CodedAssets
{
	public abstract class Asset
	{
		public static readonly Dictionary<string, object> Objects = new Dictionary<string, object>();
		public static readonly Dictionary<Type, Asset> Assets = new Dictionary<Type, Asset>();
		public static readonly Dictionary<object, Asset> ObjectToAssetMap = new Dictionary<object, Asset>();
		public static readonly List<IOverlay> Overlays = new List<IOverlay>();
		public static readonly List<IMaterialSwap> MaterialSwaps = new List<IMaterialSwap>();

		public static ContentPack BuildContentPack()
		{
			var result = new ContentPack();

			var assets = Assembly.GetCallingAssembly().GetTypes()
				.Where(x => typeof(Asset).IsAssignableFrom(x) && !x.IsAbstract);

			var localAssets = assets.ToDictionary(x => x, x => (Asset)Activator.CreateInstance(x));
			foreach (var (key, value) in localAssets) Assets[key] = value;
			var instances = localAssets.Values;
			foreach (var asset in instances) asset.Initialize();

			Overlays.AddRange(instances.Where(x => x is IOverlay).Cast<IOverlay>());
			MaterialSwaps.AddRange(instances.Where(x => x is IMaterialSwap).Cast<IMaterialSwap>());
			var entityStates = instances.Where(x => x is IEntityStates).SelectMany(x =>
				(Type[])Objects.GetOrSet(x.GetType().Assembly.FullName + "_" + x.GetType().FullName + "_EntityStates",
					() => ((IEntityStates)x).GetEntityStates()));

			result.unlockableDefs.Add(instances.Where(x => x is IUnlockable).Select(x => (UnlockableDef)x!).ToArray());
			result.itemDefs.Add(instances.Where(x => x is IItem).Select(x => (ItemDef)x).ToArray());
			result.buffDefs.Add(instances.Where(x => x is IBuff).Select(x => (BuffDef)x).ToArray());
			result.skillDefs.Add(instances.Where(x => x is ISkill).Select(x => (SkillDef)x!).ToArray());
			result.entityStateTypes.Add(instances.Where(x => x is ISkill)
				.SelectMany(x =>
					(Type[])Objects[
						x.GetType().Assembly.FullName + "_" + x.GetType().FullName + "_" + nameof(ISkill) +
						"_EntityStates"])
				.Concat(entityStates).Distinct().ToArray());
			result.skillFamilies.Add(instances.Where(x => x is ISkillFamily).Select(x => (SkillFamily)x!).ToArray());
			result.networkedObjectPrefabs.Add(instances.Where(x => x is INetworkedObject)
				.Select(x => (GameObject)GetObjectOrThrow<INetworkedObject>(x))
				.ToArray());
			result.bodyPrefabs.Add(instances.Where(x => x is IBody).Select(x => (GameObject)GetObjectOrThrow<IBody>(x))
				.ToArray());
			result.survivorDefs.Add(instances.Where(x => x is ISurvivor).Select(x => (SurvivorDef)x!).ToArray());
			result.projectilePrefabs.Add(instances.Where(x => x is IProjectile)
				.Select(x => (GameObject)GetObjectOrThrow<IProjectile>(x)).ToArray());
			result.effectDefs.Add(instances.Where(x => x is IEffect)
				.Select(x => new EffectDef((GameObject)GetObjectOrThrow<IEffect>(x))).ToArray());
			result.masterPrefabs.Add(instances.Where(x => x is IMaster)
				.Select(x => (GameObject)GetObjectOrThrow<IMaster>(x))
				.ToArray());
			result.musicTrackDefs.Add(instances.Where(x => x is IMusicTrack).Select(x => (MusicTrackDef)x!).ToArray());

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

		public static Asset GetAsset(Type assetType) => Assets.ContainsKey(assetType)
			? Assets[assetType]
			: throw new AssetTypeInvalidException($"{assetType.FullName} is not an Asset");

		public static bool TryGetGameObject<T, T2>(out GameObject asset) where T2 : IGameObject where T : Asset, T2
		{
			var foundAsset = GetObjectOrNull<T2>(GetAsset<T>());
			if (foundAsset is GameObject gameObject)
			{
				asset = gameObject;
				return true;
			}
			asset = default!;
			return false;
		}

		public static GameObject GetGameObject<T, T2>() where T2 : IGameObject where T : Asset, T2 =>
			GetGameObject(typeof(T), typeof(T2));

		public static GameObject GetGameObject(Type callingType, Type targetType) =>
			(GameObject)GetObjectOrThrow(GetAsset(callingType), targetType);

		public static object? GetObjectOrNull<T>(Asset asset) => GetObjectOrNull(asset, typeof(T));

		public static object? GetObjectOrNull(Asset asset, Type targetType)
		{
			var assetType = asset.GetType();
			var name = assetType.FullName;
			var targetTypeName = targetType.Name;
			var key = assetType.Assembly.FullName + "_" + name + "_" + targetTypeName;
			if (Objects.TryGetValue(key, out var result))
			{
				return result!;
			}

			object? returnedObject;
			switch (targetTypeName)
			{
				case nameof(ISkill):
					var skill = (asset as ISkill)?.BuildObject();
					if (skill is null) return null;
					var entityStates = ((ISkill)asset).GetEntityStates();
					Objects[key + "_EntityStates"] = entityStates;
					// ObjectToAssetMap ??
					skill.skillName = name + nameof(SkillDef);
					skill.activationState = new SerializableEntityStateType(entityStates.FirstOrDefault());
					returnedObject = skill;
					break;
				case nameof(IEffect):
					var effect = (asset as IEffect)?.BuildObject();
					if (effect is null) return null;
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
						SkillDef? skillDef = asset;
						if (skillDef is null) return null;
						variant = new SkillFamily.Variant
						{
							skillDef = skillDef, viewableNode = new ViewablesCatalog.Node(skillDef.skillName, false)
						};
					}

					returnedObject = variant;
					break;
				case nameof(ISkillFamily):
					if (asset is not ISkillFamily familyAsset) return null;
					var family = familyAsset.BuildObject();
					// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
					if (family is null)
					{
						family = ScriptableObject.CreateInstance<SkillFamily>();
						family.variants = familyAsset.GetSkillAssets()
							.Select(x => (SkillFamily.Variant)x!).ToArray();
					}

					returnedObject = family;
					break;
				case nameof(IModel):
					if (asset is not IModel modelAsset) return null;
					returnedObject = modelAsset.BuildObject();
					Objects[key] = returnedObject;
					ObjectToAssetMap[returnedObject] = asset;
					var skinController = ((GameObject)returnedObject).GetOrAddComponent<ModelSkinController>();
					skinController.skins = modelAsset.GetSkins().Select(x => (SkinDef)x!).ToArray();
					break;
				default:
					returnedObject = targetType.GetMethod("BuildObject")?.Invoke(asset, null);
					if (returnedObject is null) return null;
					break;
			}

			var returnedType = returnedObject.GetType();
			var scriptableObject = typeof(ScriptableObject);
			var cachedName = returnedType.GetProperty("cachedName");
			var nameProperty = scriptableObject.GetProperty("name")!.GetSetMethod();
			var objectName = (assetType.Name + "_" + targetTypeName).Replace(".", "_");
			cachedName?.GetSetMethod().Invoke(returnedObject, new object[] { objectName });
			if (cachedName is null && scriptableObject.IsAssignableFrom(returnedType))
			{
				nameProperty.Invoke(returnedObject, new object[] { objectName });
			}

			Objects[key] = returnedObject;
			ObjectToAssetMap[returnedObject] = asset;
			return returnedObject;
		}

		public static object GetObjectOrThrow<T>(Asset asset) => GetObjectOrThrow(asset, typeof(T));

		public static object GetObjectOrThrow(Asset asset, Type targetType)
		{
			return GetObjectOrNull(asset, targetType) ??
			       throw new AssetTypeInvalidException($"{asset.GetType().FullName} is not of type {targetType.Name}");
		}

		public static explicit operator ItemDef(Asset asset) => (ItemDef)GetObjectOrThrow<IItem>(asset);

		public static implicit operator ItemIndex(Asset asset) =>
			(GetObjectOrNull<IItem>(asset) as ItemDef)?.itemIndex ?? ItemIndex.None;

		public static implicit operator UnlockableDef?(Asset asset) =>
			GetObjectOrNull<IUnlockable>(asset) as UnlockableDef;

		public static explicit operator BuffDef(Asset asset) => (BuffDef)GetObjectOrThrow<IBuff>(asset);

		public static implicit operator BuffIndex(Asset asset) =>
			(GetObjectOrNull<IBuff>(asset) as BuffDef)?.buffIndex ?? BuffIndex.None;

		public static implicit operator BodyIndex(Asset asset) =>
			(GetObjectOrNull<IBody>(asset) as GameObject)?.GetComponent<CharacterBody>().bodyIndex ?? BodyIndex.None;

		public static implicit operator Material?(Asset asset) => GetObjectOrNull<IMaterial>(asset) as Material;

		public static implicit operator SurvivorDef?(Asset asset) => GetObjectOrNull<ISurvivor>(asset) as SurvivorDef;

		public static implicit operator SkinDef?(Asset asset) => GetObjectOrNull<ISkin>(asset) as SkinDef;

		public static implicit operator SkillDef?(Asset asset) => GetObjectOrNull<ISkill>(asset) as SkillDef;

		public static implicit operator MusicTrackDef?(Asset asset) =>
			GetObjectOrNull<IMusicTrack>(asset) as MusicTrackDef;


		public static implicit operator SkillFamily?(Asset asset) =>
			GetObjectOrNull<ISkillFamily>(asset) as SkillFamily;

		public static implicit operator SkillFamily.Variant?(Asset asset) =>
			(SkillFamily.Variant?)GetObjectOrNull<IVariant>(asset);

		public virtual void Initialize() { }
	}

	public class AssetTypeInvalidException : Exception
	{
		public AssetTypeInvalidException(string message) : base(message)
		{
		}
	}
}