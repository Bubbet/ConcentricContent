global using static CodedAssets.CodedAssetsPlugin;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Security;
using System.Security.Permissions;

// ReSharper disable MemberCanBePrivate.Global

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

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