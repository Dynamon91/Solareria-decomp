using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using SPR.UI;
using TMPro;
using UnityEngine;

public class InputIconText : MonoBehaviour, IControllerEventUpdate
{
	[SerializeField]
	private string iconKey;

	private TextMeshProUGUI tmp;

	private TextMeshProUGUI Tmp => tmp ?? (tmp = GetComponent<TextMeshProUGUI>());

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	private void OnEnable()
	{
		if (Game.IsInitialized)
		{
			UpdateText();
		}
	}

	public void OnControllerChanged()
	{
		UpdateText();
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		UpdateText();
	}

	private void UpdateText()
	{
		if (!iconKey.IsNullOrEmpty())
		{
			Locale.TryParseIcon("<icon=input_" + iconKey + ">", out var output);
			Tmp.text = output;
		}
	}
}
