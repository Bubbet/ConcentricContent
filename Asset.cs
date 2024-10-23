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

namespace ConcentricContent
{
	public abstract partial class Asset
	{
		public static readonly Dictionary<string, object> Objects = new Dictionary<string, object>();
		public static readonly Dictionary<Type, Asset> Assets = new Dictionary<Type, Asset>();
		public static readonly Dictionary<object, Asset> ObjectToAssetMap = new Dictionary<object, Asset>();
		public static readonly List<IOverlay> Overlays = new List<IOverlay>();
		public static readonly List<IMaterialSwap> MaterialSwaps = new List<IMaterialSwap>();
		
		public static readonly Dictionary<IMaterialSwap, Material> MaterialSwapMaterials = new Dictionary<IMaterialSwap, Material>();
		public static readonly Dictionary<IOverlay, Material> OverlayMaterials = new Dictionary<IOverlay, Material>();

		public static async Task<ContentPack> BuildContentPack(Assembly assembly)
		{
			var result = new ContentPack();

			var types = assembly.GetTypes();
			foreach (var barData in types.Where(x => typeof(BarData).IsAssignableFrom(x) && !x.IsAbstract))
			{
				ExtraHealthBarSegments._barDataTypes.Add(barData);
			}

			var assets = types
				.Where(x => typeof(Asset).IsAssignableFrom(x) && !x.IsAbstract);

			var localAssets = assets.ToDictionary(x => x, x => (Asset)Activator.CreateInstance(x));
			foreach (var (key, value) in localAssets) Assets[key] = value;
			var instances = localAssets.Values;
			await Task.WhenAll(instances.Select(asset => asset.Initialize()));

			var overlays = instances.Where(x => x is IOverlay).Cast<IOverlay>().ToArray();
			Overlays.AddRange(overlays);
			var materialsOL = await Task.WhenAll(overlays.Select(x => GetObjectOrThrow<IOverlay, Material>((Asset) x)));
			for (var i = 0; i < overlays.Length; i++)
			{
				OverlayMaterials[overlays[i]] = materialsOL[i];
			}
			
			MaterialSwaps.AddRange(instances.Where(x => x is IMaterialSwap).Cast<IMaterialSwap>());
			var swaps = instances.Where(x => x is IMaterialSwap).Cast<IMaterialSwap>().ToArray();
			MaterialSwaps.AddRange(swaps);
			var materialsSW = await Task.WhenAll(swaps.Select(x => GetObjectOrThrow<IMaterialSwap, Material>((Asset) x)));
			for (var i = 0; i < overlays.Length; i++)
			{
				MaterialSwapMaterials[swaps[i]] = materialsSW[i];
			}
			
			var entityStates = instances.Where(x => x is IEntityStates).SelectMany(x =>
				(Type[])Objects.GetOrSet(x.GetType().Assembly.FullName + "_" + x.GetType().FullName + "_EntityStates",
					() => ((IEntityStates)x).GetEntityStates()));

			result.unlockableDefs.Add((await Task.WhenAll(instances.Where(x => x is IUnlockable)
				.Select(GetObjectOrThrow<IUnlockable, UnlockableDef>)))!);
			result.itemDefs.Add(
				(await Task.WhenAll(instances.Where(x => x is IItem).Select(GetObjectOrThrow<IItem, ItemDef>)))!);
			result.buffDefs.Add(
				(await Task.WhenAll(instances.Where(x => x is IBuff).Select(GetObjectOrThrow<IBuff, BuffDef>)))!);
			result.skillDefs.Add((await Task.WhenAll(instances.Where(x => x is ISkill)
				.Select(GetObjectOrThrow<ISkill, SkillDef>)))!);
			result.entityStateTypes.Add(instances.Where(x => x is ISkill)
				.SelectMany(x =>
					(Type[])Objects[
						x.GetType().Assembly.FullName + "_" + x.GetType().FullName + "_" + nameof(ISkill) +
						"_EntityStates"])
				.Concat(entityStates).Distinct().ToArray());
			result.skillFamilies.Add((await Task.WhenAll(instances.Where(x => x is ISkillFamily)
				.Select(GetObjectOrThrow<ISkillFamily, SkillFamily>)))!);
			result.networkedObjectPrefabs.Add((await Task.WhenAll(instances.Where(x => x is INetworkedObject)
				.Select(GetObjectOrThrow<INetworkedObject, GameObject>)))!);
			result.bodyPrefabs.Add(
				(await Task.WhenAll(instances.Where(x => x is IBody).Select(GetObjectOrThrow<IBody, GameObject>)))!);
			result.survivorDefs.Add((await Task.WhenAll(instances.Where(x => x is ISurvivor)
				.Select(GetObjectOrThrow<ISurvivor, SurvivorDef>)))!);
			result.projectilePrefabs.Add((await Task.WhenAll(instances.Where(x => x is IProjectile)
				.Select(GetObjectOrThrow<IProjectile, GameObject>)))!);
			result.effectDefs.Add(
				(await Task.WhenAll(instances.Where(x => x is IEffect).Select(GetObjectOrThrow<IEffect, GameObject>)))!
				.Select(x => new EffectDef(x)).ToArray());
			result.masterPrefabs.Add((await Task.WhenAll(instances.Where(x => x is IMaster)
				.Select(GetObjectOrThrow<IMaster, GameObject>)))!);
			result.musicTrackDefs.Add((await Task.WhenAll(instances.Where(x => x is IMusicTrack)
				.Select(GetObjectOrThrow<IMusicTrack, MusicTrackDef>)))!);

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
			 * ItemDisplayRuleSet
			 */

			return result;
		}

