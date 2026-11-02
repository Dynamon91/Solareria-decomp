using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Rewired;
using SPR.L10N;
using SPR.Library;
using UnityEngine;

namespace SPR.UI.Option;

public class GamepadInputSettingItem : InputSettingItem
{
	public enum InputAction
	{
		None = 0,
		Jump = 1,
		Left = 2,
		Right = 3,
		Down = 4,
		Up = 5,
		Menu = 6,
		Map = 7,
		Pause = 8,
		Attack = 100,
		Dash = 101,
		Guard = 102,
		Action = 103,
		Heat = 104,
		Heal = 105,
		SkillA = 106,
		SkillB = 107,
		SpecialParry = 108,
		Ignition = 109,
		LeftUI = 200,
		RightUI = 201,
		UpUI = 202,
		DownUI = 203,
		[InspectorName("Category 1 Left")]
		PreviousCategoryUI = 204,
		[InspectorName("Category 1 Right")]
		NextCategoryUI = 205,
		[InspectorName("Category 2 Left")]
		SubPreviousCategory = 206,
		[InspectorName("Category 2 Right")]
		SubNextCategory = 207,
		ConfirmUI = 208,
		Interact = 209,
		Detail = 210,
		Reset = 211,
		Mode = 212,
		ScrollUp = 213,
		ScrollDown = 214,
		Cancel = 215
	}

	[Header("Gamepad Action")]
	[SerializeField]
	protected InputAction inputAction;

	public InputAction CurrentInputAction => inputAction;

	public override int InputIndex
	{
		get
		{
			switch ((int)inputAction)
			{
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 32:
			case 33:
			case 34:
			case 35:
			case 36:
			case 37:
			case 38:
			case 39:
			case 40:
			case 41:
			case 42:
			case 43:
			case 44:
			case 45:
			case 46:
			case 47:
			case 48:
			case 49:
			case 50:
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
			case 56:
			case 57:
			case 58:
			case 59:
			case 60:
			case 61:
			case 62:
			case 63:
			case 64:
			case 65:
			case 66:
			case 67:
			case 68:
			case 69:
			case 70:
			case 71:
			case 72:
			case 73:
			case 74:
			case 75:
			case 76:
			case 77:
			case 78:
			case 79:
			case 80:
			case 81:
			case 82:
			case 83:
			case 84:
			case 85:
			case 86:
			case 87:
			case 88:
			case 89:
			case 90:
			case 91:
			case 92:
			case 93:
			case 94:
			case 95:
			case 96:
			case 97:
			case 98:
			case 99:
				return 1;
			case 100:
			case 101:
			case 102:
			case 103:
			case 104:
			case 105:
			case 106:
			case 107:
			case 108:
			case 109:
			case 110:
			case 111:
			case 112:
			case 113:
			case 114:
			case 115:
			case 116:
			case 117:
			case 118:
			case 119:
			case 120:
			case 121:
			case 122:
			case 123:
			case 124:
			case 125:
			case 126:
			case 127:
			case 128:
			case 129:
			case 130:
			case 131:
			case 132:
			case 133:
			case 134:
			case 135:
			case 136:
			case 137:
			case 138:
			case 139:
			case 140:
			case 141:
			case 142:
			case 143:
			case 144:
			case 145:
			case 146:
			case 147:
			case 148:
			case 149:
			case 150:
			case 151:
			case 152:
			case 153:
			case 154:
			case 155:
			case 156:
			case 157:
			case 158:
			case 159:
			case 160:
			case 161:
			case 162:
			case 163:
			case 164:
			case 165:
			case 166:
			case 167:
			case 168:
			case 169:
			case 170:
			case 171:
			case 172:
			case 173:
			case 174:
			case 175:
			case 176:
			case 177:
			case 178:
			case 179:
			case 180:
			case 181:
			case 182:
			case 183:
			case 184:
			case 185:
			case 186:
			case 187:
			case 188:
			case 189:
			case 190:
			case 191:
			case 192:
			case 193:
			case 194:
			case 195:
			case 196:
			case 197:
			case 198:
			case 199:
				return 2;
			default:
				return 3;
			}
		}
	}

	public override void Control(int horizontal, int vertical)
	{
	}

	public override void Save()
	{
	}

	public override bool CanRemapping()
	{
		return inputAction != InputAction.None;
	}

	protected override void ConvertActionToIcon()
	{
		if (CanRemapping())
		{
			Locale.TryParseIcon("<icon=input_" + inputAction.ToString().ToLower() + ">", isJoystick: true, out var output);
			inputText.text = output;
		}
	}

