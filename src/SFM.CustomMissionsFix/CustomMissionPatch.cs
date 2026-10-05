using ExposureUnnoticed2.Master.Stage;
using ExposureUnnoticed2.ObjectUI.MissionMenuPanel;
using HarmonyLib;
using SFM.CustomMissionsFix;
using SFMACM = SFM_advanced_custom_mission;
using ExposureUnnoticed2.Scripts.Base;
using ExposureUnnoticed2.Scripts.InGame;
using System.IO;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save))]
public static class SaveManager_Save_Patch
{
	[HarmonyPostfix]
	public static void Postfix(int slot, bool isBackup)
	{
		if (isBackup)
			return;
		if (slot < 0)
			slot = CustomMissionsMemory.CurrentSaveSlot;
		if (slot < 0)
			return;

		CustomMissionsMemory.SaveAllMissionProgress(slot);
	}
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Load))]
public static class SaveManager_Load_Patch
{
	[HarmonyPostfix]
	public static void Postfix(int slot)
	{
		if (slot < 0)
			slot = CustomMissionsMemory.CurrentSaveSlot;
		if (slot < 0)
			return;
		CustomMissionsMemory.CurrentSaveSlot = slot;

		CustomMissionsMemory.LoadAllMissionProgress(slot);
	}
}

[HarmonyPatch(typeof(GameOverController), nameof(GameOverController.OnGameOverEvent))]
public static class GameOverController_OnGameOverEvent_Patch
{
	[HarmonyPrefix]
	public static void Prefix()
	{
		Plugin.Log.LogInfo("Game over, marking all missions as not completed this run");

		foreach (var item in CustomMissionsMemory.Missions)
		{
			if (item.Value.CurrentInGameCompleted)
			{
				CustomMissionsMemory.LogInfo(item.Key, "Resetting mission completion for current run");
				item.Value.CurrentInGameCompleted = false;
			}
		}
	}
}

[HarmonyPatch(typeof(Scheduler), nameof(Scheduler.Update))]
public static class Scheduler_Update_Patch
{
	private static StageType Stage = StageType.None;

	[HarmonyPrefix]
	public static void Prefix()
	{
		if (InGameManager.Instance?.CurrentStageController == null)
			return;

		StageType prevStage = Stage;
		Stage = InGameManager.Instance.CurrentStageController.StageType;

		if (prevStage != Stage && Stage == StageType.Apart)
		{
			Plugin.Log.LogInfo("We are home, clearing all missions we completed this run");

			foreach (var item in CustomMissionsMemory.Missions)
			{
				if (item.Value.CurrentInGameCompleted)
				{
					item.Value.Clears += 1;
					item.Value.CurrentInGameCompleted = false;
					CustomMissionsMemory.LogInfo(item.Key, $"Completed in current run, now cleared {item.Value.Clears} times");
				}
			}
		}
	}
}

[HarmonyPatch(typeof(SFMACM.CustomMission), MethodType.Constructor, [typeof(SFMACM.CustomMissionManager), typeof(string)])]
public static class CustomMission_Constructor_Patch
{
	[HarmonyPostfix]
	public static void Postfix(SFMACM.CustomMission __instance, string filename)
	{
		if (__instance?.missionData == null)
			return;

		filename = Path.GetFileName(filename);
		CustomMissionsMemory.LogInfo(__instance, $"Is loading from '{filename}'");

		if (CustomMissionsMemory.Missions.TryGetValue(__instance, out var info))
		{
			info.Filename = filename;
		}
		else
		{
			CustomMissionsMemory.Missions.Add(__instance, new CustomMissionsMemory.Info
			{
				Filename = filename,
			});
		}
	}
}

[HarmonyPatch(typeof(SFMACM.CustomMission), nameof(SFMACM.CustomMission.Destroy))]
public static class CustomMission_Destroy_Patch
{
	[HarmonyPrefix]
	public static void Prefix(SFMACM.CustomMission __instance)
	{
		if (__instance?.missionData == null)
			return;

		CustomMissionsMemory.LogInfo(__instance, "Is being destroyed");
		CustomMissionsMemory.Missions.Remove(__instance);
	}
}