		public static bool TryGetAssetFromObject<T>(object obj, out T asset)
		{
			var found = ObjectToAssetMap.TryGetValue(obj, out var assetObj);
			asset = assetObj is T ? (T)(object)assetObj : default!;
			return found;
		}

		public static T GetAsset<T>() where T : Asset => (T)GetAsset(typeof(T));

		public static T GetAsset<T, T2>() where T : Asset, T2 => GetAsset<T>();

		public static Asset GetAsset(Type assetType) => Assets.ContainsKey(assetType)
			? Assets[assetType]
			: throw new AssetTypeInvalidException($"{assetType.FullName} is not an Asset");



		public static Task<T2> GetObjectOrThrow<T, T1, T2>() where T : Asset, T1 => GetObjectOrThrow<T1, T2>(GetAsset<T>());

		public static async Task<T2> GetObjectOrThrow<T, T2>(Asset asset) => (T2) await GetObjectOrThrow(asset, typeof(T));
		public static Task<object> GetObjectOrThrow<T>(Asset asset) => GetObjectOrThrow(asset, typeof(T));

		public static async Task<object> GetObjectOrThrow(Asset asset, Type targetType)
		{
			return await GetObjectOrNull(asset, targetType) ??
			       throw new AssetTypeInvalidException($"{asset.GetType().FullName} is not of type {targetType.Name}");
		}
		
		public static async Task<T2?> GetObjectOrNull<T, T1, T2>() where T : Asset, T1
		{
			var assetType = typeof(T);
			return Assets.TryGetValue(assetType, out var asset)
				? await GetObjectOrNull(asset, assetType) is T2 result ? result : default
				: default;
		}

		public static async Task<T2?> GetObjectOrNull<T, T2>(Asset asset) => await GetObjectOrNull<T>(asset) is T2 result ? result : default;
		public static Task<object?> GetObjectOrNull<T>(Asset asset) => GetObjectOrNull(asset, typeof(T));

		public static async Task<object?> GetObjectOrNull(Asset asset, Type targetType)
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
					var skillTask = (asset as ISkill)?.BuildObject();
					if (skillTask is null) return null;
					var skill = await skillTask;

					var entityStates = ((ISkill)asset).GetEntityStates();
					Objects[key + "_EntityStates"] = entityStates;
					// ObjectToAssetMap ??
					skill.skillName = name + nameof(SkillDef);
					skill.activationState = new SerializableEntityStateType(entityStates.FirstOrDefault());
					returnedObject = skill;
					break;
				case nameof(IEffect):
					var effectTask = (asset as IEffect)?.BuildObject();
					if (effectTask is null) return null;
					var effect = await effectTask;

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
					var variantTask = (asset as IVariant)?.BuildObject();
					SkillFamily.Variant variant;
					if (variantTask is null)
					{
						var skillDef = await GetObjectOrNull<ISkill, SkillDef>(asset);
						if (skillDef is null) return null;
						variant = new SkillFamily.Variant
						{
							skillDef = skillDef, viewableNode = new ViewablesCatalog.Node(skillDef.skillName, false)
						};
					}
					else
						variant = await variantTask;

					returnedObject = variant;
					break;
				case nameof(ISkillFamily):
					if (asset is not ISkillFamily familyAsset) return null;
					var family = await familyAsset.BuildObject();
					// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
					if (family is null)
					{
						family = ScriptableObject.CreateInstance<SkillFamily>();
						family.variants = await Task.WhenAll(familyAsset.GetSkillAssets()
							.Select(GetObjectOrThrow<IVariant, SkillFamily.Variant>))!;
					}

					returnedObject = family;
					break;
				case nameof(IModel):
					if (asset is not IModel modelAsset) return null;
					returnedObject = await modelAsset.BuildObject();
					Objects[key] = returnedObject;
					ObjectToAssetMap[returnedObject] = asset;
					var skinController = ((GameObject)returnedObject).GetOrAddComponent<ModelSkinController>();
					skinController.skins =
						await Task.WhenAll(modelAsset.GetSkins().Select(GetObjectOrThrow<ISkin, SkinDef>))!;
					break;
				default:
					if (targetType.GetMethod("BuildObject")?.Invoke(asset, null) is not Task task) return null;
					await task;
					var taskType = task.GetType().GetProperty("Result");
					if (taskType == null) return null;
					returnedObject = taskType.GetValue(task);
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

		public virtual Task Initialize() => Task.CompletedTask;
	}

	public class AssetTypeInvalidException : Exception
	{
		public AssetTypeInvalidException(string message) : base(message)
		{
		}
	}
}