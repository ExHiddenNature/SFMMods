using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using SFM.CustomMissionsManager.Configuration;
using SFM.CustomMissionsManager.Models;

namespace SFM.CustomMissionsManager.UI;

public class PacksTab
{
	public GameObject Root { get; }

	private readonly InputFieldRef _newPackInput;
	private readonly InputFieldRef _memberSearchInput;

	private GameObject _packsListContent = null!;
	private GameObject _membersListContent = null!;
	private Text _selectedPackTitle = null!;
	private Text _selectedPackStats = null!;

	private MissionPack? _selectedPack;
	private string _memberSearchText = string.Empty;

	public PacksTab(GameObject parent)
	{
		Root = UIFactory.CreateHorizontalGroup(parent, "PacksTab", false, true, true, true, 6, new Vector4(4, 4, 4, 4), UIHelpers.PanelBgColor);
		UIFactory.SetLayoutElement(Root, flexibleWidth: 9999, flexibleHeight: 9999);

		// ==================== LEFT COLUMN: Packs List ====================
		var leftCol = UIFactory.CreateVerticalGroup(Root, "LeftColumn", true, false, true, true, 4, new Vector4(4, 4, 4, 4), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(leftCol, minWidth: 270, flexibleWidth: 0, flexibleHeight: 9999);

		// Create Pack Row
		var createRow = UIFactory.CreateHorizontalGroup(leftCol, "CreateRow", false, false, true, true, 4);
		UIFactory.SetLayoutElement(createRow, minHeight: 28, flexibleWidth: 9999);

		_newPackInput = UIFactory.CreateInputField(createRow, "NewPackInput", "New pack name...");
		UIFactory.SetLayoutElement(_newPackInput.Component.gameObject, minWidth: 160, flexibleWidth: 1, minHeight: 28);

		var createBtn = UIFactory.CreateButton(createRow, "CreateBtn", "Add", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(createBtn.Component.gameObject, minWidth: 50, minHeight: 28);
		createBtn.OnClick += OnCreatePackClicked;

		// Packs Scroll View
		var packsScroll = UIFactory.CreateScrollView(leftCol, "PacksScrollView", out _packsListContent, out _, new Color(0.08f, 0.08f, 0.1f, 0.8f));
		UIFactory.SetLayoutElement(packsScroll, flexibleWidth: 9999, flexibleHeight: 9999);

		// ==================== RIGHT COLUMN: Pack Editor ====================
		var rightCol = UIFactory.CreateVerticalGroup(Root, "RightColumn", true, false, true, true, 4, new Vector4(6, 4, 6, 4), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(rightCol, minWidth: 300, flexibleWidth: 9999, flexibleHeight: 9999);

		// Selected pack header
		var headerRow = UIFactory.CreateHorizontalGroup(rightCol, "HeaderRow", false, false, true, true, 6);
		UIFactory.SetLayoutElement(headerRow, minHeight: 28, flexibleWidth: 9999);

		_selectedPackTitle = UIFactory.CreateLabel(headerRow, "PackTitle", "Select a Pack", TextAnchor.MiddleLeft, Color.white, true, 15);
		UIFactory.SetLayoutElement(_selectedPackTitle.gameObject, flexibleWidth: 9999);

		_selectedPackStats = UIFactory.CreateLabel(headerRow, "PackStats", "", TextAnchor.MiddleRight, UIHelpers.TextMuted, true, 12);
		UIFactory.SetLayoutElement(_selectedPackStats.gameObject, minWidth: 120, flexibleWidth: 0);

		// Member search filter
		var searchRow = UIFactory.CreateHorizontalGroup(rightCol, "SearchRow", false, false, true, true, 4);
		UIFactory.SetLayoutElement(searchRow, minHeight: 28, flexibleWidth: 9999);

		_memberSearchInput = UIFactory.CreateInputField(searchRow, "MemberSearch", "Filter available missions...");
		UIFactory.SetLayoutElement(_memberSearchInput.Component.gameObject, flexibleWidth: 9999, minHeight: 26);
		_memberSearchInput.OnValueChanged += text =>
		{
			_memberSearchText = text;
			RefreshMemberList();
		};

		// Members Scroll View
		var membersScroll = UIFactory.CreateScrollView(rightCol, "MembersScrollView", out _membersListContent, out _, new Color(0.08f, 0.08f, 0.1f, 0.8f));
		UIFactory.SetLayoutElement(membersScroll, flexibleWidth: 9999, flexibleHeight: 9999);

		Refresh();
	}

	public void Refresh()
	{
		RefreshPacksList();
		RefreshMemberList();
	}

	private void OnCreatePackClicked()
	{
		var text = _newPackInput.Text?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			Plugin.StateManager.CreatePack(text);
			_newPackInput.Text = "";
			_selectedPack = Plugin.StateManager.Config.Packs.FirstOrDefault(p => string.Equals(p.Name, text, StringComparison.OrdinalIgnoreCase));
			Refresh();
		}
	}

	private void RefreshPacksList()
	{
		for (int i = _packsListContent.transform.childCount - 1; i >= 0; i--)
			UnityEngine.Object.Destroy(_packsListContent.transform.GetChild(i).gameObject);

		var packs = Plugin.StateManager.Config.Packs;
		var selectedSlot = Plugin.StateManager.SelectedSlotKey;

		if (_selectedPack == null && packs.Count > 0)
			_selectedPack = packs[0];

		foreach (var pack in packs)
		{
			var row = UIFactory.CreateHorizontalGroup(_packsListContent, $"Pack_{pack.Name}", false, false, true, true, 4, new Vector4(4, 2, 4, 2), _selectedPack == pack ? UIHelpers.ElementActiveBgColor : UIHelpers.ElementBgColor, TextAnchor.MiddleLeft);
			UIFactory.SetLayoutElement(row, minHeight: 32, flexibleWidth: 9999);

			// Active in slot toggle
			bool isPackActive = Plugin.StateManager.IsPackActive(pack.Name, selectedSlot);
			var toggleObj = UIFactory.CreateToggle(row, "Toggle", out var toggle, out _, isPackActive ? UIHelpers.SuccessGreen : UIHelpers.ElementBgColor, 18, 18);
			toggle.isOn = isPackActive;
			UIFactory.SetLayoutElement(toggleObj, minWidth: 22, flexibleWidth: 0);

			var p = pack;
			toggle.onValueChanged.AddListener((UnityAction<bool>)((bool val) =>
			{
				Plugin.StateManager.SetPackEnabled(p.Name, val, selectedSlot);
			}));

			// Select pack button
			var selectBtn = UIFactory.CreateButton(row, "SelectBtn", $"{pack.Name} ({pack.Missions.Count})", UIHelpers.ButtonNormalColor);
			UIFactory.SetLayoutElement(selectBtn.Component.gameObject, minWidth: 140, flexibleWidth: 9999, minHeight: 26);
			selectBtn.OnClick += () =>
			{
				_selectedPack = p;
				Refresh();
			};

			// Delete button
			var delBtn = UIFactory.CreateButton(row, "DelBtn", "X", UIHelpers.DangerRed);
			UIFactory.SetLayoutElement(delBtn.Component.gameObject, minWidth: 24, minHeight: 24, flexibleWidth: 0);
			delBtn.OnClick += () =>
			{
				Plugin.StateManager.DeletePack(p.Name);
				if (_selectedPack == p)
					_selectedPack = Plugin.StateManager.Config.Packs.FirstOrDefault();
				Refresh();
			};
		}
	}

	private void RefreshMemberList()
	{
		for (int i = _membersListContent.transform.childCount - 1; i >= 0; i--)
			UnityEngine.Object.Destroy(_membersListContent.transform.GetChild(i).gameObject);

		if (_selectedPack == null)
		{
			_selectedPackTitle.text = "No Pack Selected";
			_selectedPackStats.text = "";
			return;
		}

		_selectedPackTitle.text = $"Pack: {_selectedPack.Name}";
		_selectedPackStats.text = $"{_selectedPack.Missions.Count} missions assigned";

		var allMissions = Plugin.Registry.AllMissions.AsEnumerable();
		if (!string.IsNullOrWhiteSpace(_memberSearchText))
		{
			allMissions = allMissions.Where(m =>
				m.DirectoryName.Contains(_memberSearchText, StringComparison.OrdinalIgnoreCase) ||
				(!string.IsNullOrEmpty(m.Title) && m.Title.Contains(_memberSearchText, StringComparison.OrdinalIgnoreCase))
			);
		}

		foreach (var mission in allMissions)
		{
			bool isInPack = _selectedPack.Missions.Any(m => string.Equals(m, mission.DirectoryName, StringComparison.OrdinalIgnoreCase));

			var row = UIFactory.CreateHorizontalGroup(_membersListContent, $"Member_{mission.DirectoryName}", false, false, true, true, 6, new Vector4(6, 2, 6, 2), UIHelpers.ElementBgColor, TextAnchor.MiddleLeft);
			UIFactory.SetLayoutElement(row, minHeight: 28, flexibleWidth: 9999);

			var toggleObj = UIFactory.CreateToggle(row, "Toggle", out var toggle, out _, isInPack ? UIHelpers.SuccessGreen : UIHelpers.ElementBgColor, 18, 18);
			toggle.isOn = isInPack;
			UIFactory.SetLayoutElement(toggleObj, minWidth: 22, flexibleWidth: 0);

			var m = mission;
			toggle.onValueChanged.AddListener((UnityAction<bool>)((bool _) =>
			{
				Plugin.StateManager.ToggleMissionInPack(_selectedPack.Name, m.DirectoryName);
				RefreshMemberList();
				RefreshPacksList();
			}));

			var badgeColor = mission.Type == MissionType.V1 ? UIHelpers.V1BadgeColor : UIHelpers.V2BadgeColor;
			var badgeText = mission.Type == MissionType.V1 ? "[v1]" : "[v2]";
			var badge = UIFactory.CreateLabel(row, "Badge", badgeText, TextAnchor.MiddleCenter, badgeColor, true, 11);
			UIFactory.SetLayoutElement(badge.gameObject, minWidth: 30, flexibleWidth: 0);

			var label = UIFactory.CreateLabel(row, "Name", mission.DisplayName, TextAnchor.MiddleLeft, Color.white, true, 12);
			UIFactory.SetLayoutElement(label.gameObject, flexibleWidth: 9999);
		}
	}
}
