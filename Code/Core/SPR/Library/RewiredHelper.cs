using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rewired;
using SPR.UI;
using SPR.UI.Option;

namespace SPR.Library;

public static class RewiredHelper
{
	public const string DEFAULT_CATEGORY = "Default";

	private const string UI_CATEGORY = "UI";

	private const string DefaultLayout = "Default";

	private static Player player;

	private static Controller mainController;

	public static ControllerType CurrentControllerType { get; private set; }

	public static Player Player => player ?? (player = ReInput.players.GetPlayer(0));

	public static bool IsJoystickConnected
	{
		get
		{
			if (mainController == null)
			{
				FindMainController();
			}
			return CurrentControllerType == ControllerType.Joystick;
		}
	}

	public static ControllerMapLayoutManager LayoutManager => Player.controllers.maps.layoutManager;

	public static Controller Mouse { get; private set; }

	public static Controller Keyboard { get; private set; }

	public static Controller Gamepad { get; private set; }

	public static Controller Controller
	{
		get
		{
			if (mainController != null)
			{
				return mainController;
			}
			FindMainController();
			return mainController;
		}
	}

	public static ControllerMap DefaultControllerMap
	{
		get
		{
			if (Controller != null)
			{
				return Player.controllers.maps.GetMap(Controller.type, Controller.id, "Default", "Default");
			}
			return null;
		}
	}

	public static ControllerMap UIControllerMap
	{
		get
		{
			if (Controller != null)
			{
				return Player.controllers.maps.GetMap(Controller.type, Controller.id, "UI", "Default");
			}
			return null;
		}
	}

	public static ControllerMap MouseControllerMap => Player.controllers.maps.GetMap(ControllerType.Mouse, 0, "Default", "Default");

	public static ControllerMap MouseUIControllerMap => Player.controllers.maps.GetMap(ControllerType.Mouse, 0, "UI", "Default");

	public static event Action<ControllerStatusChangedEventArgs> OtherControllerInputEvent;

	static RewiredHelper()
	{
		SetConnectedController();
		ReInput.ControllerConnectedEvent += OnControllerChanged;
		ReInput.ControllerDisconnectedEvent += OnControllerChanged;
		OtherControllerInputEvent += OnControllerChanged;
	}

	private static IEnumerable<Controller> GetValidControllers()
	{
		return Player.controllers.Controllers.Where((Controller controller) => controller.IsValidController());
	}

	public static void EnableUIInput(bool enable)
	{
		Player.controllers.maps.SetMapsEnabled(enable, "UI");
	}

	public static bool IsValidController(this Controller controller)
	{
		if (!controller.isConnected)
		{
			return false;
		}
		ControllerMap map = player.controllers.maps.GetMap(controller.type, controller.id, "Default", "Default");
		if (map == null)
		{
			return false;
		}
		if (map.elementMapCount == 0)
		{
			return false;
		}
		ControllerMap map2 = player.controllers.maps.GetMap(controller.type, controller.id, "UI", "Default");
		if (map2 == null)
		{
			return false;
		}
		if (map2.elementMapCount == 0)
		{
			return false;
		}
		return true;
	}

	public static void FindMainController()
	{
		StringBuilder stringBuilder = new StringBuilder("Find main controller...");
		List<Controller> list = GetValidControllers().ToList();
		if (list.Count == 0)
		{
			mainController = null;
			CurrentControllerType = ControllerType.Keyboard;
			stringBuilder.Append(" None of controllers are valid.");
		}
		else
		{
			stringBuilder.Append($" There are {list.Count} controllers.");
			mainController = list[0];
			CurrentControllerType = mainController.type;
			foreach (Controller item in list)
			{
				stringBuilder.Append("\n" + item.GetControllerInfoString());
				if (item.type == ControllerType.Joystick)
				{
					mainController = item;
					CurrentControllerType = mainController.type;
				}
			}
			stringBuilder.Append("\nMain controller Info [" + mainController.GetControllerInfoString() + "]");
		}
		DebugLog.ControllerLog(stringBuilder.ToString());
	}

	public static string GetControllerInfoString(this Controller controller)
	{
		return $"Type : {controller.type}, ID : {controller.id}, Name : {controller.hardwareName}";
	}

	public static string ToActionName(this KeyboardSettingKey.ControlCommands command)
	{
		string result = command.ToString();
		switch (command)
		{
		case KeyboardSettingKey.ControlCommands.Up:
		case KeyboardSettingKey.ControlCommands.Down:
			result = "Vertical";
			break;
		case KeyboardSettingKey.ControlCommands.Left:
		case KeyboardSettingKey.ControlCommands.Right:
			result = "Horizontal";
			break;
		case KeyboardSettingKey.ControlCommands.UpUI:
		case KeyboardSettingKey.ControlCommands.DownUI:
			result = "VerticalUI";
			break;
		case KeyboardSettingKey.ControlCommands.LeftUI:
		case KeyboardSettingKey.ControlCommands.RightUI:
			result = "HorizontalUI";
			break;
		}
		return result;
	}

