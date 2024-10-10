using UnityEngine;

namespace ConcentricContent
{
	public static class ExtensionMethods
	{
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			var result = gameObject.GetComponent<T>();
			if (!result) result = gameObject.AddComponent<T>();
			return result;
		}

		public static TV GetOrSet<TK, TV>(this Dictionary<TK, TV> dict, TK key, Func<TV> valueGetter)
		{
			if (dict.TryGetValue(key, out var value)) return value;
			value = valueGetter();
			dict[key] = value;
			return value;
		}
	}
}