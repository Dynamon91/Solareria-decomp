using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.UI.Trait;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace SPR.UI.Upgrade;

public class CardEngravingPanel : UpgradePanel
{
	public const float Duration = 1f / 6f;

	[Header("Card Engraving")]
	[SerializeField]
	private CardEngravingRadialBoard cardBoard;

	[SerializeField]
	private TraitStatusCardElement cutsceneCard;

	[Header("Timeline Director")]
	[SerializeField]
	private int timelineSkipFrame;

	[SerializeField]
	private int timelineEndFrame;

	[Header("Key Guides")]
	[SerializeField]
	private List<GameObject> upgradeControlGuides;

	private CanvasGroup canvasGroup;

	private bool isInitialized;

	private Sequence fadeSeq;

	protected override bool UsingOhn => false;

	private CardEngravingBoard EngravingBoard => upgradeBoard as CardEngravingBoard;

	protected override string ConfirmType => "Engraving";

	public void Initialize()
	{
		if (!isInitialized)
		{
			if ((object)canvasGroup == null)
			{
				canvasGroup = GetComponent<CanvasGroup>();
			}
			cardBoard.Initialize();
			EngravingBoard.InitializeAsync().Forget();
			isInitialized = true;
		}
	}

	public override bool HasStock()
	{
		return EngravingBoard.HasStock();
	}

	private void SetData()
	{
		CardEngravingModel data = new CardEngravingModel(cardBoard.CurrentID);
		cardBoard.Current.RefreshData();
		EngravingBoard.SetData(data);
		UpdateControlGuide();
	}

	public override string ControllerName()
	{
		return "Engraving Panel";
	}

	protected override async UniTask OnActivateAsync()
	{
		cardBoard.OnActivate(focusCurrent: true);
		base.gameObject.SetActive(value: true);
		SetData();
		base.gameObject.SetActive(value: false);
		await UniTask.CompletedTask;
	}

	protected override void OnControlInput(int horizontal, int vertical)
	{
		if (!base.IsDirecting)
		{
			if (horizontal != 0)
			{
				cardBoard.RotateToActive(horizontal).Forget();
			}
			SetData();
		}
	}

	protected override void OnDirectingInput()
	{
		float num = (float)timelineSkipFrame / 60f;
		if (base.Director.time <= (double)num)
		{
			base.Director.time = num;
			return;
		}
		float num2 = (float)timelineEndFrame / 60f;
		if (base.Director.time <= (double)num2)
		{
			base.Director.time = num2;
		}
	}

	protected override void OnUpgradeSuccess()
	{
		TraitStatusCardElement traitStatusCardElement = cardBoard.Current.CastObject<TraitStatusCardElement>();
		string type = ((traitStatusCardElement.ID == 4 && traitStatusCardElement.Rank > 1) ? ("Heat" + ConfirmType) : ConfirmType);
		ServicePanel<Unit>.ConfirmPopup.SetModel(new FormatConfirmModel(type, OnConfirmInput, null));
		ServicePanel<Unit>.InputManager.StackControlTarget(ServicePanel<Unit>.ConfirmPopup).Forget();
	}

	protected override void OnConfirmInput()
	{
		cutsceneCard.SetData(cardBoard.CurrentID);
		int rank = cardBoard.Current.CastObject<TraitStatusCardElement>().Rank;
		successTimeline.GetTrack<AnimationTrack>("Animation Track Rank 1").muted = rank != 1;
		successTimeline.GetTrack<AnimationTrack>("Animation Track Rank 2").muted = rank != 2;
		PlaySuccessTimelineAsync().GetAwaiter().OnCompleted(base.CheckSoldOutScenario);
		upgradeControlGuides.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: false);
		});
	}

	protected override async UniTask OnDeactivateAsync()
	{
		fadeSeq?.Kill();
		fadeSeq = DOTween.Sequence().Join(canvasGroup.DOAlpha(0f, 1f / 6f).SetEase(Ease.Linear));
		if (HasStock())
		{
			ServicePanel<Unit>.FieldCanvas.ShowAllAsync(instant: false).Forget();
		}
		await fadeSeq.WaitAsync();
	}

	protected override void UpdateControlGuide()
	{
		UpgradeCheckResult upgradeCheckResult = upgradeBoard.CanProceedUpgrade();
		upgradeControlGuides[0].SetActive(value: true);
		upgradeControlGuides[1].SetActive(upgradeCheckResult != UpgradeCheckResult.MaximumUpgrade);
		LayoutRebuilder.ForceRebuildLayoutImmediate(upgradeControlGuides[0].transform.parent.GetComponent<RectTransform>());
	}

	public override void UpdateData()
	{
		int iD = cardBoard.Current.ID;
		EngravingBoard.SetData(new CardEngravingModel(iD));
		UpdateControlGuide();
	}

	public void ApplyEngraving()
	{
		upgradeBoard.OnProceedUpgrade();
		SetData();
	}

	public void TryShowSpecialParryTutorial()
	{
		int iD = cardBoard.Current.ID;
		if (iD == 4 && ServicePanel<Unit>.PlayerData.TryGetCardData(iD, out var cardData) && cardData.Rank == 2)
		{
			base.OnDeactiveteEvent -= ShowSpecialParryTutorial;
			base.OnDeactiveteEvent += ShowSpecialParryTutorial;
		}
	}

	private void ShowSpecialParryTutorial()
	{
		TutorialCanvas tutorialCanvas = Game.Instance.UIManager.TutorialCanvas;
		tutorialCanvas.SetData(ActionName.SpecialParry);
		ServicePanel<Unit>.InputManager.StackControlTarget(tutorialCanvas).Forget();
	}
}
