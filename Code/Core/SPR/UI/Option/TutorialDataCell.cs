using SPR.L10N;
using SPR.ObjectPool;
using SPR.Save;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.UI.Option;

public class TutorialDataCell : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private LocaleText localeText;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private GameObject notiIcon;

	private RectTransform rectTransform;

	public bool IsActive { get; private set; }

	public ActionName ActionName { get; private set; }

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public string PrefabKey => "Tutorial Data Cell.prefab";

	public GameObject GameObject => base.gameObject;

	public void SetData(ActionName actionName)
	{
		ActionName = actionName;
		IsActive = !WanderingData.ShouldShowTutorial(actionName);
		string data = (IsActive ? LocaleKey.TutorialName(actionName) : "Name_Unknown");
		localeText.SetData(data);
		notiIcon.SetActive(WanderingData.IsUnwatchedTutorialInOption(actionName));
	}

	public void Focus(bool value)
	{
		canvasGroup.alpha = (value ? 1f : 0f);
		if (value)
		{
			notiIcon.SetActive(value: false);
			WanderingData.RemoveUnwatchedTutorialInOption(ActionName);
		}
	}

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
