global using static CodedAssets.CodedAssetsPlugin;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CodedAssets
{
	[BepInPlugin(GUID, "CodedAssets", "1.0.0")]
	public class CodedAssetsPlugin : BaseUnityPlugin
	{
		public static Harmony harm;
		public static CodedAssetsPlugin instance;
		public static ManualLogSource log;
		public const string GUID = "bubbet.codedassets";

		public void Awake()
		{
			instance = this;
			log = Logger;
			harm = new Harmony(Info.Metadata.GUID);
			harm.PatchAll();
		}
	}
}