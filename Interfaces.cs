using RoR2;
using RoR2.Skills;
using UnityEngine;
// ReSharper disable RedundantVirtualModifier
// ReSharper disable RedundantAbstractModifier
// ReSharper disable UnusedMember.Global

namespace CodedAssets
{
	public interface IGameObject {}

	public interface IGenericObject : IGameObject
	{
		public new abstract GameObject BuildObject();
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
		public abstract IEnumerable<Asset> GetSkins();
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

	// ReSharper disable once IdentifierTypo
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

		public abstract IEnumerable<Asset> GetSkillAssets();

		public virtual string GetNameToken(GenericSkill skill) => "";
	}

	public interface ISkill
	{
		public abstract SkillDef BuildObject();

		public abstract IEnumerable<Type> GetEntityStates();
	}

	public interface IEntityStates
	{
		public abstract IEnumerable<Type> GetEntityStates();
	}

	public interface IVariant
	{
		public abstract SkillFamily.Variant BuildObject();
	}
}