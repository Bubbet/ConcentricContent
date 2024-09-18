global using static CodedAssets.CodedAssetsPlugin;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

// ReSharper disable MemberCanBePrivate.Global

namespace CodedAssets
{
	[BepInPlugin(Guid, "CodedAssets", "1.0.0")]
	public class CodedAssetsPlugin : BaseUnityPlugin
	{
		public static Harmony Harm = null!;
		public static CodedAssetsPlugin Instance = null!;
		public static ManualLogSource LOG = null!;
		public const string Guid = "bubbet.codedassets";

		public void Awake()
		{
			Instance = this;
			LOG = Logger;
			Harm = new Harmony(Info.Metadata.GUID);
			Harm.PatchAll();
		}
	}
}