[HarmonyPatch(typeof(SFMACM.CustomMission), nameof(SFMACM.CustomMission.Update))]
public static class CustomMission_UpdateGUI_Patch
{
	[HarmonyPostfix]
	public static void Postfix(SFMACM.CustomMission __instance, int ___currentCheckpointNum)
	{
		if (!CustomMissionsMemory.Missions.TryGetValue(__instance, out var info))
		{
			CustomMissionsMemory.Missions.Add(__instance, new CustomMissionsMemory.Info
			{
				CurrentCheckpoint = ___currentCheckpointNum,
			});
			return;
		}

		if (info.CurrentCheckpoint != ___currentCheckpointNum)
		{
			CustomMissionsMemory.LogInfo(__instance, $"Checkpoint updated {info.CurrentCheckpoint} => {___currentCheckpointNum}");
			info.CurrentCheckpoint = ___currentCheckpointNum;

			var isMissionComplete = false;
			if (
				___currentCheckpointNum < 0 ||
				___currentCheckpointNum >= __instance.missionData.checkpoints.Count ||
				__instance.missionData.checkpoints[___currentCheckpointNum] == null
			)
			{
				// The index is out of range which means we completed all conditions and the mission is complete
				isMissionComplete = true;
			}
			else if (___currentCheckpointNum == __instance.missionData.checkpoints.Count - 1)
			{
				// This is the last mission in the list, if its an infinite one count the mission as completed
				// Infinite conditions are used for when a mission is complete and you now get a small continious stream of RP
				const float DURATION_CONSIDERED_INFINITE = 10000.0f;

				var checkpoint = __instance.missionData.checkpoints[___currentCheckpointNum];
				if (checkpoint.condition.rp_persec > 0.0f && checkpoint.condition.duration >= DURATION_CONSIDERED_INFINITE)
					isMissionComplete = true;
			}

			if (isMissionComplete && !info.CurrentInGameCompleted)
			{
				info.CurrentInGameCompleted = true;
				CustomMissionsMemory.LogInfo(__instance, "Mission completed for current run");
				// TODO: Play the completion sound?
				// Visually more fixes are required that I don't care enough about to implement
				// Currently it just fades away while in reality it should blink, stay shortly, then fade out
			}
		}
	}
}

[HarmonyPatch(typeof(SFMACM.CustomMission), nameof(SFMACM.CustomMission.CreateMissionMenuItemView))]
public static class CustomMission_CreateMissionMenuItemView_Patch
{
	[HarmonyPrefix]
	public static void Prefix(SFMACM.CustomMission __instance, int ___currentCheckpointNum, StageType stage, out bool __state)
	{
		__state = false;

		// We recreate the logic of CreateMissionMenuItemView here
		if (!__instance.missionData.listmission)
			return;

		var addCompletionToUI = false;
		var customMissionCheckpoint = CustomMissionsMemory.GetMissionCurrentCheckpoint(__instance, ___currentCheckpointNum);
		foreach (var area in customMissionCheckpoint.zone.areas)
		{
			if (area.stage == stage)
			{
				addCompletionToUI = true;
				break;
			}
		}
		if (addCompletionToUI)
			__state = true;
	}

	[HarmonyPostfix]
	public static void Postfix(SFMACM.CustomMission __instance, bool __state)
	{
		if (!__state)
			return;

		if (MissionMenuPanelView.Instance == null || MissionMenuPanelView.Instance.missionItemParent == null)
			return;

		var parentTransform = MissionMenuPanelView.Instance.missionItemParent;
		if (parentTransform.childCount > 0)
		{
			var lastChild = parentTransform.GetChild(parentTransform.childCount - 1);
			MissionMenuItemView component = lastChild.GetComponent<MissionMenuItemView>();

			if (component != null)
			{
				if (CustomMissionsMemory.Missions.TryGetValue(__instance, out var info) && (info.CurrentInGameCompleted || info.Clears > 0))
				{
					component.clearMask.active = info.CurrentInGameCompleted;
					component.checkMarkObject.active = info.Clears > 0;
					component.achievementsText.text = info.Clears.ToString();
					CustomMissionsMemory.LogInfo(__instance, $"ClearedInCurrentRun={info.CurrentInGameCompleted} Clears={info.Clears}");
				}
			}
		}
	}
}
