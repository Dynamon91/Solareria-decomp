using System.Collections.Generic;
using System.Linq;
using Library;
using Rewired;
using SPR.Library;
using SPR.UI.Option;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR.L10N;

public static class Locale
{
	private static LocaleFontSet fontSet;

	private const string HOLD_LOWER = "_hold";

	private static readonly string[] DualShockSprites = new string[3] { "Touchpad", "Start", "Select" };

	public static string KeyboardKeyListening => GetLocaleString("Option_KeyboardKeyListening");

	public static TMP_FontAsset GetLocaleTMPFont(TextType textType)
	{
		if (fontSet.IsNull())
		{
			AsyncOperationHandle<LocaleFontSet> asyncOperationHandle = Addressables.LoadAssetAsync<LocaleFontSet>("Locale Font Set");
			asyncOperationHandle.WaitForCompletion();
			fontSet = asyncOperationHandle.Result;
		}
		return fontSet.GetCurrentLocaleTMPFont(textType, Game.CurrentLanguage);
	}

	public static Font GetLocaleFont(TextType textType)
	{
		if (fontSet.IsNull())
		{
			AsyncOperationHandle<LocaleFontSet> asyncOperationHandle = Addressables.LoadAssetAsync<LocaleFontSet>("Locale Font Set");
			asyncOperationHandle.WaitForCompletion();
			fontSet = asyncOperationHandle.Result;
		}
		return fontSet.GetCurrentLocaleFont(textType, Game.CurrentLanguage);
	}

