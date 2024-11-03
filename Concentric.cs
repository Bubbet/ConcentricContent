using System.Reflection;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System.Collections.Concurrent;
using UnityEngine;

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace ConcentricContent
{
	public abstract partial class Concentric
	{
		public static readonly Dictionary<string, object> Objects = new Dictionary<string, object>();
		public static readonly Dictionary<Type, Concentric> Assets = new Dictionary<Type, Concentric>();
		public static readonly Dictionary<object, Concentric> ObjectToAssetMap = new Dictionary<object, Concentric>();
		public static readonly List<IOverlay> Overlays = new List<IOverlay>();
		public static readonly List<IMaterialSwap> MaterialSwaps = new List<IMaterialSwap>();

		public static readonly Dictionary<IMaterialSwap, Material> MaterialSwapMaterials =
			new Dictionary<IMaterialSwap, Material>();

		public static readonly Dictionary<IOverlay, Material> OverlayMaterials = new Dictionary<IOverlay, Material>();

		public static readonly ConcurrentDictionary<string, Task<object?>> Tasks =
			new ConcurrentDictionary<string, Task<object?>>(Environment.ProcessorCount * 2, 64);

		public static async Task<ContentPack> BuildContentPack(Assembly assembly)
		{
			var result = new ContentPack();

			var types = assembly.GetTypes();
			foreach (var barData in types.Where(x => typeof(BarData).IsAssignableFrom(x) && !x.IsAbstract))
			{
				ExtraHealthBarSegments._barDataTypes.Add(barData);
			}

			var assets = types
				.Where(x => typeof(Concentric).IsAssignableFrom(x) && !x.IsAbstract);

			var localAssets = assets.ToDictionary(x => x, x => (Concentric)Activator.CreateInstance(x));
			foreach (var (key, value) in localAssets) Assets[key] = value;
			var instances = localAssets.Values;
			await Task.WhenAll(instances.Select(asset => asset.Initialize()));

			var overlays = instances.Where(x => x is IOverlay).Cast<IOverlay>().ToArray();
			Overlays.AddRange(overlays);
			var materialsOL = await Task.WhenAll(overlays.Select(x => GetObjectOrThrow<IOverlay, Material>((Concentric)x)));
			for (var i = 0; i < overlays.Length; i++)
			{
				OverlayMaterials[overlays[i]] = materialsOL[i];
			}

			MaterialSwaps.AddRange(instances.Where(x => x is IMaterialSwap).Cast<IMaterialSwap>());
			var swaps = instances.Where(x => x is IMaterialSwap).Cast<IMaterialSwap>().ToArray();
			MaterialSwaps.AddRange(swaps);
			var materialsSW =
				await Task.WhenAll(swaps.Select(x => GetObjectOrThrow<IMaterialSwap, Material>((Concentric)x)));
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
			result.bodyPrefabs.Add((await Task.WhenAll(instances.Where(x => x is IBody).Select(GetObjectOrThrow<IBody, GameObject>)))!);
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

		public static T GetAsset<T>() where T : Concentric => (T)GetAsset(typeof(T));

		public static T GetAsset<T, T2>() where T : Concentric, T2 => GetAsset<T>();

		public static Concentric GetAsset(Type assetType) => Assets.ContainsKey(assetType)
			? Assets[assetType]
			: throw new AssetTypeInvalidException($"{assetType.FullName} is not an Asset");


		public static Task<T2> GetObjectOrThrow<T, T1, T2>() where T : Concentric, T1 =>
			GetObjectOrThrow<T1, T2>(GetAsset<T>());

		public static async Task<T2> GetObjectOrThrow<T, T2>(Concentric concentric) =>
			(T2)await GetObjectOrThrow(concentric, typeof(T));

		public static Task<object> GetObjectOrThrow<T>(Concentric concentric) => GetObjectOrThrow(concentric, typeof(T));

		public static async Task<object> GetObjectOrThrow(Concentric concentric, Type targetType)
		{
			return await GetObjectOrNull(concentric, targetType) ??
			       throw new AssetTypeInvalidException($"{concentric.GetType().FullName} is not of type {targetType.Name}");
		}

		public static async Task<T2?> GetObjectOrNull<T, T1, T2>() where T : Concentric, T1
		{
			var assetType = typeof(T);
			return Assets.TryGetValue(assetType, out var asset)
				? await GetObjectOrNull(asset, assetType) is T2 result ? result : default
				: default;
		}

		public static async Task<T2?> GetObjectOrNull<T, T2>(Concentric concentric) =>
			await GetObjectOrNull<T>(concentric) is T2 result ? result : default;

		public static Task<object?> GetObjectOrNull<T>(Concentric concentric) => GetObjectOrNull(concentric, typeof(T));

		public static Task<object?> GetObjectOrNull(Concentric concentric, Type targetType)
		{
			var assetType = concentric.GetType();
			var name = assetType.FullName;
			var targetTypeName = targetType.Name;
			var key = assetType.Assembly.FullName + "_" + name + "_" + targetTypeName;

			if (Tasks.TryGetValue(key, out var task)) return task;
			task = GetObjectOrNull_Internal(concentric, targetType);
			Tasks[key] = task;
			return task;
		}

		private static async Task<object?> GetObjectOrNull_Internal(Concentric concentric, Type targetType)
		{
			var assetType = concentric.GetType();
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
					var skillTask = (concentric as ISkill)?.BuildObject();
					if (skillTask is null) return null;
					var skill = await skillTask;

					var entityStates = ((ISkill)concentric).GetEntityStates();
					Objects[key + "_EntityStates"] = entityStates;
					// ObjectToAssetMap ??
					skill.skillName = name + nameof(SkillDef);
					skill.activationState = new SerializableEntityStateType(entityStates.FirstOrDefault());
					returnedObject = skill;
					break;
				case nameof(IEffect):
					var effectTask = (concentric as IEffect)?.BuildObject();
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
					var variantTask = (concentric as IVariant)?.BuildObject();
					SkillFamily.Variant variant;
					if (variantTask is null)
					{
						var skillDef = await GetObjectOrNull<ISkill, SkillDef>(concentric);
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
					if (concentric is not ISkillFamily familyAsset) return null;
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
					if (concentric is not IModel modelAsset) return null;
					returnedObject = await modelAsset.BuildObject();
					Objects[key] = returnedObject;
					ObjectToAssetMap[returnedObject] = concentric;
					var skinController = ((GameObject)returnedObject).GetOrAddComponent<ModelSkinController>();
#pragma warning disable CS4014
					Task.WhenAll(modelAsset.GetSkins().Select(GetObjectOrThrow<ISkin, SkinDef>)).ContinueWith(completedTask =>
					{
						skinController.skins = completedTask.Result;
					}).ConfigureAwait(false);
#pragma warning restore CS4014
					break;
				default:
					if (targetType.GetMethod("BuildObject")?.Invoke(concentric, null) is not Task task) return null;
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

			if (nameof(IModel) != targetTypeName && Objects.TryGetValue(key, out var existingObject))
			{
				LOG.LogWarning(
					$"You shouldn't be seeing this({key}). It might mean a race condition, report to Concentric Content author.");
				return existingObject;
			}

			Objects[key] = returnedObject;
			ObjectToAssetMap[returnedObject] = concentric;
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