using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.Runtime;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace SFM.CustomMissionsManager.UI;

public class SettingsTab
{
	private static readonly List<string> DefaultHotkeyOptions = new()
	{
		"F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
		"BackQuote", "Tab", "CapsLock", "Insert", "Delete", "Home", "End",
		"PageUp", "PageDown", "Pause", "ScrollLock",
		"Minus", "Equals", "Backslash", "LeftBracket", "RightBracket", "Semicolon", "Quote"
	};

	private readonly List<string> _hotkeyOptions = new(DefaultHotkeyOptions);
	private ButtonRef _rebindBtn = null!;
	private Dropdown _hotkeyDropdown = null!;
	private Text _infoDescLabel = null!;

	public GameObject Root { get; }

	public SettingsTab(GameObject parent)
	{
		Root = UIFactory.CreateVerticalGroup(parent, "SettingsTab", true, true, true, true, 8, new Vector4(12, 12, 12, 12), UIHelpers.PanelBgColor);
		UIFactory.SetLayoutElement(Root, flexibleWidth: 9999, flexibleHeight: 9999);

		// Toggle Hotkey Row
		var hotkeyRow = UIFactory.CreateHorizontalGroup(Root, "HotkeyRow", false, false, true, true, 8, new Vector4(8, 6, 8, 6), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(hotkeyRow, minHeight: 36, flexibleWidth: 9999);

		var hotkeyLabel = UIFactory.CreateLabel(hotkeyRow, "HotkeyLabel", "Toggle Hotkey:", TextAnchor.MiddleLeft, Color.white, true, 13);
		UIFactory.SetLayoutElement(hotkeyLabel.gameObject, minWidth: 105, flexibleWidth: 0);

		// Rebind button
		_rebindBtn = UIFactory.CreateButton(hotkeyRow, "RebindBtn", $"[ {Plugin.Hotkey} ]  Click to Rebind", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(_rebindBtn.Component.gameObject, minWidth: 180, minHeight: 28, flexibleWidth: 0);
		_rebindBtn.OnClick += OnRebindBtnClicked;

		// Dropdown
		EnsureHotkeyInOptions(Plugin.Hotkey.ToString());
		var dropdownObj = UIFactory.CreateDropdown(hotkeyRow, "HotkeyDropdown", out _hotkeyDropdown, Plugin.Hotkey.ToString(), 12, OnDropdownChanged, _hotkeyOptions.ToArray());
		UIFactory.SetLayoutElement(dropdownObj, minWidth: 110, minHeight: 28, flexibleWidth: 0);
		int initialIdx = _hotkeyOptions.IndexOf(Plugin.Hotkey.ToString());
		if (initialIdx >= 0)
		{
			_hotkeyDropdown.SetValueWithoutNotify(initialIdx);
			_hotkeyDropdown.RefreshShownValue();
		}

		// Reset Button
		var resetBtn = UIFactory.CreateButton(hotkeyRow, "ResetHotkeyBtn", "Reset (F10)", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(resetBtn.Component.gameObject, minWidth: 90, minHeight: 28, flexibleWidth: 0);
		resetBtn.OnClick += () =>
		{
			if (UniverseLib.Input.InputManager.Rebinding)
				CancelRebind();
			ApplyHotkey(KeyCode.F10);
		};

		var hotkeyHint = UIFactory.CreateLabel(hotkeyRow, "HotkeyHint", "Click to press any key, or pick from dropdown", TextAnchor.MiddleLeft, UIHelpers.TextMuted, true, 11);
		UIFactory.SetLayoutElement(hotkeyHint.gameObject, flexibleWidth: 9999);

		// Auto Reload
		var autoReloadRow = UIFactory.CreateHorizontalGroup(Root, "AutoReloadRow", false, false, true, true, 8, new Vector4(8, 6, 8, 6), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(autoReloadRow, minHeight: 36, flexibleWidth: 9999);

		var autoReloadToggleObj = UIFactory.CreateToggle(autoReloadRow, "Toggle", out var autoReloadToggle, out _, Plugin.ConfigService.Config.AutoReloadOnChanges ? UIHelpers.SuccessGreen : UIHelpers.ElementBgColor, 20, 20);
		autoReloadToggle.isOn = Plugin.ConfigService.Config.AutoReloadOnChanges;
		UIFactory.SetLayoutElement(autoReloadToggleObj, minWidth: 26, flexibleWidth: 0);

		autoReloadToggle.onValueChanged.AddListener((UnityAction<bool>)((bool val) =>
		{
			Plugin.ConfigService.Config.AutoReloadOnChanges = val;
			Plugin.ConfigService.SaveConfig();
		}));

		var autoReloadLabel = UIFactory.CreateLabel(autoReloadRow, "Label", "Auto-reload custom missions in-game when toggling missions/packs", TextAnchor.MiddleLeft, Color.white, true, 13);
		UIFactory.SetLayoutElement(autoReloadLabel.gameObject, flexibleWidth: 9999);

		// Rescan Missions
		var rescanRow = UIFactory.CreateHorizontalGroup(Root, "RescanRow", false, false, true, true, 8, new Vector4(8, 6, 8, 6), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(rescanRow, minHeight: 36, flexibleWidth: 9999);

		var rescanBtn = UIFactory.CreateButton(rescanRow, "RescanBtn", "Rescan Mission Folders", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(rescanBtn.Component.gameObject, minWidth: 180, minHeight: 28);
		rescanBtn.OnClick += () =>
		{
			Plugin.Registry.Rescan();
			Plugin.Log.LogInfo("Missions rescanned.");
		};

		var rescanLabel = UIFactory.CreateLabel(rescanRow, "Label", "Re-reads CustomMissions and CustomMissions2 for newly added files", TextAnchor.MiddleLeft, UIHelpers.TextMuted, true, 12);
		UIFactory.SetLayoutElement(rescanLabel.gameObject, flexibleWidth: 9999);

		// Info & Instructions
		var infoBox = UIFactory.CreateVerticalGroup(Root, "InfoBox", false, false, true, true, 4, new Vector4(8, 8, 8, 8), UIHelpers.ElementBgColor);
		UIFactory.SetLayoutElement(infoBox, minHeight: 120, flexibleWidth: 9999);

		var infoTitle = UIFactory.CreateLabel(infoBox, "InfoTitle", "<b>SFM Custom Missions Manager (In-Game Mod)</b>", TextAnchor.MiddleLeft, Color.white, true, 14);
		UIFactory.SetLayoutElement(infoTitle.gameObject, minHeight: 24);

		_infoDescLabel = UIFactory.CreateLabel(infoBox, "InfoDesc", "", TextAnchor.UpperLeft, UIHelpers.TextMuted, true, 12);
		UIFactory.SetLayoutElement(_infoDescLabel.gameObject, minHeight: 80);
		UpdateInfoDesc();
	}

	private void OnRebindBtnClicked()
	{
		if (UniverseLib.Input.InputManager.Rebinding)
		{
			CancelRebind();
			return;
		}

		_rebindBtn.ButtonText.text = "Press any key... (Esc cancels)";
		RuntimeHelper.SetColorBlockAuto(_rebindBtn.Component, new Color(0.18f, 0.38f, 0.65f));

		UniverseLib.Input.InputManager.BeginRebind((KeyCode kc) =>
		{
			UniverseLib.Input.InputManager.EndRebind();
			if (kc == KeyCode.Escape)
				CancelRebind();
			else
				ApplyHotkey(kc);
		}, null);
	}

	private void CancelRebind()
	{
		if (UniverseLib.Input.InputManager.Rebinding)
			UniverseLib.Input.InputManager.EndRebind();

		_rebindBtn.ButtonText.text = $"[ {Plugin.Hotkey} ]  Click to Rebind";
		RuntimeHelper.SetColorBlockAuto(_rebindBtn.Component, UIHelpers.ButtonNormalColor);
	}

	private void OnDropdownChanged(int index)
	{
		if (index >= 0 && index < _hotkeyOptions.Count)
		{
			if (Enum.TryParse<KeyCode>(_hotkeyOptions[index], true, out var parsedKey))
			{
				if (parsedKey != Plugin.Hotkey)
				{
					ApplyHotkey(parsedKey);
				}
			}
		}
	}

	private void ApplyHotkey(KeyCode newKey)
	{
		CancelRebind();

		Plugin.Hotkey = newKey;
		Plugin.ConfigService.Config.ToggleHotkey = newKey.ToString();
		Plugin.ConfigService.SaveConfig();

		_rebindBtn.ButtonText.text = $"[ {newKey} ]  Click to Rebind";

		EnsureHotkeyInOptions(newKey.ToString());
		int idx = _hotkeyOptions.IndexOf(newKey.ToString());
		if (idx >= 0 && _hotkeyDropdown != null)
		{
			_hotkeyDropdown.SetValueWithoutNotify(idx);
			_hotkeyDropdown.RefreshShownValue();
		}

		UpdateInfoDesc();
		Plugin.Log.LogInfo($"[SettingsTab] Toggle hotkey updated to: {newKey}");
	}

	private void EnsureHotkeyInOptions(string keyName)
	{
		if (!_hotkeyOptions.Contains(keyName))
		{
			_hotkeyOptions.Add(keyName);
			if (_hotkeyDropdown != null)
			{
				_hotkeyDropdown.options.Add(new Dropdown.OptionData(keyName));
			}
		}
	}

	private void UpdateInfoDesc()
	{
		if (_infoDescLabel != null)
		{
			_infoDescLabel.text =
				$"• Press <color=#60A5FA><b>{Plugin.Hotkey}</b></color> at any time to open/close this manager window.\n" +
				"• Missions are selectively loaded into the game without requiring symlinks or moving files.\n" +
				"• v1 missions reside in <i>CustomMissions</i>, v2 missions reside in <i>CustomMissions2</i>.\n" +
				"• Each save game slot (1-20) can have its own independent selection of enabled missions and mod packs.\n" +
				"• Hot reloading occurs instantly in-game when changes are made.";
		}
	}
}