	public static ActionElementMap ToActionElement(this KeyboardSettingKey.ControlCommands command)
	{
		string actionName = command.ToActionName();
		AxisRange axisRange = ((!new KeyboardSettingKey.ControlCommands[4]
		{
			KeyboardSettingKey.ControlCommands.Down,
			KeyboardSettingKey.ControlCommands.Left,
			KeyboardSettingKey.ControlCommands.DownUI,
			KeyboardSettingKey.ControlCommands.LeftUI
		}.Contains(command)) ? AxisRange.Positive : AxisRange.Negative);
		foreach (ActionElementMap item in DefaultControllerMap.ElementMapsWithAction(actionName))
		{
			if (item.axisRange != AxisRange.Full || !(item.axisContribution.ToString() != axisRange.ToString()))
			{
				return item;
			}
		}
		foreach (ActionElementMap item2 in UIControllerMap.ElementMapsWithAction(actionName))
		{
			if (item2.axisRange != AxisRange.Full || !(item2.axisContribution.ToString() != axisRange.ToString()))
			{
				return item2;
			}
		}
		foreach (ActionElementMap item3 in MouseControllerMap.ElementMapsWithAction(actionName))
		{
			if (item3.axisRange != AxisRange.Full || !(item3.axisContribution.ToString() != axisRange.ToString()))
			{
				return item3;
			}
		}
		foreach (ActionElementMap item4 in MouseUIControllerMap.ElementMapsWithAction(actionName))
		{
			if (item4.axisRange != AxisRange.Full || !(item4.axisContribution.ToString() != axisRange.ToString()))
			{
				return item4;
			}
		}
		DebugLog.ControllerLogError($"{command}이라는 ActionElementMap이 없습니다! Rewired의 Action 목록을 확인하세요.");
		return null;
	}

	public static string GetKeyCodeString(this KeyboardSettingKey.ControlCommands command)
	{
		string elementIdentifierName = command.ToActionElement().elementIdentifierName;
		if (elementIdentifierName.Length == 1)
		{
			return elementIdentifierName;
		}
		if (elementIdentifierName.Contains("Arrow"))
		{
			if (elementIdentifierName.Contains("Up"))
			{
				return "↑";
			}
			if (elementIdentifierName.Contains("Down"))
			{
				return "↓";
			}
			if (elementIdentifierName.Contains("Left"))
			{
				return "←";
			}
			if (elementIdentifierName.Contains("Right"))
			{
				return "→";
			}
		}
		if (elementIdentifierName.Contains("Shoulder"))
		{
			if (elementIdentifierName.Contains("Left"))
			{
				return "LB";
			}
			if (elementIdentifierName.Contains("Right"))
			{
				return "RB";
			}
		}
		if (elementIdentifierName.Contains("Shift"))
		{
			return "Shift";
		}
		return elementIdentifierName;
	}

	public static void RegisterControllerChangeEvent(IControllerEventUpdate ui)
	{
		ReInput.ControllerConnectedEvent += ui.OnControllerChanged;
		ReInput.ControllerDisconnectedEvent += ui.OnControllerChanged;
		OtherControllerInputEvent += ui.OnControllerChanged;
	}

	public static void RemoveControllerChangeEvent(IControllerEventUpdate ui)
	{
		ReInput.ControllerConnectedEvent -= ui.OnControllerChanged;
		ReInput.ControllerDisconnectedEvent -= ui.OnControllerChanged;
		OtherControllerInputEvent -= ui.OnControllerChanged;
	}

	public static void CheckOtherControllerInput()
	{
		Controller lastActiveController = Player.controllers.GetLastActiveController();
		if (lastActiveController == null)
		{
			return;
		}
		ControllerType controllerType = lastActiveController.type;
		if (controllerType == ControllerType.Mouse)
		{
			controllerType = ControllerType.Keyboard;
		}
		if (Controller.type != controllerType)
		{
			Controller controller = player.controllers.Controllers.FirstOrDefault((Controller c) => c.type == controllerType);
			if (controller != null)
			{
				mainController = controller;
			}
			RewiredHelper.OtherControllerInputEvent?.Invoke(null);
		}
	}

	public static ControllerMap GetControllerMap(Controller controller, int category)
	{
		return Player.controllers.maps.GetMap(controller, category, 0);
	}

	public static bool HasInput(Controller controller, string action)
	{
		if (controller == null)
		{
			return false;
		}
		ActionElementMap actionElementMap = null;
		ActionElementMap actionElementMap2 = GetControllerMap(controller, 0).AllMaps.ToList().Find((ActionElementMap m) => m.actionDescriptiveName == action);
		if (controller.type != ControllerType.Mouse)
		{
			actionElementMap = GetControllerMap(controller, 1).AllMaps.ToList().Find((ActionElementMap m) => m.actionDescriptiveName == action);
		}
		if (actionElementMap2 == null)
		{
			return actionElementMap != null;
		}
		return true;
	}

	public static void UpdateInputIcons()
	{
		RewiredHelper.OtherControllerInputEvent?.Invoke(null);
	}

	private static void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		SetConnectedController();
		if (args == null)
		{
			CurrentControllerType = mainController.type;
			return;
		}
		if (args.controller == null)
		{
			FindMainController();
		}
		else
		{
			mainController = args.controller;
		}
		CurrentControllerType = mainController.type;
	}

	private static void SetConnectedController()
	{
		foreach (Controller controller in Player.controllers.Controllers)
		{
			switch (controller.type)
			{
			case ControllerType.Keyboard:
				if (Keyboard == null)
				{
					Keyboard = controller;
				}
				break;
			case ControllerType.Mouse:
				if (Mouse == null)
				{
					Mouse = controller;
				}
				break;
			case ControllerType.Joystick:
				if (Gamepad == null || Gamepad.name != controller.name)
				{
					Gamepad = controller;
				}
				break;
			}
		}
		if (Player.controllers.Controllers.All((Controller c) => c.type != ControllerType.Joystick))
		{
			Gamepad = null;
		}
	}
}
