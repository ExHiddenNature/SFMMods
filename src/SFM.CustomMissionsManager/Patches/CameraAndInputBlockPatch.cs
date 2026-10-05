using ExposureUnnoticed2.Object3D.Camera;
using ExposureUnnoticed2.Object3D.IngameManager;
using ExposureUnnoticed2.Object3D.Player.Scripts;
using ExposureUnnoticed2.Scripts.Base;
using ExposureUnnoticed2.Scripts.InGame;
using HarmonyLib;
using UnityEngine;

namespace SFM.CustomMissionsManager.Patches;

/// <summary>
/// When the Custom Missions Manager UI is open, suppresses camera rotation, player locomotion,
/// and in-game keyboard/controller inputs, while ensuring the cursor remains unlocked and visible.
/// </summary>
public static class CameraAndInputBlockPatch
{
	private static bool IsUiOpen => Plugin.Panel != null && Plugin.Panel.Enabled;

	#region Camera Suppression

	[HarmonyPatch(typeof(GameStateFacade), nameof(GameStateFacade.GetCameraMoveX))]
	public static class CameraMoveXPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (IsUiOpen)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(GameStateFacade), nameof(GameStateFacade.GetCameraMoveY))]
	public static class CameraMoveYPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (IsUiOpen)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(BaseCameraController), nameof(BaseCameraController.IsLockCamera), MethodType.Getter)]
	public static class BaseCameraIsLockCameraPatch
	{
		[HarmonyPostfix]
		public static void Postfix(ref bool __result)
		{
			if (IsUiOpen)
				__result = true;
		}
	}

	#endregion

	#region InputManager Suppression

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.GetVector3))]
	public static class InputManagerGetVector3Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref Vector3 __result)
		{
			if (IsUiOpen)
			{
				__result = Vector3.zero;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.GetVector2))]
	public static class InputManagerGetVector2Patch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref Vector2 __result)
		{
			if (IsUiOpen)
			{
				__result = Vector2.zero;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsDown))]
	public static class InputManagerIsDownPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			if (IsUiOpen)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsDownFrame))]
	public static class InputManagerIsDownFramePatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			if (IsUiOpen)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsUpFrame))]
	public static class InputManagerIsUpFramePatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			if (IsUiOpen)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsDownLongRepeat))]
	public static class InputManagerIsDownLongRepeatPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			if (IsUiOpen)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.GetLongDown))]
	public static class InputManagerGetLongDownPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			if (IsUiOpen)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InputManager), nameof(InputManager.GetShortUp))]
	public static class InputManagerGetShortUpPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			if (IsUiOpen)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	#endregion

	#region Player Locomotion InputResolver Suppression

	[HarmonyPatch(typeof(InputResolver), nameof(InputResolver.Resolve))]
	public static class InputResolverResolvePatch
	{
		private static InputData? _emptyInputData;

		[HarmonyPrefix]
		public static bool Prefix(ref InputData __result)
		{
			if (IsUiOpen)
			{
				_emptyInputData ??= new InputData();
				_emptyInputData.ClearSomePart();
				_emptyInputData.Move = Vector3.zero;
				_emptyInputData.CameraMove = Vector2.zero;
				_emptyInputData.JustOpenClothesA = false;
				_emptyInputData.JustOpenClothesB = false;
				_emptyInputData.JustCloseClothesA = false;
				_emptyInputData.JustCloseClothesB = false;
				_emptyInputData.JustCrouch = false;
				_emptyInputData.JustMove = false;
				_emptyInputData.JustCancel = false;
				_emptyInputData.LightAttack = false;
				_emptyInputData.HoldCommonRingMenu = false;
				_emptyInputData.HoldSkillRingMenu = false;
				_emptyInputData.JustSystemMenu = false;
				__result = _emptyInputData;
				return false;
			}
			return true;
		}
	}

	#endregion

	#region Cursor and UI State Protection

	[HarmonyPatch(typeof(CursorController), nameof(CursorController.IsVisible), MethodType.Getter)]
	public static class CursorControllerIsVisiblePatch
	{
		[HarmonyPostfix]
		public static void Postfix(ref bool __result)
		{
			if (IsUiOpen)
				__result = true;
		}
	}

	[HarmonyPatch(typeof(InGameUiManager), nameof(InGameUiManager.IsOpenSomeCancelInputUi), MethodType.Getter)]
	public static class InGameUiManagerIsOpenSomeCancelInputUiPatch
	{
		[HarmonyPostfix]
		public static void Postfix(ref bool __result)
		{
			if (IsUiOpen)
				__result = true;
		}
	}

	[HarmonyPatch(typeof(CursorController), nameof(CursorController.SetVisible))]
	public static class CursorControllerSetVisiblePatch
	{
		[HarmonyPrefix]
		public static void Prefix(ref bool isVisible)
		{
			if (IsUiOpen)
				isVisible = true;
		}
	}

	[HarmonyPatch(typeof(CursorController), "SetDisplayMouse")]
	public static class CursorControllerSetDisplayMousePatch
	{
		[HarmonyPrefix]
		public static void Prefix(ref bool isDisplay)
		{
			if (IsUiOpen)
				isDisplay = true;
		}
	}

	[HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Setter)]
	public static class CursorVisibleSetterPatch
	{
		[HarmonyPrefix]
		public static void Prefix(ref bool value)
		{
			if (IsUiOpen)
				value = true;
		}
	}

	[HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Setter)]
	public static class CursorLockStateSetterPatch
	{
		[HarmonyPrefix]
		public static void Prefix(ref CursorLockMode value)
		{
			if (IsUiOpen)
				value = CursorLockMode.None;
		}
	}

	#endregion
}
