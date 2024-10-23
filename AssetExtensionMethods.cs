using RoR2;
using RoR2.Skills;
using UnityEngine;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace ConcentricContent
{
	public abstract partial class Asset
	{
		public static Task<ItemDef> GetItemDef<T>() where T : Asset, IItem =>
			GetObjectOrThrow<T, IItem, ItemDef>();

		public static async Task<ItemIndex> GetItemIndex<T>() where T : Asset, IItem =>
			(await GetObjectOrThrow<T, IItem, ItemDef>()).itemIndex;

		public static Task<UnlockableDef> GetUnlockableDef<T>() where T : Asset, IUnlockable =>
			GetObjectOrThrow<T, IUnlockable, UnlockableDef>();

		public static Task<BuffDef> GetBuffDef<T>() where T : Asset, IBuff =>
			GetObjectOrThrow<T, IBuff, BuffDef>();

		public static async Task<BuffIndex> GetBuffIndex<T>() where T : Asset, IBuff =>
			(await GetObjectOrThrow<T, IBuff, BuffDef>()).buffIndex;

		public static async Task<BodyIndex> GetBodyIndex<T>() where T : Asset, IBody =>
			(await GetObjectOrThrow<T, IBody, GameObject>()).GetComponent<CharacterBody>().bodyIndex;

		public static Task<Material> GetMaterial<T>() where T : Asset, IMaterial =>
			GetObjectOrThrow<T, IMaterial, Material>();

		public static Task<SurvivorDef> GetSurvivorDef<T>() where T : Asset, ISurvivor =>
			GetObjectOrThrow<T, ISurvivor, SurvivorDef>();

		public static Task<SkinDef> GetSkinDef<T>() where T : Asset, ISkin =>
			GetObjectOrThrow<T, ISkin, SkinDef>();

		public static Task<SkillDef> GetSkillDef<T>() where T : Asset, ISkill =>
			GetObjectOrThrow<T, ISkill, SkillDef>();

		public static Task<MusicTrackDef> GetMusicTrackDef<T>() where T : Asset, IMusicTrack =>
			GetObjectOrThrow<T, IMusicTrack, MusicTrackDef>();

		public static Task<SkillFamily> GetSkillFamily<T>() where T : Asset, ISkillFamily =>
			GetObjectOrThrow<T, ISkillFamily, SkillFamily>();

		public static Task<SkillFamily.Variant> GetSkillFamilyVariant<T>() where T : Asset, ISkill =>
			GetObjectOrThrow<IVariant, SkillFamily.Variant>(GetAsset<T>());

		public static Task<GameObject> GetNetworkedObject<T>() where T : Asset, INetworkedObject =>
			GetObjectOrThrow<T, INetworkedObject, GameObject>();
		public static Task<GameObject> GetGenericObject<T>() where T : Asset, IGenericObject =>
			GetObjectOrThrow<T, IGenericObject, GameObject>();

		public static Task<GameObject> GetProjectile<T>() where T : Asset, IProjectile =>
			GetObjectOrThrow<T, IProjectile, GameObject>();

		public static Task<GameObject> GetProjectileGhost<T>() where T : Asset, IProjectileGhost =>
			GetObjectOrThrow<T, IProjectileGhost, GameObject>();

		public static Task<GameObject> GetEffect<T>() where T : Asset, IEffect =>
			GetObjectOrThrow<T, IEffect, GameObject>();

		public static Task<GameObject> GetMaster<T>() where T : Asset, IMaster =>
			GetObjectOrThrow<T, IMaster, GameObject>();

		public static Task<GameObject> GetBody<T>() where T : Asset, IBody =>
			GetObjectOrThrow<T, IBody, GameObject>();

		public static Task<GameObject> GetBodyDisplay<T>() where T : Asset, IBodyDisplay =>
			GetObjectOrThrow<T, IBodyDisplay, GameObject>();

		public static Task<GameObject> GetModel<T>() where T : Asset, IModel =>
			GetObjectOrThrow<T, IModel, GameObject>();
	}

	public static class AssetExtensionMethods
	{
		public static Task<ItemDef> GetItemDef<T>(this T _) where T : Asset, IItem => Asset.GetItemDef<T>();
		public static Task<ItemIndex> GetItemIndex<T>(this T _) where T : Asset, IItem => Asset.GetItemIndex<T>();

		public static Task<UnlockableDef> GetUnlockableDef<T>(this T _) where T : Asset, IUnlockable =>
			Asset.GetUnlockableDef<T>();

		public static Task<BuffDef> GetBuffDef<T>(this T _) where T : Asset, IBuff => Asset.GetBuffDef<T>();
		public static Task<BuffIndex> GetBuffIndex<T>(this T _) where T : Asset, IBuff => Asset.GetBuffIndex<T>();
		public static Task<BodyIndex> GetBodyIndex<T>(this T _) where T : Asset, IBody => Asset.GetBodyIndex<T>();
		public static Task<Material> GetMaterial<T>(this T _) where T : Asset, IMaterial => Asset.GetMaterial<T>();

		public static Task<SurvivorDef> GetSurvivorDef<T>(this T _) where T : Asset, ISurvivor =>
			Asset.GetSurvivorDef<T>();

		public static Task<SkinDef> GetSkinDef<T>(this T _) where T : Asset, ISkin => Asset.GetSkinDef<T>();
		public static Task<SkillDef> GetSkillDef<T>(this T _) where T : Asset, ISkill => Asset.GetSkillDef<T>();

		public static Task<MusicTrackDef> GetMusicTrackDef<T>(this T _) where T : Asset, IMusicTrack =>
			Asset.GetMusicTrackDef<T>();

		public static Task<SkillFamily> GetSkillFamily<T>(this T _) where T : Asset, ISkillFamily =>
			Asset.GetSkillFamily<T>();

		public static Task<SkillFamily.Variant> GetSkillFamilyVariant<T>(this T _) where T : Asset, ISkill =>
			Asset.GetSkillFamilyVariant<T>();

		public static Task<GameObject> GetNetworkedObject<T>(this T _) where T : Asset, INetworkedObject =>
			Asset.GetNetworkedObject<T>();

		public static Task<GameObject> GetGenericObject<T>(this T _) where T : Asset, IGenericObject =>
			Asset.GetGenericObject<T>();

		public static Task<GameObject> GetProjectile<T>(this T _) where T : Asset, IProjectile =>
			Asset.GetProjectile<T>();

		public static Task<GameObject> GetProjectileGhost<T>(this T _) where T : Asset, IProjectileGhost => Asset.GetProjectileGhost<T>();
		public static Task<GameObject> GetEffect<T>(this T _) where T : Asset, IEffect => Asset.GetEffect<T>();
		public static Task<GameObject> GetMaster<T>(this T _) where T : Asset, IMaster => Asset.GetMaster<T>();
		public static Task<GameObject> GetBody<T>(this T _) where T : Asset, IBody => Asset.GetBody<T>();

		public static Task<GameObject> GetBodyDisplay<T>(this T _) where T : Asset, IBodyDisplay =>
			Asset.GetBodyDisplay<T>();

		public static Task<GameObject> GetModel<T>(this T _) where T : Asset, IModel => Asset.GetModel<T>();
	}
}