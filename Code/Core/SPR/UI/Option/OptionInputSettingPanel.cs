using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using Rewired;
using SPR.Library;
using UnityEngine;

namespace SPR.UI.Option;

public class OptionInputSettingPanel : OptionPanel, IControllerEventUpdate
{
	[SerializeField]
	private ControllerType type;

	[SerializeField]
	private List<InputSettingItem> inputs;

	[SerializeField]
	private int columnSize;

	private List<InputSettingItem> focusableInputs;

	private InputSettingItem currentInputSetting;

	private int index;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	public override string ControllerName()
	{
		return "Option Input Setting Panel";
	}

	public override void Initialize()
	{
		base.Initialize();
		for (int i = 0; i < inputs.Count; i++)
		{
			InputSettingItem inputSettingItem = inputs[i];
			if (!inputSettingItem.IsNull())
			{
				inputSettingItem.SetActionText();
			}
		}
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	protected override void OnActivate()
	{
		index = 0;
		focusableInputs = new List<InputSettingItem>();
		for (int i = 0; i < inputs.Count; i++)
		{
			InputSettingItem inputSettingItem = inputs[i];
			bool flag = inputSettingItem is GamepadSubInputSettingItem || inputSettingItem is KeyboardSubInputSettingItem;
			focusableInputs.Add(flag ? null : inputSettingItem);
			if (!inputSettingItem.IsNull())
			{
				inputSettingItem.Synchronize();
				inputSettingItem.OnFocus(i == index);
			}
		}
		currentInputSetting = focusableInputs[index];
	}

	protected override void OnDeactivate()
	{
		focusableInputs = null;
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
	}

	protected override void OnControl()
	{
		int verticalDownUI = OptionPanel.InputManager.verticalDownUI;
		int horizontalDownUI = OptionPanel.InputManager.horizontalDownUI;
		int num = index;
		index = focusableInputs.CalculateTargetCellIndex(index, columnSize, -verticalDownUI, -horizontalDownUI);
		if (num != index)
		{
			FocusItem();
		}
		if (OptionPanel.InputManager.confirmDown)
		{
			OnConfirm();
		}
		currentInputSetting.Control(0, 0);
		if (OptionPanel.InputManager.reset)
		{
			ConfirmModel model = new ConfirmModel("ResetInput", ResetInputMap);
			ConfirmPopup.SetModel(model);
			OptionPanel.InputManager.StackControlTarget(ConfirmPopup).Forget();
		}
	}

	private void FocusItem()
	{
		InputSettingItem inputSettingItem = currentInputSetting;
		currentInputSetting = focusableInputs[index];
		inputSettingItem.OnFocus(value: false);
		currentInputSetting.OnFocus(value: true);
	}

	private void OnConfirm()
	{
		HandleRemappingInput().Forget();
	}

	protected override async UniTask DeactivateOption()
	{
		await OptionPanel.InputManager.RemoveControlTarget(this);
	}

	private async UniTaskVoid HandleRemappingInput()
	{
		if (!currentInputSetting.CanRemapping())
		{
			currentInputSetting.ShowWarning(hasNoAction: true);
			return;
		}
		await OptionPanel.InputManager.BlockControlForTask(currentInputSetting.StartMapping(), "Wait For Remapping");
		foreach (InputSettingItem input in inputs)
		{
			if (!input.IsNull())
			{
				input.Synchronize();
			}
		}
	}

	private void ResetInputMap()
	{
		ReInput.players.GetPlayer(0).controllers.maps.LoadDefaultMaps(type);
		if (type == ControllerType.Keyboard)
		{
			ReInput.players.GetPlayer(0).controllers.maps.LoadDefaultMaps(ControllerType.Mouse);
		}
		ReInput.userDataStore.Save();
		foreach (InputSettingItem input in inputs)
		{
			if (!input.IsNull())
			{
				input.Synchronize();
			}
		}
		RewiredHelper.UpdateInputIcons();
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		if (OptionPanel.InputManager.ContainsInControlStack(this) && type == ControllerType.Joystick && RewiredHelper.Gamepad == null)
		{
			OptionPanel.InputManager.RemoveControlTarget(this).Forget();
		}
	}

	private void OnDestroy()
	{
		focusableInputs = null;
		RewiredHelper.RemoveControllerChangeEvent(this);
	}
}
