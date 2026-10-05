using System;
using ExposureUnnoticed2.Master.Cosplay;
using ExposureUnnoticed2.Scripts.InGame;
using HarmonyLib;

namespace SFM.CustomMissionsManager.Patches;

[HarmonyPatch(typeof(SFM_custom_mission_v2.PlayerData), nameof(SFM_custom_mission_v2.PlayerData.DataOutput))]
public static class V2PlayerDataOutputPatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		try
		{
			if (GameState.GameStateData == null || MCosplay.dict == null)
				return false;
		}
		catch
		{
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(SFM_custom_mission_v2.Condition), nameof(SFM_custom_mission_v2.Condition.OutputCurrentConditions))]
public static class V2ConditionOutputPatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		try
		{
			if (MCosplay.dict == null)
				return false;
		}
		catch
		{
			return false;
		}
		return true;
	}
}
