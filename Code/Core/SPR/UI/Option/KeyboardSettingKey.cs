using System.Collections;
using DG.Tweening;
using Rewired;
using SPR.L10N;
using SPR.Library;
using TMPro;
using UnityEngine;

namespace SPR.UI.Option;

public class KeyboardSettingKey : MonoBehaviour
{
	public enum ControlCommands
	{
		Up,
		Down,
		Left,
		Right,
		Jump,
		Attack,
		Guard,
		Dash,
		Interact,
		CamUp,
		CamDown,
		CamLeft,
		CamRight,
		Item,
		Skill1,
		Skill2,
		Pendant,
		Menu,
		WeaponSwap,
		Minimap,
		Map,
		BigCategoryLeftUI,
		BigCategoryRightUI,
		SmallCategoryLeftUI,
		SmallCategoryRightUI,
		SliderUI,
		UpUI,
		DownUI,
		LeftUI,
		RightUI,
		ConfirmUI,
		CancelUI,
		InteractUI
	}

	[SerializeField]
	private TextMeshProUGUI keyNameText;

	[SerializeField]
	private ControlCommands commandType;

	[SerializeField]
	private AxisRange axisRange;

	[SerializeField]
	private TextMeshProUGUI keyCodeText;

	[SerializeField]
	private TextMeshProUGUI pressNewKeyText;

	private bool _listening;

	private ActionElementMap _actionElementMap;

	private Sequence _conflictFeedbackSeq;

	private OptionKeyboardPanel optionKeyboardPanel;

	private OptionKeyboardPanel KeyboardPanel => optionKeyboardPanel ?? (optionKeyboardPanel = GetComponentInParent<OptionKeyboardPanel>());

	private InputMapper InputMapper => KeyboardPanel.InputMapper;

	private ControllerMap Controller
	{
		get
		{
			if (!IsUIInputKey(commandType))
			{
				return RewiredHelper.DefaultControllerMap;
			}
			return RewiredHelper.UIControllerMap;
		}
	}

	public SettingItemActiveState Active { get; protected set; }

	public int ActionId => _actionElementMap.actionId;

	public string ElementIdentifier => _actionElementMap.elementIdentifierName;

	public ControlCommands CommandType => commandType;

	public void RefreshLanguage()
	{
		keyNameText.text = commandType.GetLocaleKeyboardKeyName();
	}

	public void Control()
	{
	}

	public void Synchronize()
	{
		Active = SettingItemActiveState.Enable;
		if (RewiredHelper.Controller == null)
		{
			pressNewKeyText.text = "";
			return;
		}
		_actionElementMap = commandType.ToActionElement();
		string keyCodeString = commandType.GetKeyCodeString();
		keyNameText.text = commandType.GetLocaleKeyboardKeyName();
		keyCodeText.text = keyCodeString;
		keyCodeText.alpha = 1f;
		pressNewKeyText.alpha = 0f;
	}

	public void Save()
	{
	}

	public void OnConflict()
	{
		_conflictFeedbackSeq.Kill();
		_conflictFeedbackSeq.Append(keyCodeText.rectTransform.DOShakeRotation(1f, 30f));
		_conflictFeedbackSeq.Append(keyCodeText.rectTransform.DORotate(Vector3.zero, 0.2f));
	}

	public void OnMapNewKey()
	{
		if (RewiredHelper.Controller != null)
		{
			StartCoroutine(StartListeningDelayed());
		}
	}

	private IEnumerator StartListeningDelayed()
	{
		yield return new WaitForSeconds(0.1f);
		InputMapper.Start(new InputMapper.Context
		{
			actionId = ActionId,
			controllerMap = Controller,
			actionRange = axisRange,
			actionElementMapToReplace = _actionElementMap
		});
		RewiredHelper.EnableUIInput(enable: false);
		keyCodeText.DOFade(0f, 0.3f);
		pressNewKeyText.text = Locale.KeyboardKeyListening;
		pressNewKeyText.DOFade(1f, 0.3f);
	}

	private static bool IsUIInputKey(ControlCommands command)
	{
		return command.ToString().Contains("UI");
	}

	private void OnDrawGizmosSelected()
	{
		base.name = $"Keyboard Setting Key - {commandType}";
	}
}
