using System.Collections.Generic;
using System.Linq;
using Rewired;
using SPR.Library;
using SPR.UI.Menu;
using UnityEngine;

namespace SPR.UI.Option;

public class OptionKeyboardPanel : MenuPanel, IControllerEventUpdate
{
	[SerializeField]
	private List<KeyboardSettingKey> settingKeys;

	private InputMapper.ConflictFoundEventData _conflictData;

	private ControllerType _selectedControllerType;

	private int _selectedControllerId;

	private bool _listenMode;

	private KeyboardSettingKey focusedKey;

	private bool ListenMode
	{
		set
		{
			if (value != _listenMode)
			{
				_listenMode = value;
				if (_listenMode)
				{
					focusedKey.OnMapNewKey();
				}
			}
		}
	}

	public override MenuCategory Category => MenuCategory.System;

	public InputMapper InputMapper { get; } = new InputMapper();

	protected override void OnActivate()
	{
		Synchronize();
		InputMapper.options.timeout = 5f;
		InputMapper.options.ignoreMouseXAxis = true;
		InputMapper.options.ignoreMouseYAxis = true;
		RewiredHelper.RegisterControllerChangeEvent(this);
		InputMapper.InputMappedEvent += OnInputMapped;
		InputMapper.ConflictFoundEvent += OnConflictFound;
		InputMapper.StoppedEvent += OnStopped;
	}

	protected override void OnControl()
	{
	}

	protected override void OnDeactivate()
	{
		InputMapper.Stop();
		InputMapper.RemoveAllEventListeners();
		ReInput.ControllerConnectedEvent -= OnControllerChanged;
		ReInput.ControllerDisconnectedEvent -= OnControllerChanged;
		foreach (KeyboardSettingKey settingKey in settingKeys)
		{
			settingKey.Save();
		}
	}

	public void Synchronize()
	{
		if (RewiredHelper.Controller == null)
		{
			DebugLog.GeneralLog("No Controller Selected");
			ClearUI();
			return;
		}
		DebugLog.GeneralLog("Keymap Setting Synchronized. Controller : " + RewiredHelper.Controller.name);
		if (RewiredHelper.IsJoystickConnected)
		{
			return;
		}
		foreach (KeyboardSettingKey settingKey in settingKeys)
		{
			settingKey.Synchronize();
		}
	}

	private void ClearUI()
	{
		foreach (KeyboardSettingKey settingKey in settingKeys)
		{
			settingKey.Synchronize();
		}
	}

	private void SetSelectedController(ControllerType controllerType)
	{
		bool flag = false;
		if (controllerType != _selectedControllerType)
		{
			_selectedControllerType = controllerType;
			flag = true;
		}
		int selectedControllerId = _selectedControllerId;
		if (_selectedControllerType == ControllerType.Joystick)
		{
			if (RewiredHelper.Player.controllers.joystickCount > 0)
			{
				_selectedControllerId = RewiredHelper.Player.controllers.Joysticks[0].id;
			}
			else
			{
				_selectedControllerId = -1;
			}
		}
		else
		{
			_selectedControllerId = 0;
		}
		if (_selectedControllerId != selectedControllerId)
		{
			flag = true;
		}
		if (flag)
		{
			InputMapper.Stop();
			Synchronize();
		}
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		RewiredHelper.FindMainController();
		SetSelectedController(_selectedControllerType);
	}

	private void OnConflictFound(InputMapper.ConflictFoundEventData data)
	{
		_conflictData = data;
		List<ElementAssignmentConflictInfo> conflictedActions = _conflictData.conflicts.Where((ElementAssignmentConflictInfo map) => map.isConflict).ToList();
		DebugLog.GeneralLog("conflict with: " + conflictedActions[0].action.name);
		List<KeyboardSettingKey> list = settingKeys.Where((KeyboardSettingKey key) => conflictedActions.Select((ElementAssignmentConflictInfo map) => map.actionId).Contains(key.ActionId) && key.ElementIdentifier == _conflictData.assignment.elementIdentifier.name).ToList();
		DebugLog.GeneralLog($"conflict with: {list[0].CommandType}");
		list.ForEach(delegate(KeyboardSettingKey key)
		{
			key.OnConflict();
		});
		_conflictData.responseCallback(InputMapper.ConflictResponse.Ignore);
	}

	private void OnInputMapped(InputMapper.InputMappedEventData data)
	{
		DebugLog.GeneralLog($"Remapped {data.actionElementMap.actionDescriptiveName} : {data.actionElementMap.keyCode}");
		ReInput.userDataStore.Save();
		Synchronize();
		ListenMode = false;
	}

	private void OnStopped(InputMapper.StoppedEventData data)
	{
		Debug.Log("Mapping Stopped");
		RewiredHelper.EnableUIInput(enable: true);
		Synchronize();
		ListenMode = false;
	}
}
