using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.L10N;
using UnityEngine;

namespace SPR.UI.Store;

public class StatusChangeBoard : MonoBehaviour, IControllable
{
	[SerializeField]
	private LocaleText foodNameText;

	[SerializeField]
	private ChangingStatusListBoard statusListBoard;

	private static InputManager InputManager => Game.Instance.InputManager;

	public bool IsValid => base.gameObject.activeInHierarchy;

	public void SetData(string itemID)
	{
		List<ChangingStatusInfoBoard.StatusChangeInfo> data = Game.DataManager.GetFoodData(itemID).StatTypes.Select((StatType t, int i) => default(ChangingStatusInfoBoard.StatusChangeInfo)).ToList();
		foodNameText.SetData(LocaleKey.ItemName(itemID));
		statusListBoard.SetData(data);
	}

	public string ControllerName()
	{
		return "Status Change Board";
	}

	public UniTask Activate()
	{
		return UniTask.CompletedTask;
	}

	public void Control()
	{
		if (InputManager.confirmDown)
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		base.gameObject.SetActive(value: false);
		return UniTask.CompletedTask;
	}
}
