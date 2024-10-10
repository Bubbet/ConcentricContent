global using static ConcentricContent.ConcentricContentPlugin;
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

namespace ConcentricContent
{
	[BepInPlugin(Guid, "ConcentricContent", "1.0.0")]
	public class ConcentricContentPlugin : BaseUnityPlugin
	{
		public static Harmony Harm = null!;
		public static ConcentricContentPlugin Instance = null!;
		public static ManualLogSource LOG = null!;
		public const string Guid = "bubbet.concentriccontent";

		public void Awake()
		{
			Instance = this;
			LOG = Logger;
			Harm = new Harmony(Info.Metadata.GUID);
			Harm.PatchAll();
		}
	}
}