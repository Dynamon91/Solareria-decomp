using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Choice;

public class ChoiceBoard : MonoBehaviour, IControllable
{
	[SerializeField]
	private float inputDelayDuration;

	[Header("References")]
	[SerializeField]
	private RectTransform backgroundRect;

	[SerializeField]
	private List<ChoiceOption> choiceOptions;

	[SerializeField]
	private OptionSelectMarker selectMarker;

	[SerializeField]
	private MarkerIcon markerIcon;

	[SerializeField]
	private GameObject controlGuide;

	private CanvasGroup canvasGroup;

	private List<ChoiceData> choices;

	private int targetIndex;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => true;

	public void Initialize()
	{
		choiceOptions.ForEach(delegate(ChoiceOption option)
		{
			option.Initialize();
		});
		choices = new List<ChoiceData>();
		targetIndex = 0;
		selectMarker.SetActive(value: false, instant: true);
		CanvasGroup.alpha = 0f;
		controlGuide.SetActive(value: false);
	}

	private async UniTask SelectChoiceAsync()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		UniTask task = markerIcon.ShakeVerticalAsync();
		await InputManager.BlockControlForTask(task, "Select Marker Icon Shake");
		choices[targetIndex].OnSelectChoice();
	}

	private int GetNextTargetIndex(int vertical)
	{
		int num = targetIndex - vertical;
		if (num < 0)
		{
			num = 0;
		}
		else if (num >= choices.Count)
		{
			num = choices.Count - 1;
		}
		return num;
	}

	private void ChangeTargetTo(int index, bool immediately = false)
	{
		if (targetIndex != index || immediately)
		{
			targetIndex = index;
			for (int i = 0; i < choiceOptions.Count; i++)
			{
				choiceOptions[i].Focus(i == targetIndex);
			}
			selectMarker.MoveToAsync(choiceOptions[targetIndex].Rect, immediately).Forget();
			if (!immediately)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
			}
		}
	}

	public void AddChoice(ChoiceData choiceData)
	{
		choices.Add(choiceData);
	}

	public async UniTask PlayShakeTweenAsync(int optionIndex = -1)
	{
		optionIndex = ((optionIndex < 0) ? targetIndex : optionIndex);
		await choiceOptions[optionIndex].Rect.DOShakeAnchorPos(0.75f, 10f * Vector2.right, 10, 0f).WaitAsync();
	}

	public string ControllerName()
	{
		return "Choice Board";
	}

	public async UniTask Activate()
	{
		CanvasGroup.alpha = 1f;
		for (int i = 0; i < choiceOptions.Count; i++)
		{
			if (i < choices.Count)
			{
				choiceOptions[i].SetData(choices[i].ChoiceText);
				choiceOptions[i].Show(choices[i].ShowNotiIcon);
			}
			else
			{
				choiceOptions[i].Hide();
			}
		}
		backgroundRect.sizeDelta = new Vector2(backgroundRect.sizeDelta.x, 50f * (float)choices.Count + 30f);
		selectMarker.SetActive(value: false);
		await UniTask.WaitForSeconds(inputDelayDuration);
		ChangeTargetTo(0, immediately: true);
		selectMarker.SetActive(value: true);
		controlGuide.SetActive(value: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
	}

	public void Control()
	{
		bool confirmDown = InputManager.confirmDown;
		int verticalDownUI = InputManager.verticalDownUI;
		bool flag = InputManager.menuDown || InputManager.cancelDown;
		if (confirmDown)
		{
			SelectChoiceAsync().Forget();
		}
		if (verticalDownUI != 0)
		{
			int nextTargetIndex = GetNextTargetIndex(verticalDownUI);
			ChangeTargetTo(nextTargetIndex);
		}
		if (flag)
		{
			int num = choices.FindIndex((ChoiceData c) => c.IsDefaultChoice);
			if (num >= 0)
			{
				ChangeTargetTo(num);
				SelectChoiceAsync().Forget();
			}
		}
	}

	public UniTask Pause()
	{
		return default(UniTask);
	}

	public UniTask Restart()
	{
		return default(UniTask);
	}

	public UniTask Deactivate()
	{
		choices.Clear();
		CanvasGroup.alpha = 0f;
		ChangeTargetTo(0, immediately: true);
		controlGuide.SetActive(value: false);
		return default(UniTask);
	}
}
