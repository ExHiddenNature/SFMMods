using System;
using HarmonyLib;
using UniverseLib;
using UniverseLib.Config;
using UniverseLib.Input;
using UniverseLib.UI;

namespace SFM.CustomMissionsManager.Patches;

/// <summary>
/// Disables UniverseLib's broken CursorUnlocker loop which forcibly sets Cursor.visible = true
/// every frame at WaitForEndOfFrame(), preventing the game from ever hiding the mouse.
/// By completely skipping UpdateCursorControl and disabling UniverseLib's EventSystem override,
/// UniverseLib leaves both cursor control and EventSystem management to the game and CustomMissionsManager.
/// </summary>
[HarmonyPatch(typeof(CursorUnlocker), "UpdateCursorControl")]
public static class UniverseLibCursorPatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		// Completely skip UniverseLib's UpdateCursorControl.
		// UniverseLib must not touch Cursor.lockState, Cursor.visible, or EventSystem.
		return false;
	}
}

[HarmonyPatch(typeof(CursorUnlocker), "Prefix_set_lockState")]
public static class UniverseLibCursorLockStatePatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		// Bypass UniverseLib's setter prefix
		return false;
	}
}

[HarmonyPatch(typeof(CursorUnlocker), "Prefix_set_visible")]
public static class UniverseLibCursorVisiblePatch
{
	[HarmonyPrefix]
	public static bool Prefix()
	{
		// Bypass UniverseLib's setter prefix
		return false;
	}
}
