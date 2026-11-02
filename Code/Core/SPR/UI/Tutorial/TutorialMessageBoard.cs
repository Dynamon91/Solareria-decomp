using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Tutorial;

public class TutorialMessageBoard : MonoBehaviour
{
	[SerializeField]
	private GameObject messagePrefab;

	[SerializeField]
	private Transform messageParent;

	private const float Margin = 10f;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private Sequence showHideSequence;

	private List<TutorialMessageComponent> messages = new List<TutorialMessageComponent>();

	private bool shouldSort;

	private bool shouldCheckValid;

	private int MessageCount => messageParent.childCount;

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public void Initialize()
	{
		base.gameObject.SetActive(MessageCount > 0);
	}

	public void OnSceneLoaded()
	{
		if (Game.Instance.CurrentFieldID.SectorName == SectorName.None)
		{
			((IEnumerable<TutorialMessageComponent>)messages.ToList()).ForEach((Func<TutorialMessageComponent, UniTask>)RemoveMessageNow);
		}
		else
		{
			AddExistingMessages().Forget();
		}
	}

	public bool HasMessage(ActionName actionName)
	{
		return messages.Any((TutorialMessageComponent message) => message.ActionName == actionName);
	}

	private async UniTask AddExistingMessages()
	{
		WanderingData.GetProgressingTutorialMessages().Except(messages.Select((TutorialMessageComponent m) => m.ActionName)).ForEach(delegate(ActionName action)
		{
			AddMessage(action, ignoreTimeScale: false, activate: false);
		});
		messages.ForEach(delegate(TutorialMessageComponent message)
		{
			message.OnSceneLoaded();
		});
		foreach (TutorialMessageComponent item in messages.Where((TutorialMessageComponent message) => message.CheckSkip()).ToList())
		{
			await RemoveMessageNow(item);
		}
		Sort(isInstant: true);
	}

	public void AddMessage(ActionName actionName, bool ignoreTimeScale = false, bool activate = true)
	{
		if (activate)
		{
			base.gameObject.SetActive(value: true);
		}
		if (HasMessage(actionName))
		{
			DebugLog.GeneralLogWarning($"{actionName} 튜토리얼 메시지가 이미 존재하여 추가하지 않습니다!");
			return;
		}
		GameObject message = UnityEngine.Object.Instantiate(messagePrefab, messageParent);
		Vector3 localPosition = message.transform.localPosition;
		message.transform.localPosition = new Vector3(localPosition.x, CalculateLocalY(MessageCount - 1), localPosition.z);
		TutorialMessageComponent messageComponent = message.GetComponent<TutorialMessageComponent>();
		messages.Add(messageComponent);
		messageComponent.SetDataAsync(actionName, delegate
		{
			messages.Remove(messageComponent);
			shouldCheckValid = true;
		}, delegate
		{
			DestroyMessage(message);
		}, ignoreTimeScale);
	}

	private async UniTask RemoveMessageNow(TutorialMessageComponent message)
	{
		await message.DeactivateNow(isInstant: true);
	}

	private void DestroyMessage(GameObject message)
	{
		if (!message.IsNull())
		{
			message.transform.SetParent(null);
			message.transform.DOKill();
			UnityEngine.Object.Destroy(message);
			shouldSort = true;
		}
	}

	private float CalculateLocalY(int index)
	{
		float num = 0f;
		for (int i = 0; i < index; i++)
		{
			num += messageParent.GetChild(i).GetComponent<RectTransform>().rect.height + 10f;
		}
		return 0f - num;
	}

	private void MoveToTargetIndex(Transform message, int index, bool isInstant = false)
	{
		message.DOKill();
		message.DOLocalMoveY(CalculateLocalY(index), isInstant ? 0f : 0.6f, snapping: true);
	}

	public async UniTask ShowAsync(float duration = 0.6f)
	{
		duration = 0.6f;
		showHideSequence?.Kill();
		if (CanvasGroup.alpha.IsAlmostSame(1f) && base.gameObject.activeSelf)
		{
			return;
		}
		base.gameObject.SetActive(messageParent.childCount > 0);
		if (duration <= 0f)
		{
			Rect.localScale = Vector3.one;
			CanvasGroup.alpha = 1f;
			messages.ForEach(delegate(TutorialMessageComponent m)
			{
				m.ActivateAsync().Forget();
			});
		}
		else
		{
			CanvasGroup.alpha = 0f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(Rect.DOScale(Vector3.one, duration)).Join(CanvasGroup.DOFade(1f, duration));
			await showHideSequence.WaitAsync();
		}
	}

	public async UniTask HideAsync(float duration = 0.6f, bool deactivate = false)
	{
		showHideSequence?.Kill();
		if (!CanvasGroup.alpha.IsAlmostSame(0f))
		{
			if (duration <= 0f)
			{
				Rect.localScale = Vector3.one * 1.1f;
				CanvasGroup.alpha = 0f;
			}
			else
			{
				showHideSequence = DOTween.Sequence();
				showHideSequence.Append(Rect.DOScale(Vector3.one * 1.1f, duration)).Join(CanvasGroup.DOFade(0f, duration));
				await showHideSequence.WaitAsync();
			}
		}
		if (deactivate)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void FixedUpdate()
	{
		CheckValid();
		Sort();
	}

	private void CheckValid()
	{
		if (!shouldCheckValid)
		{
			return;
		}
		if (messages.Count == 0)
		{
			WaitForFadeOutAsync().GetAwaiter().OnCompleted(delegate
			{
				if (MessageCount == 0)
				{
					base.gameObject.SetActive(value: false);
				}
			});
		}
		shouldCheckValid = false;
	}

	private void Sort(bool isInstant = false)
	{
		if (!shouldSort)
		{
			return;
		}
		for (int i = 0; i < MessageCount; i++)
		{
			Transform child = messageParent.GetChild(i);
			if (!child.IsNull())
			{
				MoveToTargetIndex(child, i, isInstant);
			}
		}
		shouldSort = false;
	}

	private static async UniTask WaitForFadeOutAsync()
	{
		await UniTask.WaitForSeconds(1.2f);
		await UniTask.Yield();
	}
}
