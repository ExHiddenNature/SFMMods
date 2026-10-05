using System;
using ExposureUnnoticed2.Scripts.InGame;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;

namespace SFM.CustomMissionsManager.UI;

public class ProfilesTab
{
	public GameObject Root { get; }
	private GameObject _slotsListContent = null!;
	private Text _editingStatusLabel = null!;

	public ProfilesTab(GameObject parent)
	{
		Root = UIFactory.CreateVerticalGroup(parent, "ProfilesTab", true, true, true, true, 4, new Vector4(4, 4, 4, 4), UIHelpers.PanelBgColor);
		UIFactory.SetLayoutElement(Root, flexibleWidth: 9999, flexibleHeight: 9999);

		var topBar = UIFactory.CreateHorizontalGroup(Root, "TopBar", false, false, true, true, 6, new Vector4(4, 4, 4, 4), UIHelpers.HeaderBgColor);
		UIFactory.SetLayoutElement(topBar, minHeight: 30, flexibleWidth: 9999);

		var headerLabel = UIFactory.CreateLabel(topBar, "Header", "Save Slot Profiles", TextAnchor.MiddleLeft, Color.white, true, 13);
		UIFactory.SetLayoutElement(headerLabel.gameObject, minWidth: 160, flexibleWidth: 1);

		_editingStatusLabel = UIFactory.CreateLabel(topBar, "EditingStatus", "", TextAnchor.MiddleRight, UIHelpers.AccentBlue, true, 13);
		UIFactory.SetLayoutElement(_editingStatusLabel.gameObject, minWidth: 260, flexibleWidth: 0);

		var scrollObj = UIFactory.CreateScrollView(Root, "ProfilesScrollView", out _slotsListContent, out _, new Color(0.08f, 0.08f, 0.1f, 0.8f));
		UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);

		Refresh();
	}

	public void Refresh()
	{
		if (_editingStatusLabel != null)
			_editingStatusLabel.text = $"Editing: {GetDisplayName(Plugin.StateManager.SelectedSlotKey)}";

		for (int i = _slotsListContent.transform.childCount - 1; i >= 0; i--)
			UnityEngine.Object.Destroy(_slotsListContent.transform.GetChild(i).gameObject);

		// Default profile
		CreateSlotRow("default", "Default Profile (Title Menu / New Games)");

		// Slots 0 to 19 (Save 1 to 20)
		for (int i = 0; i < 20; i++)
			CreateSlotRow($"slot_{i}", $"Save {i + 1} (Slot {i})");
	}

	private void CreateSlotRow(string slotKey, string displayName)
	{
		bool isActive = string.Equals(Plugin.StateManager.ActiveSlotKey, slotKey, StringComparison.OrdinalIgnoreCase);
		bool isSelected = string.Equals(Plugin.StateManager.SelectedSlotKey, slotKey, StringComparison.OrdinalIgnoreCase);

		var rowColor = isSelected ? UIHelpers.ElementActiveBgColor : UIHelpers.ElementBgColor;
		var row = UIFactory.CreateHorizontalGroup(_slotsListContent, $"Slot_{slotKey}", false, false, true, true, 8, new Vector4(8, 4, 8, 4), rowColor, TextAnchor.MiddleLeft);
		UIFactory.SetLayoutElement(row, minHeight: 34, flexibleWidth: 9999);

		// Status tag
		string tag = "";
		if (isActive)
		{
			bool inGame = InGameManager.Instance != null;
			tag += inGame ? "<color=#34D399>[IN GAME]</color> " : "<color=#34D399>[ACTIVE]</color> ";
		}
		if (isSelected)
			tag += "<color=#60A5FA>[EDITING]</color> ";

		var nameLabel = UIFactory.CreateLabel(row, "Name", $"{tag}{displayName}", TextAnchor.MiddleLeft, Color.white, true, 13);
		UIFactory.SetLayoutElement(nameLabel.gameObject, minWidth: 260, flexibleWidth: 1);

		// Stats
		var profile = Plugin.StateManager.Config.GetOrCreateProfile(slotKey);
		string stats = $"{profile.ActivePacks.Count} packs, {profile.ExplicitlyEnabledMissions.Count} individual";
		var statsLabel = UIFactory.CreateLabel(row, "Stats", stats, TextAnchor.MiddleRight, UIHelpers.TextMuted, true, 12);
		UIFactory.SetLayoutElement(statsLabel.gameObject, minWidth: 140, flexibleWidth: 0);

		// Select for Editing Button
		var editBtn = UIFactory.CreateButton(row, "EditBtn", isSelected ? "Selected" : "Select", isSelected ? UIHelpers.ButtonHighlightColor : UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(editBtn.Component.gameObject, minWidth: 70, minHeight: 26, flexibleWidth: 0);
		editBtn.OnClick += () =>
		{
			Plugin.StateManager.SelectedSlotKey = slotKey;
		};

		// Copy from Default button
		if (slotKey != "default")
		{
			var copyBtn = UIFactory.CreateButton(row, "CopyBtn", "Copy Default", UIHelpers.ButtonNormalColor);
			UIFactory.SetLayoutElement(copyBtn.Component.gameObject, minWidth: 90, minHeight: 26, flexibleWidth: 0);
			copyBtn.OnClick += () =>
			{
				Plugin.StateManager.CopyProfile("default", slotKey);
				Refresh();
			};
		}

		// Reset button
		var resetBtn = UIFactory.CreateButton(row, "ResetBtn", "Clear", UIHelpers.ButtonNormalColor);
		UIFactory.SetLayoutElement(resetBtn.Component.gameObject, minWidth: 50, minHeight: 26, flexibleWidth: 0);
		resetBtn.OnClick += () =>
		{
			Plugin.StateManager.ResetProfile(slotKey);
			Refresh();
		};
	}

	private static string GetDisplayName(string slotKey)
	{
		if (string.Equals(slotKey, "default", StringComparison.OrdinalIgnoreCase))
			return "Default Profile";
		if (slotKey.StartsWith("slot_") && int.TryParse(slotKey[5..], out int slotNum))
			return $"Save {slotNum + 1} (Slot {slotNum})";
		return slotKey;
	}
}
