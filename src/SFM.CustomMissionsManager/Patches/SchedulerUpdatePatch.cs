using ExposureUnnoticed2.Scripts.Base;
using HarmonyLib;

namespace SFM.CustomMissionsManager.Patches;

[HarmonyPatch(typeof(Scheduler), nameof(Scheduler.Update))]
public static class SchedulerUpdatePatch
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		Plugin.OnGameUpdate();
	}
}

