using RoR2;
using RoR2.Skills;
using UnityEngine;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace ConcentricContent
{
	public abstract partial class Concentric
	{
		public static Task<ItemDef> GetItemDef<T>() where T : Concentric, IItem =>
			GetObjectOrThrow<T, IItem, ItemDef>();

		public static async Task<ItemIndex> GetItemIndex<T>() where T : Concentric, IItem =>
			(await GetObjectOrThrow<T, IItem, ItemDef>()).itemIndex;

		public static Task<UnlockableDef> GetUnlockableDef<T>() where T : Concentric, IUnlockable =>
			GetObjectOrThrow<T, IUnlockable, UnlockableDef>();

		public static Task<BuffDef> GetBuffDef<T>() where T : Concentric, IBuff =>
			GetObjectOrThrow<T, IBuff, BuffDef>();

		public static async Task<BuffIndex> GetBuffIndex<T>() where T : Concentric, IBuff =>
			(await GetObjectOrThrow<T, IBuff, BuffDef>()).buffIndex;

		public static async Task<BodyIndex> GetBodyIndex<T>() where T : Concentric, IBody =>
			(await GetObjectOrThrow<T, IBody, GameObject>()).GetComponent<CharacterBody>().bodyIndex;

		public static Task<Material> GetMaterial<T>() where T : Concentric, IMaterial =>
			GetObjectOrThrow<T, IMaterial, Material>();

		public static Task<SurvivorDef> GetSurvivorDef<T>() where T : Concentric, ISurvivor =>
			GetObjectOrThrow<T, ISurvivor, SurvivorDef>();

		public static Task<SkinDef> GetSkinDef<T>() where T : Concentric, ISkin =>
			GetObjectOrThrow<T, ISkin, SkinDef>();

		public static Task<SkillDef> GetSkillDef<T>() where T : Concentric, ISkill =>
			GetObjectOrThrow<T, ISkill, SkillDef>();

		public static Task<MusicTrackDef> GetMusicTrackDef<T>() where T : Concentric, IMusicTrack =>
			GetObjectOrThrow<T, IMusicTrack, MusicTrackDef>();

		public static Task<SkillFamily> GetSkillFamily<T>() where T : Concentric, ISkillFamily =>
			GetObjectOrThrow<T, ISkillFamily, SkillFamily>();

		public static Task<SkillFamily.Variant> GetSkillFamilyVariant<T>() where T : Concentric, ISkill =>
			GetObjectOrThrow<IVariant, SkillFamily.Variant>(GetAsset<T>());

		public static Task<GameObject> GetNetworkedObject<T>() where T : Concentric, INetworkedObject =>
			GetObjectOrThrow<T, INetworkedObject, GameObject>();
		public static Task<GameObject> GetGenericObject<T>() where T : Concentric, IGenericObject =>
			GetObjectOrThrow<T, IGenericObject, GameObject>();

		public static Task<GameObject> GetProjectile<T>() where T : Concentric, IProjectile =>
			GetObjectOrThrow<T, IProjectile, GameObject>();

		public static Task<GameObject> GetProjectileGhost<T>() where T : Concentric, IProjectileGhost =>
			GetObjectOrThrow<T, IProjectileGhost, GameObject>();

		public static Task<GameObject> GetEffect<T>() where T : Concentric, IEffect =>
			GetObjectOrThrow<T, IEffect, GameObject>();

		public static Task<GameObject> GetMaster<T>() where T : Concentric, IMaster =>
			GetObjectOrThrow<T, IMaster, GameObject>();

		public static Task<GameObject> GetBody<T>() where T : Concentric, IBody =>
			GetObjectOrThrow<T, IBody, GameObject>();

		public static Task<GameObject> GetBodyDisplay<T>() where T : Concentric, IBodyDisplay =>
			GetObjectOrThrow<T, IBodyDisplay, GameObject>();

		public static Task<GameObject> GetModel<T>() where T : Concentric, IModel =>
			GetObjectOrThrow<T, IModel, GameObject>();
	}

	public static class AssetExtensionMethods
	{
		public static Task<ItemDef> GetItemDef<T>(this T _) where T : Concentric, IItem => Concentric.GetItemDef<T>();
		public static Task<ItemIndex> GetItemIndex<T>(this T _) where T : Concentric, IItem => Concentric.GetItemIndex<T>();

		public static Task<UnlockableDef> GetUnlockableDef<T>(this T _) where T : Concentric, IUnlockable =>
			Concentric.GetUnlockableDef<T>();

		public static Task<BuffDef> GetBuffDef<T>(this T _) where T : Concentric, IBuff => Concentric.GetBuffDef<T>();
		public static Task<BuffIndex> GetBuffIndex<T>(this T _) where T : Concentric, IBuff => Concentric.GetBuffIndex<T>();
		public static Task<BodyIndex> GetBodyIndex<T>(this T _) where T : Concentric, IBody => Concentric.GetBodyIndex<T>();
		public static Task<Material> GetMaterial<T>(this T _) where T : Concentric, IMaterial => Concentric.GetMaterial<T>();

		public static Task<SurvivorDef> GetSurvivorDef<T>(this T _) where T : Concentric, ISurvivor =>
			Concentric.GetSurvivorDef<T>();

		public static Task<SkinDef> GetSkinDef<T>(this T _) where T : Concentric, ISkin => Concentric.GetSkinDef<T>();
		public static Task<SkillDef> GetSkillDef<T>(this T _) where T : Concentric, ISkill => Concentric.GetSkillDef<T>();

		public static Task<MusicTrackDef> GetMusicTrackDef<T>(this T _) where T : Concentric, IMusicTrack =>
			Concentric.GetMusicTrackDef<T>();

		public static Task<SkillFamily> GetSkillFamily<T>(this T _) where T : Concentric, ISkillFamily =>
			Concentric.GetSkillFamily<T>();

		public static Task<SkillFamily.Variant> GetSkillFamilyVariant<T>(this T _) where T : Concentric, ISkill =>
			Concentric.GetSkillFamilyVariant<T>();

		public static Task<GameObject> GetNetworkedObject<T>(this T _) where T : Concentric, INetworkedObject =>
			Concentric.GetNetworkedObject<T>();

		public static Task<GameObject> GetGenericObject<T>(this T _) where T : Concentric, IGenericObject =>
			Concentric.GetGenericObject<T>();

		public static Task<GameObject> GetProjectile<T>(this T _) where T : Concentric, IProjectile =>
			Concentric.GetProjectile<T>();

		public static Task<GameObject> GetProjectileGhost<T>(this T _) where T : Concentric, IProjectileGhost => Concentric.GetProjectileGhost<T>();
		public static Task<GameObject> GetEffect<T>(this T _) where T : Concentric, IEffect => Concentric.GetEffect<T>();
		public static Task<GameObject> GetMaster<T>(this T _) where T : Concentric, IMaster => Concentric.GetMaster<T>();
		public static Task<GameObject> GetBody<T>(this T _) where T : Concentric, IBody => Concentric.GetBody<T>();

		public static Task<GameObject> GetBodyDisplay<T>(this T _) where T : Concentric, IBodyDisplay =>
			Concentric.GetBodyDisplay<T>();

		public static Task<GameObject> GetModel<T>(this T _) where T : Concentric, IModel => Concentric.GetModel<T>();
	}
}