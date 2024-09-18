using UnityEngine;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace CodedAssets
{
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
}