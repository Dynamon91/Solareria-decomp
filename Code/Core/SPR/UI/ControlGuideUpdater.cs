using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Rewired;
using SPR.Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class ControlGuideUpdater : MonoBehaviour, IControllerEventUpdate
{
	[SerializeField]
	private List<RectTransform> keyboardGuides;

	[SerializeField]
	private List<RectTransform> gamepadGuides;

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	private void OnDestroy()
	{
		RewiredHelper.RemoveControllerChangeEvent(this);
	}

	private void OnEnable()
	{
		UpdateControlGuides();
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		UpdateControlGuides();
	}

	public void UpdateControlGuides()
	{
		SetActiveGuides(keyboardGuides, !RewiredHelper.IsJoystickConnected);
		SetActiveGuides(gamepadGuides, RewiredHelper.IsJoystickConnected);
	}

	private static void SetActiveGuides(List<RectTransform> guides, bool isActive)
	{
		guides.ForEach(delegate(RectTransform rt)
		{
			rt.gameObject.SetActive(isActive);
			if (isActive)
			{
				LocaleText[] componentsInChildren = rt.GetComponentsInChildren<LocaleText>(includeInactive: false);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].SetData();
				}
				ControlGuideIcon[] componentsInChildren2 = rt.GetComponentsInChildren<ControlGuideIcon>(includeInactive: false);
				for (int i = 0; i < componentsInChildren2.Length; i++)
				{
					componentsInChildren2[i].UpdateIcon();
				}
				UniTask.Yield().GetAwaiter().OnCompleted(delegate
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
				});
			}
		});
	}
}
