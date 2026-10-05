using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;
using SFM.CustomMissionsManager.Models;

namespace SFM.CustomMissionsManager.UI;

public class MissionsTab
{
	public GameObject Root { get; }

	private readonly InputFieldRef _searchInput;
	private Dropdown _typeDropdown = null!;
	private Dropdown _stateDropdown = null!;
	private Text _countLabel = null!;
	private GameObject _scrollContent = null!;
	private AutoSliderScrollbar _autoScrollbar = null!;

	private string _searchText = string.Empty;
	private int _typeFilter = 0; // 0 = All, 1 = V1, 2 = V2
	private int _stateFilter = 0; // 0 = All, 1 = Enabled, 2 = Disabled

	public MissionsTab(GameObject parent)
	{
		Root = UIFactory.CreateVerticalGroup(parent, "MissionsTab", true, true, true, true, 4, new Vector4(4, 4, 4, 4), UIHelpers.PanelBgColor);
		UIFactory.SetLayoutElement(Root, flexibleWidth: 9999, flexibleHeight: 9999);

		// Controls / Filter bar
		var filterBar = UIFactory.CreateHorizontalGroup(Root, "FilterBar", false, false, true, true, 6, new Vector4(2, 2, 2, 2), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(filterBar, minHeight: 32, flexibleWidth: 9999);

		// Search input
		_searchInput = UIFactory.CreateInputField(filterBar, "SearchInput", "Search missions...");
		UIFactory.SetLayoutElement(_searchInput.Component.gameObject, minWidth: 180, flexibleWidth: 1, minHeight: 28);
		_searchInput.OnValueChanged += text =>
		{
			_searchText = text;
			RefreshMissionList();
		};

		// Type dropdown
		var typeObj = UIFactory.CreateDropdown(filterBar, "TypeDropdown", out _typeDropdown, "All Types", 13, val =>
		{
			_typeFilter = val;
			RefreshMissionList();
		}, ["All Formats", "v1 Only", "v2 Only"]);
		UIFactory.SetLayoutElement(typeObj, minWidth: 100, minHeight: 28);

		// State dropdown
		var stateObj = UIFactory.CreateDropdown(filterBar, "StateDropdown", out _stateDropdown, "All States", 13, val =>
		{
			_stateFilter = val;
			RefreshMissionList();
		}, ["All States", "Enabled", "Disabled"]);
		UIFactory.SetLayoutElement(stateObj, minWidth: 100, minHeight: 28);

		// Enable all filtered button
		var enableAllBtn = UIFactory.CreateButton(filterBar, "EnableAllBtn", "Enable All", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(enableAllBtn.Component.gameObject, minWidth: 80, minHeight: 28);
		enableAllBtn.OnClick += OnEnableAllFiltered;

		// Disable all filtered button
		var disableAllBtn = UIFactory.CreateButton(filterBar, "DisableAllBtn", "Disable All", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(disableAllBtn.Component.gameObject, minWidth: 80, minHeight: 28);
		disableAllBtn.OnClick += OnDisableAllFiltered;

		// Summary stats bar
		var summaryBar = UIFactory.CreateHorizontalGroup(Root, "SummaryBar", false, false, true, true, 4, new Vector4(4, 1, 4, 1), UIHelpers.ElementBgColor);
		UIFactory.SetLayoutElement(summaryBar, minHeight: 22, flexibleWidth: 9999);

		_countLabel = UIFactory.CreateLabel(summaryBar, "CountLabel", "Showing 0 missions", TextAnchor.MiddleLeft, UIHelpers.TextMuted, true, 12);
		UIFactory.SetLayoutElement(_countLabel.gameObject, flexibleWidth: 9999);

		// Scroll area
		var scrollObj = UIFactory.CreateScrollView(Root, "MissionsScrollView", out _scrollContent, out _autoScrollbar, new Color(0.08f, 0.08f, 0.1f, 0.8f));
		UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);

		RefreshMissionList();
	}

	public void Refresh()
	{
		RefreshMissionList();
	}

	private IEnumerable<MissionInfo> GetFilteredMissions()
	{
		var query = Plugin.Registry.AllMissions.AsEnumerable();
		var selectedSlot = Plugin.StateManager.SelectedSlotKey;

		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			var search = _searchText.Trim();
			query = query.Where(m =>
				m.DirectoryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
				(!string.IsNullOrEmpty(m.Title) && m.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
				(!string.IsNullOrEmpty(m.Description) && m.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
			);
		}

		if (_typeFilter == 1)
			query = query.Where(m => m.Type == MissionType.V1);
		else if (_typeFilter == 2)
			query = query.Where(m => m.Type == MissionType.V2);

		if (_stateFilter == 1)
			query = query.Where(m => Plugin.StateManager.IsMissionEnabled(m.DirectoryName, selectedSlot));
		else if (_stateFilter == 2)
			query = query.Where(m => !Plugin.StateManager.IsMissionEnabled(m.DirectoryName, selectedSlot));

		return query;
	}

	private void RefreshMissionList()
	{
		// Destroy old items
		for (int i = _scrollContent.transform.childCount - 1; i >= 0; i--)
			UnityEngine.Object.Destroy(_scrollContent.transform.GetChild(i).gameObject);

		var filtered = GetFilteredMissions().ToList();
		var selectedSlot = Plugin.StateManager.SelectedSlotKey;
		int total = Plugin.Registry.AllMissions.Count;
		int enabledCount = Plugin.Registry.AllMissions.Count(m => Plugin.StateManager.IsMissionEnabled(m.DirectoryName, selectedSlot));

		_countLabel.text = $"Showing {filtered.Count} of {total} missions ({enabledCount} enabled in {selectedSlot})";

		foreach (var mission in filtered)
			CreateMissionRow(_scrollContent, mission, selectedSlot);
	}

	private void CreateMissionRow(GameObject parent, MissionInfo mission, string slotKey)
	{
		bool isEnabled = Plugin.StateManager.IsMissionEnabled(mission.DirectoryName, slotKey);
		bool inActivePack = Plugin.StateManager.IsMissionInActivePack(mission.DirectoryName, slotKey);

		var row = UIFactory.CreateHorizontalGroup(parent, $"Row_{mission.DirectoryName}", false, false, true, true, 6, new Vector4(6, 4, 6, 4), UIHelpers.ElementBgColor, TextAnchor.MiddleLeft);
		UIFactory.SetLayoutElement(row, minHeight: 34, flexibleWidth: 9999);

		// Toggle Checkbox
		var toggleObj = UIFactory.CreateToggle(row, "Toggle", out var toggle, out _, isEnabled ? UIHelpers.SuccessGreen : UIHelpers.ElementBgColor, 20, 20);
		toggle.isOn = isEnabled;
		UIFactory.SetLayoutElement(toggleObj, minWidth: 26, flexibleWidth: 0, minHeight: 24);

		toggle.onValueChanged.AddListener((UnityAction<bool>)((bool val) =>
		{
			Plugin.StateManager.SetMissionEnabled(mission.DirectoryName, val, slotKey);
			RefreshMissionList();
		}));

		// Type Badge
		var badgeColor = mission.Type == MissionType.V1 ? UIHelpers.V1BadgeColor : UIHelpers.V2BadgeColor;
		var badgeText = mission.Type == MissionType.V1 ? "[v1]" : "[v2]";
		var badge = UIFactory.CreateLabel(row, "Badge", badgeText, TextAnchor.MiddleCenter, badgeColor, true, 12);
		UIFactory.SetLayoutElement(badge.gameObject, minWidth: 32, flexibleWidth: 0);

		// Title and folder name
		string mainText = mission.DisplayName;
		if (!string.IsNullOrEmpty(mission.Subtitle))
			mainText += $" <color=#999999>({mission.Subtitle})</color>";

		if (inActivePack)
			mainText += " <color=#F59E0B>[From Active Pack]</color>";

		var titleLabel = UIFactory.CreateLabel(row, "Title", mainText, TextAnchor.MiddleLeft, Color.white, true, 13);
		UIFactory.SetLayoutElement(titleLabel.gameObject, minWidth: 200, flexibleWidth: 9999);

		// Optional Description Button
		if (!string.IsNullOrWhiteSpace(mission.Description))
		{
			var descBtn = UIFactory.CreateButton(row, "DescBtn", "Info", UIHelpers.ButtonNormalColor);
			UIFactory.SetLayoutElement(descBtn.Component.gameObject, minWidth: 42, minHeight: 24, flexibleWidth: 0);
			descBtn.OnClick += () =>
			{
				Plugin.Log.LogInfo($"[{mission.DisplayName}] {mission.Description}");
			};
		}
	}

	private void OnEnableAllFiltered()
	{
		var selectedSlot = Plugin.StateManager.SelectedSlotKey;
		var names = GetFilteredMissions().Select(m => m.DirectoryName).ToList();
		Plugin.StateManager.SetMissionsEnabled(names, true, selectedSlot);
	}

	private void OnDisableAllFiltered()
	{
		var selectedSlot = Plugin.StateManager.SelectedSlotKey;
		if (string.IsNullOrWhiteSpace(_searchText) && _typeFilter == 0 && _stateFilter == 0)
		{
			Plugin.StateManager.DisableAllMissions(selectedSlot);
		}
		else
		{
			var names = GetFilteredMissions().Select(m => m.DirectoryName).ToList();
			Plugin.StateManager.SetMissionsEnabled(names, false, selectedSlot);
		}
	}
}
