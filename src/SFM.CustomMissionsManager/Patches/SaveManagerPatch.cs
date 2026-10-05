using ExposureUnnoticed2.Scripts.Base;
using ExposureUnnoticed2.Scripts.InGame;
using ExposureUnnoticed2.Scripts.UI;
using HarmonyLib;

namespace SFM.CustomMissionsManager.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Load), typeof(SaveRapper.SaveDataType), typeof(int))]
public static class SaveManager_Load_Patch
{
	[HarmonyPostfix]
	public static void Postfix(SaveRapper.SaveDataType type, int slot)
	{
		// System settings are not gameplay save slots!
		if (type == SaveRapper.SaveDataType.System)
			return;

		if (slot < 0 && (type == SaveRapper.SaveDataType.ManualSlot || type == SaveRapper.SaveDataType.AutoSave))
			slot = SaveManager.CurrentSaveSlot;

		if (slot >= 0)
			Plugin.StateManager.SwitchActiveSlot(slot);
	}
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save), typeof(SaveRapper.SaveDataType), typeof(int), typeof(bool))]
public static class SaveManager_Save_Patch
{
	[HarmonyPostfix]
	public static void Postfix(SaveRapper.SaveDataType type, int slot, bool isBackup)
	{
		if (isBackup || type == SaveRapper.SaveDataType.System)
			return;

		if (slot < 0 && (type == SaveRapper.SaveDataType.ManualSlot || type == SaveRapper.SaveDataType.AutoSave))
			slot = SaveManager.CurrentSaveSlot;

		if (slot >= 0)
			Plugin.StateManager.SwitchActiveSlot(slot);
	}
}

[HarmonyPatch(typeof(TitleSceneView), nameof(TitleSceneView.Awake))]
public static class TitleSceneView_Awake_Patch
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		Plugin.StateManager.SwitchActiveSlot(-1);
	}
}
