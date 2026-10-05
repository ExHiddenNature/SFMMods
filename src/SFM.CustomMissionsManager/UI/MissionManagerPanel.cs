using System;
using System.Collections.Generic;
using ExposureUnnoticed2.Object3D.IngameManager;
using ExposureUnnoticed2.Scripts.Base;
using ExposureUnnoticed2.Scripts.InGame;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace SFM.CustomMissionsManager.UI;

public class MissionManagerPanel : PanelBase
{
	public override string Name => "SFM Custom Missions Manager";
	public override int MinWidth => 780;
	public override int MinHeight => 520;
	public override Vector2 DefaultAnchorMin => new(0.12f, 0.08f);
	public override Vector2 DefaultAnchorMax => new(0.88f, 0.92f);

	private Text _activeSlotLabel = null!;
	private Dropdown _slotSelectorDropdown = null!;

	private ButtonRef _missionsTabBtn = null!;
	private ButtonRef _packsTabBtn = null!;
	private ButtonRef _profilesTabBtn = null!;
	private ButtonRef _settingsTabBtn = null!;

	private MissionsTab _missionsTab = null!;
	private PacksTab _packsTab = null!;
	private ProfilesTab _profilesTab = null!;
	private SettingsTab _settingsTab = null!;

	public enum TabType
	{
		Missions,
		Packs,
		Profiles,
		Settings
	}

	private TabType _currentTab = TabType.Missions;

	public MissionManagerPanel(UIBase owner) : base(owner)
	{
		Plugin.StateManager.OnStateChanged += OnStateManagerChanged;
	}

	public override void SetActive(bool active)
	{
		if (!active && UniverseLib.Input.InputManager.Rebinding)
		{
			UniverseLib.Input.InputManager.EndRebind();
		}

		base.SetActive(active);

		try
		{
			UniversalUI.SetUIActive(MyPluginInfo.PLUGIN_GUID, active);
		}
		catch { }

		SyncCursorState(active);
	}

