using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using HarmonyLib;
using ExposureUnnoticed2.Scripts.Base;
using System.Text;

namespace SFM.WindowResize;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
	public class Options
	{
		public bool Console;
		public int? X = null;
		public int? Y = null;
		public int? Width = null;
		public int? Height = null;
		public bool Windowed = false;

		public Options(bool console)
		{
			Console = console;

			X = CommandLine.GetArg(Console ? "-cx" : "-x", int.Parse);
			Y = CommandLine.GetArg(Console ? "-cy" : "-y", int.Parse);
			Width = CommandLine.GetArg(Console ? "-cw" : "-w", int.Parse);
			Height = CommandLine.GetArg(Console ? "-ch" : "-h", int.Parse);
			Windowed = CommandLine.HasArg("-windowed");
		}
	}

	public static new ManualLogSource Log = null!;
	public static Options UnityWindowOptions = null!;
	public static Options ConsoleWindowOptions = null!;

	public override void Load()
	{
		Log = base.Log;
		UnityWindowOptions = new Options(false);
		ConsoleWindowOptions = new Options(true);

		var harmony = new Harmony("SFM.WindowResize");
		harmony.PatchAll();
	}
}

[HarmonyPatch(typeof(WindowModeOptionApplyer), nameof(WindowModeOptionApplyer.Apply))]
public static class WindowModeOptionApplyer_Apply_Patch
{
	private const int GWL_STYLE = -16;
	private const int GWL_EXSTYLE = -20;
	private const int GW_OWNER = 4;

	private const uint WS_THICKFRAME = 0x00040000;
	private const uint WS_MAXIMIZEBOX = 0x00010000;

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOMOVE = 0x0002;
	private const uint SWP_NOZORDER = 0x0004;
	private const uint SWP_FRAMECHANGED = 0x0020;

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetConsoleWindow();

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentProcessId();

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
	private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

	[DllImport("user32.dll")]
	private static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

	private static IntPtr GetUnityWindow()
	{
		var currentPid = GetCurrentProcessId();
		var consoleHandle = GetConsoleWindow();
		var windowHandle = IntPtr.Zero;
		var classNameBuffer = new StringBuilder(64);

		EnumWindows((hWnd, lParam) =>
		{
			GetWindowThreadProcessId(hWnd, out var pid);
			if (
				pid != currentPid ||
				hWnd == consoleHandle ||
				!IsWindowVisible(hWnd) ||
				GetWindow(hWnd, GW_OWNER) != IntPtr.Zero
			)
			{
				return true;
			}

			classNameBuffer.Clear();
			GetClassName(hWnd, classNameBuffer, classNameBuffer.Capacity);

			if (string.Equals(classNameBuffer.ToString(), "UnityWndClass", StringComparison.Ordinal))
			{
				windowHandle = hWnd;
				return false;
			}
			return true;
		}, IntPtr.Zero);
		return windowHandle;
	}

	private static void ApplyStyleToWindow(IntPtr hWnd, bool isConsole)
	{
		if (hWnd == IntPtr.Zero)
			return;

		var currentStyle = (uint)(GetWindowLongPtr(hWnd, GWL_STYLE).ToInt64() & 0xFFFFFFFF);
		var currentExStyle = (uint)(GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64() & 0xFFFFFFFF);
		var newStyle = currentStyle | WS_THICKFRAME | WS_MAXIMIZEBOX;
		SetWindowLongPtr(hWnd, GWL_STYLE, new IntPtr((long)newStyle));

		var options = isConsole ? Plugin.ConsoleWindowOptions : Plugin.UnityWindowOptions;
		var hasMove = options.X.HasValue || options.Y.HasValue;
		var hasSize = options.Width.HasValue || options.Height.HasValue;

		var flags = SWP_NOZORDER | SWP_FRAMECHANGED;
		if (!hasMove && !hasSize)
		{
			SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, flags | SWP_NOMOVE | SWP_NOSIZE);
			return;
		}

		if (!GetWindowRect(hWnd, out var winRect) || !GetClientRect(hWnd, out var clientRect))
		{
			Plugin.Log.LogWarning($"Failed to query rects for HWND: {hWnd}. Applying frame style only.");
			SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, flags | SWP_NOMOVE | SWP_NOSIZE);
			return;
		}

		var targetX = options.X ?? winRect.Left;
		var targetY = options.Y ?? winRect.Top;

		// Base target dimensions on client rect to avoid compounding frame borders
		var targetClientW = options.Width ?? (clientRect.Right - clientRect.Left);
		var targetClientH = options.Height ?? (clientRect.Bottom - clientRect.Top);

		var calcRect = new RECT
		{
			Left = 0,
			Top = 0,
			Right = targetClientW,
			Bottom = targetClientH
		};
		AdjustWindowRectEx(ref calcRect, newStyle, false, currentExStyle);

		var targetW = calcRect.Right - calcRect.Left;
		var targetH = calcRect.Bottom - calcRect.Top;

		if (!hasMove)
			flags |= SWP_NOMOVE;
		if (!hasSize)
			flags |= SWP_NOSIZE;

		SetWindowPos(hWnd, IntPtr.Zero, targetX, targetY, targetW, targetH, flags);
	}

	public static void Postfix()
	{
		if (Plugin.UnityWindowOptions.Windowed)
		{
			Screen.fullScreen = false;
			Screen.fullScreenMode = FullScreenMode.Windowed;
		}

		if (Screen.fullScreenMode == FullScreenMode.Windowed)
		{
			var unityHwnd = GetUnityWindow();
			if (unityHwnd != IntPtr.Zero)
			{
				Plugin.Log.LogInfo("Applying window changes for Unity window");
				ApplyStyleToWindow(unityHwnd, false);
			}

			var consoleHwnd = GetConsoleWindow();
			if (consoleHwnd != IntPtr.Zero)
			{
				Plugin.Log.LogInfo("Applying window changes for console window");
				ApplyStyleToWindow(consoleHwnd, true);
			}
		}
	}
}