	public static string GetLocaleString(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return string.Empty;
		}
		Language currentLanguage = Game.CurrentLanguage;
		string locale;
		try
		{
			locale = Game.DataManager.GetLocale(key, currentLanguage);
		}
		catch
		{
			return "<i>@" + key + "</i>";
		}
		if (!TryParseIcon(locale, out var output))
		{
			return locale;
		}
		return output;
	}

	public static string GetLocaleString(string key, Language language)
	{
		if (string.IsNullOrEmpty(key))
		{
			return string.Empty;
		}
		string locale;
		try
		{
			locale = Game.DataManager.GetLocale(key, language);
		}
		catch
		{
			return "<i>@" + key + "</i>";
		}
		if (!TryParseIcon(locale, out var output))
		{
			return locale;
		}
		return output;
	}

	public static string GetLocaleFormat(string key, params string[] values)
	{
		string text = GetLocaleString(key);
		List<int> list = new List<int>();
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '{')
			{
				list.Add(i);
			}
		}
		if (values.Length != list.Count)
		{
			return null;
		}
		List<int> list2 = new List<int>();
		bool flag = list.Count > 1;
		if (flag)
		{
			for (int j = 0; j < list.Count; j++)
			{
				list2.Add(int.Parse(text[list[j] + 1].ToString()));
			}
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			int startIndex = list[num];
			text = text.Remove(startIndex, flag ? 3 : 2);
			text = text.Insert(startIndex, flag ? values[list2[num]] : values[num]);
		}
		return text;
	}

	public static string GetLocaleKeyboardKeyName(this KeyboardSettingKey.ControlCommands command)
	{
		return GetLocaleString($"Option_KeyboardKeyName_{command}");
	}

	public static string GetAltarLocaleText(FieldID fieldID)
	{
		string text = "Map_Altar_" + fieldID.ToString();
		string localeString = GetLocaleString(text);
		if (localeString.Equals("<i>" + text + "</i>"))
		{
			localeString = GetLocaleString("Map_Altar_Default");
		}
		return localeString;
	}

	public static string GetLocaledScenarioText(ScenarioData data, Language language)
	{
		if (data == null)
		{
			return string.Empty;
		}
		return language switch
		{
			Language.Korean => data.KO, 
			Language.English => data.EN, 
			Language.Japanese => data.JA, 
			Language.TraditionalChinese => data.ZH_HANT, 
			Language.SimplifiedChinese => data.ZH_HANS, 
			Language.German => data.DE, 
			Language.Spanish => data.ES, 
			Language.Portuguese => data.PT, 
			Language.French => data.FR, 
			_ => string.Empty, 
		};
	}

	public static bool TryParseIcon(string input, out string output)
	{
		input = ParseComboInput(input, RewiredHelper.CurrentControllerType != ControllerType.Joystick);
		output = input;
		string[] array = input.Split('<', '>');
		bool result = false;
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = new string((from c in text.ToLower()
				where !char.IsWhiteSpace(c)
				select c).ToArray());
			if (text2.Contains("icon="))
			{
				string text3 = ((!(text2.Split('=', '_')[1].ToLower() == "input")) ? "" : ParseInputIcon_Internal(text, RewiredHelper.CurrentControllerType == ControllerType.Joystick));
				string newValue = text3;
				output = output.Replace(text, newValue);
				result = true;
			}
		}
		return result;
	}

	public static bool TryParseIcon(string input, bool isJoystick, out string output)
	{
		input = ParseComboInput(input, !isJoystick);
		output = input;
		string[] array = input.Split('<', '>');
		bool result = false;
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = new string((from c in text.ToLower()
				where !char.IsWhiteSpace(c)
				select c).ToArray());
			if (text2.Contains("icon="))
			{
				string text3 = ((!(text2.Split('=', '_')[1].ToLower() == "input")) ? "" : ParseInputIcon_Internal(text, isJoystick));
				string newValue = text3;
				output = output.Replace(text, newValue);
				result = true;
			}
		}
		return result;
	}

	public static string ParseIcon(string action, bool isJoystick)
	{
		return ParseInputIcon_Internal(action, isJoystick);
	}

	public static string ParseInputIcon(string input, bool isJoystick)
	{
		string text = input.Replace("icon=input_", "");
		bool flag = false;
		if (text.Contains("chargeattack"))
		{
			text = text.Replace("chargeattack", "attack");
		}
		if (!isJoystick)
		{
			if (text.Contains("move_"))
			{
				text = text.Replace("move_", "");
			}
			if (text.Contains("camera_"))
			{
				text = text.Replace("camera_", "sight");
			}
		}
		if (text.EndsWith("_hold"))
		{
			text = text.Replace("_hold", "");
			flag = true;
		}
		string text2;
		switch (text)
		{
		case "move_up":
			if (!isJoystick)
			{
				goto default;
			}
			text2 = "Stick-L-Up";
			break;
		case "move_down":
			if (!isJoystick)
			{
				goto default;
			}
			text2 = "Stick-L-Down";
			break;
		case "move":
			text2 = (isJoystick ? "Stick-L-LR" : "keyboard_a\" tint><sprite name=\"keyboard_d");
			break;
		case "camera":
			if (!isJoystick)
			{
				goto default;
			}
			text2 = "Stick-R-UDLR";
			break;
		case "move_left":
			if (!isJoystick)
			{
				goto default;
			}
			text2 = "Stick-L-Left";
			break;
		case "move_right":
			if (!isJoystick)
			{
				goto default;
			}
			text2 = "Stick-L-Right";
			break;
		case "moveuiall":
			text2 = (isJoystick ? "Stick-L-UDLR" : "keyboard_w\" tint><sprite name=\"keyboard_a\" tint><sprite name=\"keyboard_s\" tint><sprite name=\"keyboard_d");
			break;
		case "movevertical":
			text2 = ((!isJoystick) ? (ActionToIcon("upui", isJoystick: false) + "\" tint><sprite name=\"" + ActionToIcon("downui", isJoystick: false)) : "Stick-L-UD");
			break;
		case "scroll":
			text2 = (isJoystick ? Scroll(joystick: true) : Scroll(joystick: false));
			break;
		case "interact":
			text2 = (isJoystick ? ActionToIcon("heat", isJoystick: true) : ActionToIcon("action", isJoystick: false));
			break;
		default:
			text2 = ActionToIcon(text, isJoystick);
			break;
		}
		string text3 = text2;
		if (flag && !text3.Contains(GetHoldPostfix(isJoystick)))
		{
			text3 += GetHoldPostfix(isJoystick);
		}
		if (isJoystick)
		{
			text3 = ApplyGamepadPrefix(text3);
		}
		return text3;
		static string Scroll(bool joystick)
		{
			string text4 = ActionToIcon("scrollup", joystick);
			string text5 = ActionToIcon("scrolldown", joystick);
			string value = (joystick ? "Stick-R-Up" : "mouse_wheel_scroll_up");
			string value2 = (joystick ? "Stick-R-Down" : "mouse_wheel_scroll_down");
			if (!text4.Equals(value) || !text5.Equals(value2))
			{
				return text4 + "\" tint><sprite name=\"" + (joystick ? ApplyGamepadPrefix(text5) : text5);
			}
			if (!joystick)
			{
				return "mouse_wheel_scroll";
			}
			return "Stick-R-UD";
		}
	}

	private static string ApplyGamepadPrefix(string spriteName)
	{
		if (string.IsNullOrEmpty(spriteName))
		{
			return string.Empty;
		}
		if (spriteName.Contains("none"))
		{
			return "keyboard_none";
		}
		if (spriteName.StartsWith("Stick-"))
		{
			return spriteName;
		}
		string text = RewiredHelper.Controller?.name?.ToLower();
		if (string.IsNullOrEmpty(text))
		{
			DebugLog.ControllerLogWarning("현재 컨트롤러 이름을 확인할 수 없어 Xbox 레이아웃을 fallback으로 적용합니다.");
			return "Xbox-" + spriteName;
		}
		string text2;
		if (text.Contains("sony"))
		{
			text2 = ((text.Contains("dualshock") && DualShockSprites.Any((string s) => spriteName.StartsWith(s))) ? "Ps4-" : "Ps-");
		}
		else if (text.Contains("xbox"))
		{
			text2 = "Xbox-";
		}
		else
		{
			text2 = "Xbox-";
			DebugLog.ControllerLogWarning("현재: " + text + ", 대응되는 컨트롤러가 없는 관계로, Xbox 레이아웃을 적용합니다");
		}
		return text2 + spriteName;
	}

	private static string ParseInputIcon_Internal(string input, bool isJoystick)
	{
		string text = ParseInputIcon(input, isJoystick);
		return "sprite name=\"" + text + "\" tint";
	}

	private static string ActionToIcon(string key, bool isJoystick)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			DebugLog.GeneralLogError("ActionToIcon: key가 비어 있습니다.");
			return string.Empty;
		}
		Player player = RewiredHelper.Player;
		if (player == null)
		{
			DebugLog.GeneralLogError($"ActionToIcon: Player가 null입니다. key = {key}, isJoystick = {isJoystick}");
			return key;
		}
		Controller controller = (isJoystick ? RewiredHelper.Gamepad : RewiredHelper.Keyboard);
		if (controller == null)
		{
			DebugLog.GeneralLogError($"ActionToIcon: Controller가 null입니다. key = {key}, isJoystick = {isJoystick}");
			return key;
		}
		List<ActionElementMap> allMaps = new List<ActionElementMap>();
		AddMapsSafe(controller, 0, 0);
		AddMapsSafe(controller, 1, 0);
		if (controller.type == ControllerType.Keyboard)
		{
			AddMapsSafe(RewiredHelper.Mouse, 0, 0);
			AddMapsSafe(RewiredHelper.Mouse, 1, 0);
		}
		ActionElementMap actionElementMap = allMaps.FirstOrDefault((ActionElementMap b) => b != null && !string.IsNullOrEmpty(b.actionDescriptiveName) && b.actionDescriptiveName.ToLower() == key);
		if (isJoystick)
		{
			if (actionElementMap == null)
			{
				return "none";
			}
			string text = ConvertGamepadTemplateInput(actionElementMap);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return "none";
		}
		if (actionElementMap != null)
		{
			return ActionToIcon_Keyboard(actionElementMap);
		}
		return CannotFindError(key);
		void AddMapsSafe(Controller targetController, int categoryId, int layoutId)
		{
			if (targetController != null)
			{
				ControllerMap map = player.controllers.maps.GetMap(targetController, categoryId, layoutId);
				if (map == null)
				{
					DebugLog.GeneralLogWarning($"ActionToIcon: ControllerMap이 없습니다. key = {key}, controller = {targetController.name}, category = {categoryId}, layout = {layoutId}");
				}
				else if (map.AllMaps != null)
				{
					allMaps.AddRange(map.AllMaps);
				}
			}
		}
	}

	private static string ActionToIcon_Keyboard(ActionElementMap foundMap)
	{
		string text = ((foundMap.controllerMap.controller.type == ControllerType.Keyboard) ? "keyboard" : "mouse");
		KeyboardKeyCode keyboardKeyCode = KeyboardKeyCode.None;
		string text3;
		if (text == "keyboard")
		{
			string text2;
			switch (foundMap.keyboardKeyCode)
			{
			case KeyboardKeyCode.RightShift:
			case KeyboardKeyCode.LeftShift:
				text2 = "shift";
				break;
			case KeyboardKeyCode.RightControl:
			case KeyboardKeyCode.LeftControl:
				text2 = "ctrl";
				break;
			case KeyboardKeyCode.RightAlt:
			case KeyboardKeyCode.LeftAlt:
				text2 = "alt";
				break;
			case KeyboardKeyCode.Keypad0:
				text2 = "numpad0";
				break;
			case KeyboardKeyCode.Keypad1:
				text2 = "numpad1";
				break;
			case KeyboardKeyCode.Keypad2:
				text2 = "numpad2";
				break;
			case KeyboardKeyCode.Keypad3:
				text2 = "numpad3";
				break;
			case KeyboardKeyCode.Keypad4:
				text2 = "numpad4";
				break;
			case KeyboardKeyCode.Keypad5:
				text2 = "numpad5";
				break;
			case KeyboardKeyCode.Keypad6:
				text2 = "numpad6";
				break;
			case KeyboardKeyCode.Keypad7:
				text2 = "numpad7";
				break;
			case KeyboardKeyCode.Keypad8:
				text2 = "numpad8";
				break;
			case KeyboardKeyCode.Keypad9:
				text2 = "numpad9";
				break;
			default:
				text2 = foundMap.keyboardKeyCode.ToString().ToLower();
				break;
			}
			text3 = text2;
		}
		else
		{
			text3 = foundMap.elementIdentifierName.Replace(" ", "") switch
			{
				"LeftMouseButton" => "left", 
				"RightMouseButton" => "right", 
				"MiddleMouseButton" => "wheel", 
				"MouseWheelUp" => "wheel_scroll_up", 
				"MouseWheelDown" => "wheel_scroll_down", 
				"MouseButton3" => "wheel", 
				"MouseButton4" => "side1", 
				"MouseButton5" => "side2", 
				_ => foundMap.elementIdentifierName, 
			};
		}
		string text4 = "";
		string text5 = foundMap.actionDescriptiveName.ToLower();
		if (text5.Contains("category"))
		{
			text4 = (text5.Contains("next") ? "_rightshoulder" : "_leftshoulder");
		}
		return text + "_" + text3 + text4;
	}

	private static string ParseError(string iconName)
	{
		DebugLog.GeneralLogError("아이콘이 제대로 파싱되지 않았습니다! (타입 = " + iconName + ")");
		return "";
	}

	private static string CannotFindError(string actionName)
	{
		DebugLog.GeneralLogError(actionName + "에 해당하는 조작을 찾을 수 없습니다!");
		return actionName;
	}

	private static string ConvertGamepadTemplateInput(ActionElementMap actionElementMap)
	{
		if (actionElementMap == null)
		{
			DebugLog.ControllerLogError("ConvertGamepadTemplateInput: actionElementMap이 null입니다.");
			return string.Empty;
		}
		Controller gamepad = RewiredHelper.Gamepad;
		if (gamepad == null)
		{
			DebugLog.ControllerLogError("ConvertGamepadTemplateInput: Gamepad가 null입니다. action = " + actionElementMap.actionDescriptiveName);
			return string.Empty;
		}
		IGamepadTemplate template = gamepad.GetTemplate<IGamepadTemplate>();
		if (template == null)
		{
			DebugLog.ControllerLogError("ConvertGamepadTemplateInput: GamepadTemplate이 null입니다. action = " + actionElementMap.actionDescriptiveName);
			return string.Empty;
		}
		string elementIdentifierName = actionElementMap.elementIdentifierName;
		int elementIdentifierId = actionElementMap.elementIdentifierId;
		if (template.leftStick.vertical.positiveDescriptiveName == elementIdentifierName)
		{
			return "Stick-L-Up";
		}
		if (template.leftStick.vertical.negativeDescriptiveName == elementIdentifierName)
		{
			return "Stick-L-Down";
		}
		if (template.leftStick.horizontal.positiveDescriptiveName == elementIdentifierName)
		{
			return "Stick-L-Right";
		}
		if (template.leftStick.horizontal.negativeDescriptiveName == elementIdentifierName)
		{
			return "Stick-L-Left";
		}
		if (template.rightStick.vertical.positiveDescriptiveName == elementIdentifierName)
		{
			return "Stick-R-Up";
		}
		if (template.rightStick.vertical.negativeDescriptiveName == elementIdentifierName)
		{
			return "Stick-R-Down";
		}
		if (template.rightStick.horizontal.positiveDescriptiveName == elementIdentifierName)
		{
			return "Stick-R-Right";
		}
		if (template.rightStick.horizontal.negativeDescriptiveName == elementIdentifierName)
		{
			return "Stick-R-Left";
		}
		if (template.leftStick.vertical.source.fullTarget.elementIdentifierId == elementIdentifierId)
		{
			return "Stick-L-UD";
		}
		if (template.leftStick.horizontal.source.fullTarget.elementIdentifierId == elementIdentifierId)
		{
			return "Stick-R-LR";
		}
		if (template.rightStick.vertical.source.fullTarget.elementIdentifierId == elementIdentifierId)
		{
			return "Stick-R-UD";
		}
		if (template.rightStick.horizontal.source.fullTarget.elementIdentifierId == elementIdentifierId)
		{
			return "Stick-R-LR";
		}
		if (template.leftStick.press.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Stick-L-Press";
		}
		if (template.rightStick.press.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Stick-R-Press";
		}
		if (template.dPad.up.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "DPad-Up";
		}
		if (template.dPad.left.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "DPad-Left";
		}
		if (template.dPad.down.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "DPad-Down";
		}
		if (template.dPad.right.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "DPad-Right";
		}
		if (template.actionBottomRow1.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Button-South";
		}
		if (template.actionBottomRow2.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Button-East";
		}
		if (template.actionTopRow1.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Button-West";
		}
		if (template.actionTopRow2.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Button-North";
		}
		if (template.leftShoulder1.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Shoulder-Left";
		}
		if (template.leftShoulder2.source.fullTarget.elementIdentifierId == elementIdentifierId)
		{
			return "Trigger-Left";
		}
		if (template.rightShoulder1.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Shoulder-Right";
		}
		if (template.rightShoulder2.source.fullTarget.elementIdentifierId == elementIdentifierId)
		{
			return "Trigger-Right";
		}
		if (template.center1.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Select";
		}
		if (template.center2.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Start";
		}
		if (template.center3.source.target.elementIdentifierId == elementIdentifierId)
		{
			return "Logo";
		}
		if (actionElementMap.elementIdentifierName == "Touchpad Button")
		{
			return "Touchpad";
		}
		DebugLog.ControllerLogError(actionElementMap.elementIdentifierName + "에 해당하는 입력이 설정되지 않았습니다!");
		return string.Empty;
	}

	private static string GetHoldPostfix(bool isJoystick)
	{
		if (!isJoystick)
		{
			return "_hold";
		}
		return "-Hold";
	}

	private static string ParseComboInput(string value, bool isKeyboard)
	{
		value.Split('<', '>');
		if (value.Contains("ashblast"))
		{
			if (!isKeyboard)
			{
				return "<icon=input_skillb>";
			}
			return "<icon=input_ctrl> + <icon=input_guard>";
		}
		if (value.Contains("ashbreak"))
		{
			if (!isKeyboard)
			{
				return "<icon=input_skilla>";
			}
			return "<icon=input_ctrl> + <icon=input_attack>";
		}
		if (value.Contains("ignition"))
		{
			if (!isKeyboard)
			{
				return "<icon=input_guard> + <icon=input_heat>";
			}
			return "<icon=input_heat> + <icon=input_attack>";
		}
		if (value.Contains("specialparry"))
		{
			if (!isKeyboard)
			{
				return "<icon=input_guard> + <icon=input_action>";
			}
			return "<icon=input_heat> + <icon=input_guard>";
		}
		return value;
	}
}