	private void SyncCursorState(bool uiActive)
	{
		try
		{
			if (uiActive)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				if (CursorController.Instance != null)
					CursorController.SetVisible(true);
			}
			else
			{
				bool inGame = InGameManager.Instance != null;
				bool shouldBeVisible = false;

				if (inGame)
				{
					if (InGameUiManager.Instance != null)
					{
						// In-game: only keep cursor visible if a native game panel (e.g., pause menu, dialogue) is active
						shouldBeVisible = InGameUiManager.Instance.basePanelStack != null && InGameUiManager.Instance.basePanelStack.Count > 0;
						InGameUiManager.Instance.IsOpenSomeCancelInputUi = shouldBeVisible;
					}
				}
				else
				{
					// At title/main menu, cursor should always be visible
					shouldBeVisible = true;
				}

				if (shouldBeVisible)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
					if (CursorController.Instance != null)
						CursorController.SetVisible(true);
				}
				else
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
					if (CursorController.Instance != null)
						CursorController.SetVisible(false);
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[MissionManagerPanel] Error syncing cursor state: {ex}");
		}
	}

	protected override void ConstructPanelContent()
	{
		// Top Toolbar
		var toolbar = UIFactory.CreateHorizontalGroup(ContentRoot, "TopToolbar", false, false, true, true, 8,
			new Vector4(6, 4, 6, 4), UIHelpers.HeaderBgColor, TextAnchor.MiddleLeft);
		UIFactory.SetLayoutElement(toolbar, minHeight: 34, flexibleWidth: 9999);

		// Active Slot Badge
		_activeSlotLabel = UIFactory.CreateLabel(toolbar, "ActiveSlotLabel", "[Active: Default]", TextAnchor.MiddleLeft,
			UIHelpers.SuccessGreen, true, 13);
		UIFactory.SetLayoutElement(_activeSlotLabel.gameObject, minWidth: 150, flexibleWidth: 0);

		// Slot Selector label & dropdown
		var editLabel = UIFactory.CreateLabel(toolbar, "EditLabel", "Editing:", TextAnchor.MiddleRight, UIHelpers.TextMuted, true, 12);
		UIFactory.SetLayoutElement(editLabel.gameObject, minWidth: 50, flexibleWidth: 0);

		var slotOptions = new List<string> { "Default (Title / Template)" };
		for (int i = 0; i < 20; i++)
			slotOptions.Add($"Save {i + 1} (Slot {i})");

		var dropdownObj = UIFactory.CreateDropdown(toolbar, "SlotDropdown", out _slotSelectorDropdown, "Default", 12, OnSlotDropdownChanged, slotOptions.ToArray());
		UIFactory.SetLayoutElement(dropdownObj, minWidth: 180, minHeight: 26, flexibleWidth: 0);

		// Spacer
		var spacer = UIFactory.CreateUIObject("Spacer", toolbar);
		UIFactory.SetLayoutElement(spacer, flexibleWidth: 9999);

		// Manual Reload button
		var reloadBtn = UIFactory.CreateButton(toolbar, "ReloadBtn", "⚡ Reload In-Game", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(reloadBtn.Component.gameObject, minWidth: 130, minHeight: 26, flexibleWidth: 0);
		reloadBtn.OnClick += () =>
		{
			Plugin.ReloadService.ReloadAll();
		};

		// Tab Buttons Bar
		var tabBar = UIFactory.CreateHorizontalGroup(ContentRoot, "TabBar", false, false, true, true, 4,
			new Vector4(4, 2, 4, 2), UIHelpers.ElementBgColor, TextAnchor.MiddleLeft);
		UIFactory.SetLayoutElement(tabBar, minHeight: 30, flexibleWidth: 9999);

		_missionsTabBtn = UIFactory.CreateButton(tabBar, "Tab_Missions", "📜 Missions", UIHelpers.ButtonHighlightColor);
		UIFactory.SetLayoutElement(_missionsTabBtn.Component.gameObject, minWidth: 100, minHeight: 26);
		_missionsTabBtn.OnClick += () => SwitchTab(TabType.Missions);

		_packsTabBtn = UIFactory.CreateButton(tabBar, "Tab_Packs", "📦 Mod Packs", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(_packsTabBtn.Component.gameObject, minWidth: 100, minHeight: 26);
		_packsTabBtn.OnClick += () => SwitchTab(TabType.Packs);

		_profilesTabBtn = UIFactory.CreateButton(tabBar, "Tab_Profiles", "💾 Slot Profiles", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(_profilesTabBtn.Component.gameObject, minWidth: 110, minHeight: 26);
		_profilesTabBtn.OnClick += () => SwitchTab(TabType.Profiles);

		_settingsTabBtn = UIFactory.CreateButton(tabBar, "Tab_Settings", "⚙️ Settings", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(_settingsTabBtn.Component.gameObject, minWidth: 90, minHeight: 26);
		_settingsTabBtn.OnClick += () => SwitchTab(TabType.Settings);

		// Tab Content Area
		var tabContentContainer = UIFactory.CreateUIObject("TabContentContainer", ContentRoot);
		UIFactory.SetLayoutGroup<VerticalLayoutGroup>(tabContentContainer, true, true, true, true);
		UIFactory.SetLayoutElement(tabContentContainer, flexibleWidth: 9999, flexibleHeight: 9999);

		_missionsTab = new MissionsTab(tabContentContainer);
		_packsTab = new PacksTab(tabContentContainer);
		_profilesTab = new ProfilesTab(tabContentContainer);
		_settingsTab = new SettingsTab(tabContentContainer);

		UpdateHeaderLabels();
		SwitchTab(TabType.Missions);
	}

	public void SwitchTab(TabType tab)
	{
		_currentTab = tab;

		_missionsTab.Root.SetActive(tab == TabType.Missions);
		_packsTab.Root.SetActive(tab == TabType.Packs);
		_profilesTab.Root.SetActive(tab == TabType.Profiles);
		_settingsTab.Root.SetActive(tab == TabType.Settings);

		// Update button visual highlights
		UpdateButtonState(_missionsTabBtn, tab == TabType.Missions);
		UpdateButtonState(_packsTabBtn, tab == TabType.Packs);
		UpdateButtonState(_profilesTabBtn, tab == TabType.Profiles);
		UpdateButtonState(_settingsTabBtn, tab == TabType.Settings);

		// Trigger refresh of active tab
		switch (tab)
		{
			case TabType.Missions:
				_missionsTab.Refresh();
				break;
			case TabType.Packs:
				_packsTab.Refresh();
				break;
			case TabType.Profiles:
				_profilesTab.Refresh();
				break;
		}
	}

	private static void UpdateButtonState(ButtonRef btn, bool isActive)
	{
		var color = isActive ? UIHelpers.ButtonHighlightColor : UIHelpers.ButtonNormalColor;
		RuntimeHelper.SetColorBlock(btn.Component, color, color * 1.2f, color * 0.8f);
	}

	private void OnSlotDropdownChanged(int index)
	{
		if (index == 0)
			Plugin.StateManager.SelectedSlotKey = "default";
		else
			Plugin.StateManager.SelectedSlotKey = $"slot_{index - 1}";
	}

	private void OnStateManagerChanged()
	{
		UpdateHeaderLabels();
		RefreshCurrentTab();
	}

	private void UpdateHeaderLabels()
	{
		if (_activeSlotLabel == null || _slotSelectorDropdown == null)
			return;

		int activeSlot = Plugin.StateManager.CurrentSlot;
		bool inGame = InGameManager.Instance != null;
		_activeSlotLabel.text = activeSlot >= 0
			? (inGame ? $"[In Game: Save {activeSlot + 1}]" : $"[Active: Save {activeSlot + 1}]")
			: (inGame ? "[In Game: Default]" : "[Active: Default]");

		// Sync dropdown index to SelectedSlotKey
		string selectedKey = Plugin.StateManager.SelectedSlotKey;
		int targetIndex = 0;
		if (selectedKey.StartsWith("slot_") && int.TryParse(selectedKey[5..], out int slotNum))
			targetIndex = slotNum + 1;

		if (_slotSelectorDropdown.value != targetIndex)
			_slotSelectorDropdown.SetValueWithoutNotify(targetIndex);

		_slotSelectorDropdown.RefreshShownValue();
	}

	private void RefreshCurrentTab()
	{
		switch (_currentTab)
		{
			case TabType.Missions:
				_missionsTab.Refresh();
				break;
			case TabType.Packs:
				_packsTab.Refresh();
				break;
			case TabType.Profiles:
				_profilesTab.Refresh();
				break;
		}
	}
}
