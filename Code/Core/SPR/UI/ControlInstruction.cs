using Library;
using Rewired;
using SPR.Library;
using UnityEngine;

namespace SPR.UI;

public class ControlInstruction : MonoBehaviour, IControllerEventUpdate
{
	[SerializeField]
	private GameObject keyboardGroup;

	[SerializeField]
	private GameObject gamePadGroup;

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
		UpdateInstruction();
	}

	private void UpdateInstruction()
	{
		keyboardGroup.SetActive(!RewiredHelper.IsJoystickConnected);
		gamePadGroup.SetActive(RewiredHelper.IsJoystickConnected);
		keyboardGroup.GetComponentsInChildren<ControlGuideIcon>().ForEach(delegate(ControlGuideIcon i)
		{
			i.UpdateIcon();
		});
		gamePadGroup.GetComponentsInChildren<ControlGuideIcon>().ForEach(delegate(ControlGuideIcon i)
		{
			i.UpdateIcon();
		});
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		UpdateInstruction();
	}
}
