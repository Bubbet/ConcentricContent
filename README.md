## Concentric Content
A library for simplifying asset creation, for big content mods.
As simple as adding to your solution and implementing the required interfaces. Check the Kamunagi of Chains character for an example on nearly every type of use-case. You can also ping me, Nines @unit_9_type_s with any questions and I will try my best to answer them!


The example below would be for a monolithic file involving a skill, a projectile, and a visual effect.

```
public class SkillNameState : BaseState
	{
		public static GameObject MuzzlePrefab;
		public static GameObject ProjectilePrefab; //explained below
	
		public override void OnEnter()
		{
			//whatever kinda stuff
		}
	}

public class SkillName : Concentric, IEffect, IProjectile, IProjectileGhost, ISkill
{
	public override async Task Initialize()
	{
		//explained below
	}
	
	async Task<GameObject> IProjectile.BuildObject()
	{
		var projectile = (await LoadAsset<GameObject>("RoR2/DLC2/Seeker/SpiritPunchProjectile.prefab"))!.InstantiateClone("CustomSeekerPrimary", true)
	    	projectile.GetComponent<ProjectileController>().ghostPrefab = await this.GetProjectileGhost();
	    	return projectile; //NOTE LoadAsset will not work unless you read further down below
	}
	
	async Task<GameObject> IProjectileGhost.BuildObject()
	{
		var ghost = (await LoadAsset<GameObject>("RoR2/Base/Grandparent/GrandparentBoulderGhost.prefab"))!.InstantiateClone("BoulderProjectileGhost", false);
		ghost.transform.localScale = Vector3.one * 0.3f;
		return ghost;
	}
	
	async Task<SkillDef> ISkill.BuildObject()
	{
		var skill = ScriptableObject.CreateInstance<SkillDef>();
		skill.skillName = "Primary 1";
		skill.skillNameToken = KamunagiAsset.tokenPrefix + "PRIMARY1_NAME";
		skill.skillDescriptionToken = KamunagiAsset.tokenPrefix + "PRIMARY1_DESCRIPTION";
		skill.activationState = new SerializableEntityStateType(typeof(SkillName));
		skill.icon = await LoadAsset<Sprite>("AssetBundleName:FileName"); //NOTE LoadAsset will not work unless you read further down below
		skill.activationStateMachineName = "Weapon";
		skill.baseRechargeInterval = 6f;
		skill.beginSkillCooldownOnSkillEnd = true;
		skill.interruptPriority = InterruptPriority.Any;
		return skill;
	}
	
	async Task<GameObject> IEffect.BuildOject()
	{
		var effect = await LoadAsset<GameObject>("AssetBundleName:FileName"); //NOTE LoadAsset will not work unless you read further down below
		effect.AddComponent<ObjectScaleCurve>().baseScale = Vector3.one * 3f;
		return effect;
	}
	
	IEnumerable<Type> ISkill.GetEntityStates() => new[] { typeof(SkillNameState) };
}
```

Using the assets in your EntityStates:

You can field them and then intialize them in your Concentric class, like this:
```
	public override async Task Initialize()
	{
		await base.Initialize();
		SkillNameState.MuzzlePrefab = await this.GetEffect();
		SkillNameState.ProjectilePrefab = await this.GetProjectile();
	}	
```

LoadAsset won't actually work unless you have this in your MainPlugin.cs
```
        public static Task<T> LoadAsset<T>(string assetPath) where T : UnityEngine.Object
	{
		if (assetPath.StartsWith("addressable:"))
		{
			return Addressables.LoadAssetAsync<T>(assetPath["addressable:".Length..]).Task;
		}

		if (assetPath.StartsWith("legacy:"))
		{
			return LegacyResourcesAPI.LoadAsync<T>(assetPath["legacy:".Length..]).Task;
		}
		var colinIndex = assetPath.IndexOf(":", StringComparison.Ordinal);
		if (colinIndex <= 0) return Addressables.LoadAssetAsync<T>(assetPath).Task;
		
		var source = new TaskCompletionSource<T>();
		var handle = bundles[assetPath[..colinIndex]].LoadAssetAsync<T>(assetPath[(colinIndex + 1)..]);
		handle.completed += _ => source.SetResult((T)handle.asset);
		return source.Task;
	}
```

## Credits
- Bubbet, did the entire thing
- Nines, ground-zero testing, wrote Readme, also named the library.

## Changelog
`1.0.1`
-Fixed a bug that caused Captain's beacons to not show up in the lobby

`1.0.0`
- Initial Release