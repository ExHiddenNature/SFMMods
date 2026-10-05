using HarmonyLib;
using UnityEngine;

namespace SFM.CustomMissionsManager.Patches;

/// <summary>
/// Unity disallows calling UnityEngine.Object.DestroyImmediate on GameObjects or Components
/// during physics triggers/contacts (e.g. OnTriggerEnter/Stay when getting caught by an NPC),
/// animation events, or rendering callbacks, throwing:
/// "Destroying GameObjects immediately is not permitted during physics trigger/contact, animation event callbacks..."
///
/// Custom mission scripts (like v2's Object_InteractArea and Object_Text) call DestroyImmediate during OnDestroy/cleanup.
/// This patch intercepts DestroyImmediate on GameObjects and Components and safely redirects them to
/// UnityEngine.Object.Destroy, deferring destruction to the end of the frame without violating Unity lifecycle constraints.
/// </summary>
public static class UnityObjectDestroyPatch
{
	[HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.DestroyImmediate), typeof(UnityEngine.Object))]
	public static class DestroyImmediatePatch1
	{
		[HarmonyPrefix]
		public static bool Prefix(UnityEngine.Object? obj)
		{
			if (obj == null)
				return false;

			if (obj is GameObject || obj is Component)
			{
				UnityEngine.Object.Destroy(obj);
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.DestroyImmediate), typeof(UnityEngine.Object), typeof(bool))]
	public static class DestroyImmediatePatch2
	{
		[HarmonyPrefix]
		public static bool Prefix(UnityEngine.Object? obj, bool allowDestroyingAssets)
		{
			if (obj == null)
				return false;

			if (obj is GameObject || obj is Component)
			{
				UnityEngine.Object.Destroy(obj);
				return false;
			}

			return true;
		}
	}
}

