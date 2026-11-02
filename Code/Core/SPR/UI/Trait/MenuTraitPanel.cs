using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.L10N;
using SPR.Save;
using SPR.UI.Menu;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.UI.Trait;

public class MenuTraitPanel : MenuPanel
{
	public const float ActiveAlpha = 1f;

	public const float InactiveAlpha = 0.1f;

	public const float NonRestScale = 0.7f;

	public const float Duration = 1f / 6f;

	public const float MessageDuration = 1f;

	public static Color DefaultColor = Color.white;

	public static Color IncreaseColor = new Color(0f, 0.90738f, 1f);

	public static Color DecreaseColor = new Color(0.83137f, 0f, 0f);

	[SerializeField]
	private RectTransform boardMask;

	[SerializeField]
	private RectTransform boardParent;

	[SerializeField]
	private RectTransform vfxParent;

	[Header("References")]
	[SerializeField]
	private TraitStatusCardBoard statusBoard;

	[SerializeField]
	private TraitSkillCardBoard skillBoard;

	[SerializeField]
	private TraitStatusDetailBoard detailStatus;

	[SerializeField]
	private TraitStatusInfoBoard statusInfo;

	[SerializeField]
	private TraitSkillInfoBoard skillInfo;

	[SerializeField]
	private TraitStatusDetailInfoBoard detailInfo;

	[SerializeField]
	private TraitPointBoard pointBoard;

	private bool isSkill;

	private bool isDetail;

	private Vector2 initialPos = Vector2.positiveInfinity;

	public override MenuCategory Category => MenuCategory.Trait;

	protected override bool CanDeactivate
	{
		get
		{
			if ((!MenuPanel.IsRest || statusBoard.HasNoDiff) && !isDetail)
			{
				return !isSkill;
			}
			return false;
		}
	}

	protected override bool CanChangeCategory => !MenuPanel.IsRest;

	private TraitCardBoard CurrentBoard
	{
		get
		{
			if (!isSkill)
			{
				return statusBoard;
			}
			return skillBoard;
		}
	}

	private TraitInfoBoard CurrentInfo
	{
		get
		{
			if (!isSkill)
			{
				return statusInfo;
			}
			return skillInfo;
		}
	}

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	private static ControlGuideGroup GuideGroup => Game.Instance.UIManager.MenuCanvas.GuideGroup;

	public static Ease GetEase(bool active)
	{
		if (!active)
		{
			return Ease.InQuad;
		}
		return Ease.OutQuad;
	}

	public override void Initialize()
	{
		AddressableHelper.PreloadAssetsAsync<Sprite>("CoreStone").Forget();
		statusBoard.Initialize();
		skillBoard.Initialize();
		detailStatus.Initialize();
		statusInfo.Initialize();
		skillInfo.Initialize();
		if (float.IsInfinity(initialPos.magnitude))
		{
			initialPos = boardParent.anchoredPosition;
		}
	}

	protected override void OnActivate()
	{
		boardMask.anchoredPosition = new Vector2(0f, MenuPanel.IsRest ? 140 : 0);
		boardParent.anchoredPosition = initialPos + (MenuPanel.IsRest ? new Vector2(0f, -70f) : Vector2.zero);
		boardParent.localScale = (MenuPanel.IsRest ? Vector3.one : new Vector3(0.7f, 0.7f, 1f));
		statusBoard.OnActivate(focusCurrent: true);
		skillBoard.OnActivate(focusCurrent: false);
		skillBoard.ActivateCards(!MenuPanel.IsRest);
		detailStatus.OnActivate();
		isSkill = false;
		isDetail = false;
		statusInfo.SetActive(value: true, instant: true);
		skillInfo.SetActive(value: false, instant: true);
		detailInfo.SetActive(value: false, instant: true);
		detailStatus.SetActive(value: true, instant: true);
		pointBoard.SetData();
		pointBoard.SetActive(value: true, instant: true);
		detailStatus.SetCursorActive(value: false, instant: true);
		detailStatus.ShowTargetStats(CurrentBoard.CurrentID, instant: true);
		ShowInfo();
		WanderingData.RemoveUnwatchedCoreStone(CurrentBoard.CurrentID);
		CurrentBoard.Current.OnFocus();
	}

