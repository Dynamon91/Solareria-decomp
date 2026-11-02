using System.Linq;
using Rewired;
using SPR.Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class ControlGuideIcon : MonoBehaviour, IControllerEventUpdate
{
	private enum Category
	{
		Default,
		UI
	}

	[SerializeField]
	private bool useIcon;

	[SerializeField]
	private Category category = Category.UI;

	[SerializeField]
	private ControlAction inputAction;

	[Header("Child")]
	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private TextMeshProUGUI keyText;

	[Header("Assets")]
	[SerializeField]
	private Sprite defaultImage;

	[SerializeField]
	private Sprite stick;

	[SerializeField]
	private Sprite stickZoom;

	[SerializeField]
	private Sprite dPadUp;

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	public void UpdateIcon()
	{
		Controller controller = RewiredHelper.Controller;
		ControllerMap map = RewiredHelper.Player.controllers.maps.GetMap(controller, (int)category, 0);
		if (useIcon)
		{
			keyText.text = ((inputAction == ControlAction.MoveUI) ? "L" : string.Empty);
			SetInputIcon();
			return;
		}
		string text = map.AllMaps.FirstOrDefault((ActionElementMap b) => b.actionDescriptiveName == inputAction.ToString())?.elementIdentifierName;
		keyText.text = InputToAbbreviation(text);
		if (text == "Left Stick Up")
		{
			iconImage.sprite = stickZoom;
			keyText.rectTransform.anchoredPosition = new Vector2(0f, 20f);
		}
		else
		{
			keyText.rectTransform.anchoredPosition = Vector2.zero;
		}
	}

	private void SetInputIcon()
	{
		if (RewiredHelper.Controller.type == ControllerType.Joystick)
		{
			Image image = iconImage;
			image.sprite = inputAction switch
			{
				ControlAction.MoveUI => stick, 
				ControlAction.ZoomUI => stickZoom, 
				ControlAction.Item => dPadUp, 
				_ => iconImage.sprite, 
			};
		}
		else
		{
			Image image = iconImage;
			image.sprite = inputAction switch
			{
				ControlAction.MoveUI => null, 
				ControlAction.ZoomUI => null, 
				ControlAction.Item => ItemKey(), 
				_ => iconImage.sprite, 
			};
		}
	}

	public static string InputToAbbreviation(string value)
	{
		return value switch
		{
			"Left Shoulder" => "LB", 
			"Right Shoulder" => "RB", 
			"Left Trigger" => "LT", 
			"Right Trigger" => "RT", 
			"Up Arrow" => "↑", 
			"Down Arrow" => "↓", 
			"Left Stick Button" => "LStick", 
			"Right Stick Button" => "RStick", 
			"Left Stick Up" => "L", 
			_ => value, 
		};
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		UpdateIcon();
	}

	private Sprite ItemKey()
	{
		Controller controller = RewiredHelper.Controller;
		string value = RewiredHelper.Player.controllers.maps.GetMap(controller, (int)category, 0).ButtonMaps.FirstOrDefault((ActionElementMap b) => b.actionDescriptiveName == inputAction.ToString())?.elementIdentifierName;
		keyText.text = InputToAbbreviation(value);
		return defaultImage;
	}
}
