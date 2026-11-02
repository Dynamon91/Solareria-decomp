using System;
using Cysharp.Threading.Tasks;
using Rewired;
using SPR.Library;
using UnityEngine;

namespace SPR.UI.Option;

public class SettingOpen : SettingItem
{
	[SerializeField]
	private ControllerType controllerType;

	[SerializeField]
	private bool isDefault;

	public bool IsActive => controllerType switch
	{
		ControllerType.Keyboard => RewiredHelper.Keyboard != null, 
		ControllerType.Joystick => RewiredHelper.Gamepad != null, 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	private static InputManager InputManager => Game.Instance.InputManager;

	private static OptionCanvas OptionCanvas => Game.Instance.UIManager.OptionCanvas;

	public override void OnFocus(bool value)
	{
	}

	public override void Control(int horizontal, int vertical)
	{
	}

	public override void Synchronize()
	{
	}

	public override void Save()
	{
	}

	public override void OnConfirm()
	{
		OpenSettingMenu();
	}

	private void OpenSettingMenu()
	{
		if (!IsActive)
		{
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Common_Negative");
			return;
		}
		OptionCategory category = ((controllerType != ControllerType.Joystick) ? (isDefault ? OptionCategory.KeyboardDefault : OptionCategory.KeyboardUI) : (isDefault ? OptionCategory.GamepadDefault : OptionCategory.GamepadUI));
		OptionCanvas.ActivateInputSettingPanel(category).Forget();
	}
}