	protected override void OnControl()
	{
		if (WanderingData.ShouldShowTutorial(ActionName.Level))
		{
			TutorialCanvas.SetData(ActionName.Level);
			MenuPanel.InputManager.StackControlTarget(TutorialCanvas).Forget();
			return;
		}
		if (!MenuPanel.IsRest && WanderingData.ShouldShowTutorial(ActionName.CoreStoneTapChange) && PlayerData.HasSkill())
		{
			TutorialCanvas.SetData(ActionName.CoreStoneTapChange);
			MenuPanel.InputManager.StackControlTarget(TutorialCanvas).Forget();
			return;
		}
		int horizontalDownUI = MenuPanel.InputManager.horizontalDownUI;
		int verticalDownUI = MenuPanel.InputManager.verticalDownUI;
		bool mode = MenuPanel.InputManager.mode;
		bool detail = MenuPanel.InputManager.detail;
		bool confirmDown = MenuPanel.InputManager.confirmDown;
		bool flag = MenuPanel.InputManager.menuDown || MenuPanel.InputManager.cancelDown;
		bool reset = MenuPanel.InputManager.reset;
		if (verticalDownUI != 0)
		{
			if (isDetail)
			{
				StatType data = detailStatus.MoveCursor(-verticalDownUI);
				detailInfo.SetData(data);
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
			else if (MenuPanel.IsRest)
			{
				switch (statusBoard.TryAddDiff(verticalDownUI))
				{
				case TraitResult.Success:
					ShowInfo(setDiff: true);
					statusInfo.PlayUpgradeAnimation(verticalDownUI > 0);
					PlayVfx(verticalDownUI > 0);
					break;
				case TraitResult.LackOfPoint:
					MenuPanel.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.CoreStoneWarning(TraitResult.LackOfPoint));
					pointBoard.ShowColor();
					break;
				default:
					throw new ArgumentOutOfRangeException();
				case TraitResult.LowerThanZero:
				case TraitResult.IsFullRank:
					break;
				}
			}
			else if (!isSkill)
			{
				MenuPanel.FadeCanvas.WarningBoard.ShowWarning("UI_CoreStone_Warning_UpgradeAvailableAtAltar");
			}
		}
		if (horizontalDownUI != 0 && !isDetail)
		{
			(MenuPanel.IsRest ? CurrentBoard.RotateToActive(horizontalDownUI) : CurrentBoard.Rotate(horizontalDownUI)).GetAwaiter().OnCompleted(delegate
			{
				CurrentBoard.Current.OnFocus();
			});
			ShowInfo();
			WanderingData.RemoveUnwatchedCoreStone(CurrentBoard.CurrentID);
			if (!isSkill)
			{
				detailStatus.ShowTargetStats(CurrentBoard.CurrentID);
			}
		}
		if (MenuPanel.IsRest && !isDetail)
		{
			if (confirmDown && !statusBoard.HasNoDiff)
			{
				ConfirmPopup.SetModel(new FormatConfirmModel("TraitConfirm", delegate
				{
					statusBoard.ConfirmDiffs();
					ShowInfo(setDiff: true);
					MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_Confirm");
				}, null, pointBoard.AvailablePoint.ToString()));
				MenuPanel.InputManager.StackControlTarget(ConfirmPopup).Forget();
			}
			if (flag)
			{
				ConfirmPopup.SetModel(new ConfirmModel("TraitCancel", delegate
				{
					statusBoard.ResetDiffs();
					ShowInfo(setDiff: true);
					MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_Down");
				}));
				MenuPanel.InputManager.StackControlTarget(ConfirmPopup).Forget();
			}
			if (reset)
			{
				ConfirmPopup.SetModel(new FormatConfirmModel("TraitReset", delegate
				{
					statusBoard.ResetTraits();
					ShowInfo(setDiff: true);
					MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_Confirm");
				}, null, PlayerData.TotalTraitPoint.ToString()));
				MenuPanel.InputManager.StackControlTarget(ConfirmPopup).Forget();
			}
			if (mode)
			{
				MenuPanel.FadeCanvas.WarningBoard.ShowWarning(LocaleKey.CoreStoneWarningCannotSwitch());
			}
		}
		if ((mode || (flag && isSkill)) && PlayerData.HasSkill() && SetActivateSkill(!isSkill))
		{
			ShowInfo();
			WanderingData.RemoveUnwatchedCoreStone(CurrentBoard.CurrentID);
			CurrentBoard.Current.OnFocus();
		}
		if (detail || (flag && isDetail))
		{
			SetActivateDetail(!isDetail);
			ShowGuide();
		}
	}

