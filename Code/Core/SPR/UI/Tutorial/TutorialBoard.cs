using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Tutorial;

public class TutorialBoard : MonoBehaviour
{
	[SerializeField]
	private List<GameObject> tutorialObjects;

	private CanvasGroup canvasGroup;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize()
	{
		base.gameObject.SetActive(value: false);
		CanvasGroup.alpha = 0f;
	}

	public void SetData(ActionName actionName)
	{
		foreach (GameObject tutorialObject in tutorialObjects)
		{
			tutorialObject.SetActive(value: false);
		}
		if (actionName != ActionName.None)
		{
			int index = (int)(actionName - 1);
			if (actionName == ActionName.FastDown)
			{
				index = 10;
			}
			tutorialObjects[index].SetActive(value: true);
			tutorialObjects[index].GetComponentsInChildren<LocaleText>().ForEach(delegate(LocaleText lt)
			{
				lt.SetData();
			});
		}
	}

	public async UniTask SetActiveAsync(bool value)
	{
		CanvasGroup.DOKill();
		if (value)
		{
			base.gameObject.SetActive(value: true);
			await CanvasGroup.DOFade(1f, 1f).WaitAsync();
		}
		else
		{
			await CanvasGroup.DOFade(0f, 1f).WaitAsync();
			base.gameObject.SetActive(value: false);
		}
	}
}