	public override async UniTask StartMapping()
	{
		selectHighlight.SetActive(value: true);
		MappingInProgress = true;
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
		inputText.text = "";
		await UniTask.WaitForSeconds(0.1f, ignoreTimeScale: true);
		InputManager.InputMapperGamepad.InputMappedEvent += OnInputMapped;
		ControllerMap controllerMap = RewiredHelper.GetControllerMap(RewiredHelper.Gamepad, (inputAction >= InputAction.LeftUI) ? 1 : 0);
		ActionElementMap actionElementMap = null;
		int num = -1;
		foreach (ActionElementMap allMap in controllerMap.AllMaps)
		{
			if (allMap.actionDescriptiveName == inputAction.ToString())
			{
				actionElementMap = allMap;
				num = actionElementMap.actionId;
				break;
			}
		}
		if (actionElementMap == null)
		{
			Rewired.InputAction action = ReInput.mapping.GetAction(inputAction.ToString());
			if (action == null)
			{
				return;
			}
			num = action.id;
			ElementAssignment elementAssignment = new ElementAssignment(controllerMap.elementMapCount + 1, num, Pole.Positive);
			if (!controllerMap.CreateElementMap(elementAssignment))
			{
				return;
			}
			actionElementMap = controllerMap.GetFirstElementMapWithAction(num);
		}
		PreviousAssignment = new ElementAssignment(actionElementMap.controllerMap.controllerType, actionElementMap.elementType, actionElementMap.elementIdentifierId, actionElementMap.axisRange, actionElementMap.keyCode, actionElementMap.modifierKeyFlags, actionElementMap.actionId, actionElementMap.axisContribution, actionElementMap.invert, actionElementMap.id);
		AxisRange axisRange;
		switch (inputAction)
		{
		case InputAction.Left:
		case InputAction.Down:
		case InputAction.LeftUI:
		case InputAction.DownUI:
		case InputAction.ScrollDown:
			axisRange = AxisRange.Negative;
			break;
		case InputAction.Right:
		case InputAction.RightUI:
		case InputAction.UpUI:
		case InputAction.ScrollUp:
			axisRange = AxisRange.Positive;
			break;
		default:
			axisRange = AxisRange.Positive;
			break;
		}
		AxisRange actionRange = axisRange;
		InputManager.InputMapperGamepad.Start(new InputMapper.Context
		{
			actionId = num,
			controllerMap = controllerMap,
			actionRange = actionRange,
			actionElementMapToReplace = controllerMap.GetElementMap(actionElementMap.id)
		});
		await UniTask.WaitWhile(() => MappingInProgress);
		InputManager.InputMapperGamepad.InputMappedEvent -= OnInputMapped;
		await UniTask.WaitForSeconds(0.125f, ignoreTimeScale: true);
	}

	protected override void DeleteKey()
	{
		ControllerMap controllerMap = RewiredHelper.GetControllerMap(RewiredHelper.Gamepad, (inputAction >= InputAction.LeftUI) ? 1 : 0);
		ActionElementMap actionElementMap = controllerMap.AllMaps.ToList().Find((ActionElementMap m) => m.actionDescriptiveName == inputAction.ToString());
		if (actionElementMap != null)
		{
			if (controllerMap.ContainsElementMap(actionElementMap.id))
			{
				controllerMap.DeleteElementMap(actionElementMap.id);
			}
			inputText.text = "";
		}
	}

	protected override List<string> GetFixedIdentifiers()
	{
		return new List<string> { "left stick x", "left stick y", "right stick x", "right stick y", "view", "menu", "create", "options" };
	}

	protected override List<string> GetRestrictedIdentifiers()
	{
		return new List<string>();
	}

	private void OnInputMapped(InputMapper.InputMappedEventData eventData)
	{
		InputManager.InputMapperGamepad.Stop();
		ActionElementMap newMap = eventData.actionElementMap;
		ControllerMap controllerMap = RewiredHelper.GetControllerMap(RewiredHelper.Gamepad, (inputAction >= InputAction.LeftUI) ? 1 : 0);
		ActionElementMap actionElementMap = controllerMap.AllMaps.FirstOrDefault((ActionElementMap m) => m.CheckForAssignmentConflict(newMap));
		string inputName;
		if (HasFixedIdentifier(newMap))
		{
			ResetKey(out inputName);
		}
		else if (IsRestrictedIdentifier(newMap))
		{
			ResetKey(out inputName);
		}
		else if (actionElementMap != null)
		{
			actionElementMap.elementIdentifierId = -1;
			inputName = "<icon=input_" + newMap.actionDescriptiveName.ToLower() + ">";
		}
		else
		{
			inputName = "<icon=input_" + newMap.actionDescriptiveName.ToLower() + ">";
		}
		Locale.TryParseIcon(inputName, isJoystick: true, out var output);
		inputText.text = output;
		selectHighlight.SetActive(value: false);
		ReInput.userDataStore.Save();
		RewiredHelper.UpdateInputIcons();
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Map/SFX_UI_Map_Marker_Place");
		MappingInProgress = false;
		void ResetKey(out string reference)
		{
			controllerMap.ReplaceElementMap(PreviousAssignment);
			ActionElementMap firstElementMapMatch = controllerMap.GetFirstElementMapMatch((ActionElementMap m) => m.actionDescriptiveName == inputAction.ToString());
			reference = "<icon=input_" + firstElementMapMatch.actionDescriptiveName.ToLower() + ">";
			ShowWarning(hasNoAction: false);
		}
	}
}