	private void ShowInfo(bool setDiff = false)
	{
		if (setDiff)
		{
			pointBoard.SetDiff(-statusBoard.DiffSum);
		}
		CurrentInfo.SetInfo(CurrentBoard.Current, MenuPanel.IsRest, pointBoard.AvailablePoint);
		detailStatus.ShowCurrentDiffs(statusBoard.Diffs);
		ShowGuide();
	}

	protected override void OnDeactivate()
	{
		if (MenuPanel.IsRest)
		{
			statusInfo.PlayActiveAnimation(value: false);
			skillBoard.OnDeactivate();
			statusBoard.OnDeactivate();
		}
	}

	private bool SetActivateSkill(bool value)
	{
		if (MenuPanel.IsRest || isDetail)
		{
			return false;
		}
		isSkill = value;
		skillBoard.SetActive(value);
		skillInfo.SetActive(value);
		statusBoard.SetActive(!value);
		statusInfo.SetActive(!value);
		detailStatus.SetActive(!value);
		pointBoard.SetActive(!value);
		MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewSkill");
		return true;
	}

	private void SetActivateDetail(bool value)
	{
		if (!isSkill)
		{
			isDetail = value;
			StatType statType = detailStatus.SetCursorActive(value);
			detailInfo.SetActive(value);
			statusBoard.SetActive(!value);
			statusInfo.SetActive(!value);
			pointBoard.SetActive(!value);
			if (statType != StatType.None)
			{
				detailInfo.SetData(statType);
			}
			if (value)
			{
				detailStatus.HideTargetStats();
			}
			else
			{
				detailStatus.ShowTargetStats(statusBoard.CurrentID);
			}
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus");
		}
	}

	private void PlayVfx(bool isUp)
	{
		string text = (isUp ? "Up" : "Down");
		PoolingUIParticle poolingUIParticle = Game.Instance.ObjectPoolManager.GetObject<PoolingUIParticle>("Trait " + text + "grade Vfx.prefab");
		poolingUIParticle.RectTransform.SetParent(vfxParent, worldPositionStays: false);
		poolingUIParticle.RectTransform.anchoredPosition = Vector2.zero;
		MenuPanel.SoundManager.PlayOneShot(isUp ? "event:/SFX/UI/Trait/SFX_UI_TraitPoint_Up" : "event:/SFX/UI/Trait/SFX_UI_TraitPoint_Down");
	}

	private void ShowGuide()
	{
		if (isDetail)
		{
			GuideGroup.ActivateTrait(Mode.DetailStats);
		}
		else if (isSkill)
		{
			GuideGroup.ActivateTrait(Mode.Skill);
		}
		else if (MenuPanel.IsRest)
		{
			GuideGroup.ActivateTrait(statusBoard.HasNoDiff ? Mode.PointZero : Mode.Point);
		}
		else
		{
			GuideGroup.ActivateTrait(PlayerData.HasSkill() ? Mode.Status : Mode.StatusWithoutSkill);
		}
	}
}
