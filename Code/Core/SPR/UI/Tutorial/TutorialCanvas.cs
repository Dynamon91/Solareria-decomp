using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SPR.UI.Tutorial;

public class TutorialCanvas : AutoScaledCanvas, IControllable, IControllerEventUpdate, IEventListener
{
	private const string ConfirmKey = "UI_KeyGuide_Confirm";

	private const string ConfirmKeyHold = "UI_KeyGuide_Confirm_Hold";

	[SerializeField]
	private RawImage videoScreen;

	[SerializeField]
	private VideoPlayer videoPlayer;

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText actionDescText;

	[SerializeField]
	private LocaleText actionKeyGuide;

	[SerializeField]
	private TutorialPanelPageMarker pageMarker;

	[SerializeField]
	private GameObject burstIcon;

	[SerializeField]
	private HoldInput holdInput;

	[SerializeField]
	private RectTransform endMarker;

	[SerializeField]
	private float inputStartPreventTime;

	[SerializeField]
	private float inputContinuousPreventTime;

	[Header("Key Guide")]
	[SerializeField]
	private KeyGuideItem previousKeyGuide;

	[SerializeField]
	private KeyGuideItem nextKeyGuide;

	[SerializeField]
	private KeyGuideItem confirmKeyGuide;

	private const int ActionTextHighPosY = -180;

	private const int ActionTextLowPosY = -210;

	private const float HoldDuration = 1.2f;

	private float inputTimer = float.MaxValue;

	private bool notStopTime;

	private ActionName currentActionName;

	private Animator animator;

	private static readonly int Open = Animator.StringToHash("Open");

	private bool ignoreFirst;

	private readonly List<ActionName> shouldHoldConfirmInput = new List<ActionName>
	{
		ActionName.Parry,
		ActionName.ParryAction,
		ActionName.Execution,
		ActionName.Ignition,
		ActionName.Teleport,
		ActionName.SpecialParry,
		ActionName.HyperBurst,
		ActionName.BaranWarningForDemo,
		ActionName.GuardiansOfTerradinForDemo
	};

	private bool _videoError;

	private string _videoErrorMsg;

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerController Player => Game.Instance.Player;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static TutorialMessageBoard TutorialMessageBoard => Game.Instance.UIManager.FieldCanvas.TutorialMessageBoard;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public bool IsValid => true;

	public string ControllerName()
	{
		return "Tutorial Panel";
	}

	public void Initialize()
	{
		base.gameObject.SetActive(value: false);
		nameText.gameObject.SetActive(value: false);
		actionDescText.gameObject.SetActive(value: false);
		videoPlayer.clip = null;
		actionKeyGuide.gameObject.SetActive(value: false);
		pageMarker.SetData(0, 0);
		burstIcon.SetActive(value: false);
		videoScreen.color = Color.white;
		ObservePlayerTraitPoint();
		RewiredHelper.RegisterControllerChangeEvent(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<InflarePointTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<EarthGuardStuckTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<SproutTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<HeatHazeTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<CounterTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<DashPassWallTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<JumpDashAttackTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.SubscribeUnique<HeatCoreStoneTutorialEvent>(this);
	}

	private void OnDestroy()
	{
		RewiredHelper.RemoveControllerChangeEvent(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<InflarePointTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<EarthGuardStuckTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<SproutTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<HeatHazeTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<CounterTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<DashPassWallTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<JumpDashAttackTutorialEvent>(this);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<HeatCoreStoneTutorialEvent>(this);
	}

	public void SetData(ActionName actionName)
	{
		currentActionName = actionName;
		nameText.gameObject.SetActive(value: true);
		actionDescText.gameObject.SetActive(value: true);
		nameText.SetData(LocaleKey.TutorialName(actionName));
		actionDescText.SetData(LocaleKey.TutorialActionDesc(actionName));
		bool flag = ActionNameUtil.ActionNameHasKeyGuide.Contains(actionName);
		actionKeyGuide.gameObject.SetActive(flag);
		Vector2 anchoredPosition = actionDescText.RectTransform.anchoredPosition;
		anchoredPosition.y = (flag ? (-210) : (-180));
		actionDescText.RectTransform.anchoredPosition = anchoredPosition;
		SetTutorialKeyGuide();
		pageMarker.SetData(CurrentPageCount(), TotalPageCount());
		burstIcon.SetActive(actionName == ActionName.Execution);
		SetTutorialGraphicAsync(actionName).Forget();
		WanderingData.SetTutorialDone(actionName);
		SetKeyGuide();
	}

	public static void ObservePlayerHealth()
	{
		if (WanderingData.ShouldShowTutorialMessage(ActionName.Heal))
		{
			if (CurrentFieldID.SceneIndex > 3 && CurrentFieldID.SceneIndex != 7)
			{
				WanderingData.SetTutorialMessageDone(ActionName.Heal);
			}
			else if (ShouldShowHealTutorial())
			{
				TutorialMessageBoard.AddMessage(ActionName.Heal);
			}
		}
	}

	public static bool ShouldShowHealTutorial()
	{
		Health stat = Player.PlayerStat.GetStat<Health>();
		return (float)stat.CurrentHealth <= (float)stat.MaxHealth * 0.5f;
	}

	private void ObservePlayerTraitPoint()
	{
		if (WanderingData.ShouldShowNewTutorialMessage(ActionName.TraitPoint) && PlayerData.TotalTraitPoint != 0)
		{
			FieldID fieldID = new FieldID(SectorName.SilentRuins, 2);
			bool num = Game.Instance.SaveData.WorldData.IsVisitedField(fieldID);
			bool flag = CurrentFieldID == fieldID;
			if (num || flag)
			{
				TutorialMessageBoard.AddMessage(ActionName.TraitPoint);
			}
		}
	}

	private void ObservePlayerStuck()
	{
		if (Game.Instance.CurrentFieldID.SectorName != SectorName.SilentRuins || Game.Instance.CurrentFieldID.SceneIndex != 2)
		{
			return;
		}
		if (!WanderingData.ShouldShowTutorial(ActionName.EnemyGuard))
		{
			if (!TutorialMessageBoard.HasMessage(ActionName.EnemyGuard) && WanderingData.ShouldShowTutorialMessage(ActionName.EnemyGuard))
			{
				TutorialMessageBoard.AddMessage(ActionName.EnemyGuard);
			}
			return;
		}
		SetData(ActionName.EnemyGuard);
		PlayerStateMachine psm = Game.Instance.Player.mover.PSM;
		UniTask.WaitUntil(() => psm.CurrentState != psm.AttackStuckState).GetAwaiter().OnCompleted(delegate
		{
			Game.Instance.InputManager.StackControlTarget(this).Forget();
		});
	}

	private void ObserveTutorial(ActionName actionName)
	{
		if (WanderingData.ShouldShowTutorial(actionName))
		{
			WanderingData.SetTutorialDone(actionName);
			UniTask.WaitUntil(() => InputManager.IsCurrentControlTarget(Player)).GetAwaiter().OnCompleted(delegate
			{
				SetData(actionName);
				Game.Instance.InputManager.StackControlTarget(this).Forget();
			});
		}
	}

	private async UniTask SetTutorialGraphicAsync(ActionName actionName)
	{
		videoScreen.texture = null;
		videoScreen.SetAlpha(0f);
		if (ActionNameUtil.ActionNameShowsImage.Contains(actionName))
		{
			await SetTextureAsync(actionName);
		}
		else
		{
			await SetVideoAsync(actionName);
		}
	}

	private async UniTask SetVideoAsync(ActionName actionName)
	{
		AsyncOperationHandle<VideoClip> task = Addressables.LoadAssetAsync<VideoClip>(Address.TutorialExample(actionName));
		await task;
		if ((object)task.Result == null)
		{
			task = Addressables.LoadAssetAsync<VideoClip>(Address.TutorialExample(actionName, Language.English));
			await task;
		}
		if ((object)task.Result == null)
		{
			DebugLog.AddressableLogWarning($"[Tutorial] 비디오 클립 로드에 실패했습니다. action={actionName}");
			videoScreen.SetAlpha(0f);
			return;
		}
		videoPlayer.clip = task.Result;
		if (await UniTask.WhenAny(UniTask.WaitUntil(() => videoPlayer.isActiveAndEnabled), UniTask.Delay(TimeSpan.FromSeconds(1.0), DelayType.Realtime)) != 0)
		{
			DebugLog.GeneralLogWarning("[Tutorial] 비디오 플레이어가 시간 내에 활성화되지 않았습니다.");
			videoScreen.SetAlpha(0f);
			return;
		}
		videoPlayer.Prepare();
		int num = await UniTask.WhenAny(UniTask.WaitUntil(() => videoPlayer.isPrepared || _videoError), UniTask.Delay(TimeSpan.FromSeconds(1.0), DelayType.Realtime));
		if (_videoError || num != 0 || !videoPlayer.isPrepared || videoPlayer.texture == null)
		{
			DebugLog.GeneralLogWarning("[Tutorial] Video prepare failed. err=" + _videoErrorMsg);
			videoScreen.SetAlpha(0f);
		}
		else
		{
			videoScreen.texture = videoPlayer.texture;
			videoScreen.SetAlpha(1f);
			videoPlayer.Play();
		}
	}

	private async UniTask SetTextureAsync(ActionName actionName)
	{
		AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.TutorialExample(actionName));
		await task;
		if ((object)task.Result == null)
		{
			task = Addressables.LoadAssetAsync<Sprite>(Address.TutorialExample(actionName, Language.English));
			await task;
		}
		Sprite result = task.Result;
		videoScreen.texture = result.texture;
		videoScreen.SetAlpha(1f);
	}

	public void SetNotStopTime()
	{
		notStopTime = Game.Instance.RuntimeData.GetAvailableStatusEffectTutorialCount() > 0;
	}

	public async UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		Animator.SetBool(Open, value: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Show");
		if (!notStopTime && !ActionNameUtil.ActionNameAtBonfire.Contains(currentActionName) && !InputManager.ContainsInControlStack(Game.Instance.UIManager.RestCanvas))
		{
			StopTimeAfter2FramesAsync().Forget();
		}
		RebuildLayout();
		DebugLog.ControlStackLog("Before WaitUntil");
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		DebugLog.ControlStackLog("After WaitUntil");
		if (await UniTask.WhenAny(UniTask.WaitUntil(() => videoScreen.texture != null), UniTask.Delay(TimeSpan.FromSeconds(1.0), DelayType.Realtime)) != 0)
		{
			DebugLog.GeneralLogWarning("[Tutorial] videoScreen.texture is not set within timeout.");
			videoScreen.SetAlpha(0f);
		}
		DebugLog.ControlStackLog("After wait until videoScreen.texture");
		inputTimer = inputStartPreventTime;
		ignoreFirst = true;
		notStopTime = false;
	}

	public void Control()
	{
		bool flag = !ignoreFirst && InputManager.horizontalDownUI > 0;
		if (ignoreFirst && InputManager.horizontalDownUI <= 0)
		{
			ignoreFirst = false;
		}
		if (inputTimer > 0f)
		{
			inputTimer -= Time.unscaledDeltaTime;
			return;
		}
		bool num = flag || InputManager.confirmDown;
		bool flag2 = InputManager.horizontalDownUI < 0;
		bool flag3 = holdInput.CheckInput(InputManager.confirm, ignoreTimeScale: true);
		if (num)
		{
			(ActionName, ActionName) tuple = ActionNameUtil.ActionNameHasPage.FirstOrDefault(((ActionName former, ActionName latter) tuple4) => currentActionName == tuple4.former);
			DebugLog.GeneralLog($"next : pair = ({tuple.Item1}, {tuple.Item2})");
			(ActionName, ActionName) tuple2 = tuple;
			if (tuple2.Item1 != ActionName.None || tuple2.Item2 != ActionName.None)
			{
				SetData(tuple.Item2);
				inputTimer = inputContinuousPreventTime;
				return;
			}
		}
		if (flag3)
		{
			endMarker.DOKill(complete: true);
			endMarker.DONodYOnce(-10f, 0.25f).SetUpdate(isIndependentUpdate: true);
			InputManager.RemoveControlTarget(this).Forget();
			inputTimer = inputContinuousPreventTime;
		}
		else if (flag2)
		{
			(ActionName, ActionName) tuple3 = ActionNameUtil.ActionNameHasPage.FirstOrDefault(((ActionName former, ActionName latter) tuple4) => currentActionName == tuple4.latter);
			DebugLog.GeneralLog($"previous : pair = ({tuple3.Item1}, {tuple3.Item2})");
			(ActionName, ActionName) tuple2 = tuple3;
			if (tuple2.Item1 != ActionName.None || tuple2.Item2 != ActionName.None)
			{
				SetData(tuple3.Item1);
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

	public async UniTask Deactivate()
	{
		DebugLog.GeneralLog($"Deactivate : currentActionName = {currentActionName}");
		(ActionName, ActionName) tuple = ActionNameUtil.ActionNameAlsoShowsDifferentMessage.FirstOrDefault(((ActionName former, ActionName latter) tuple3) => tuple3.former.Equals(currentActionName));
		(ActionName, ActionName) tuple2 = tuple;
		if (tuple2.Item1 != ActionName.None || tuple2.Item2 != ActionName.None)
		{
			TutorialMessageBoard.AddMessage(tuple.Item2);
		}
		else if (ActionNameUtil.ActionNameAlsoShowsMessage.Contains(currentActionName))
		{
			TutorialMessageBoard.AddMessage(currentActionName);
		}
		ActionName previousAction = currentActionName;
		currentActionName = ActionName.None;
		Animator.SetBool(Open, value: false);
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Hide");
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f);
		if (previousAction != ActionName.ParryAction)
		{
			Game.SetTimeScale(1f);
		}
		inputTimer = float.MaxValue;
	}

	private async UniTask StopTimeAfter2FramesAsync()
	{
		DebugLog.ControlStackLog("StopTimeAfter2FramesAsync called");
		await UniTask.Yield();
		await UniTask.Yield();
		Game.SetTimeScale(0f);
		DebugLog.ControlStackLog("StopTimeAfter2FramesAsync finished");
	}

	private void SetTutorialKeyGuide()
	{
		if (ActionNameUtil.ActionNameHasKeyGuide.Contains(currentActionName))
		{
			ActionName actionName = ((currentActionName == ActionName.Parry) ? ActionName.Guard : currentActionName);
			actionKeyGuide.SetData($"Tutorial_{actionName}_ActionName");
		}
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		SetTutorialKeyGuide();
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is InflarePointTutorialEvent)
		{
			ObservePlayerTraitPoint();
		}
		else if (e is EarthGuardStuckTutorialEvent)
		{
			ObservePlayerStuck();
		}
		else if (e is SproutTutorialEvent)
		{
			ObserveTutorial(ActionName.Sprout);
		}
		else if (e is HeatHazeTutorialEvent)
		{
			ObserveTutorial(ActionName.HeatHaze);
		}
		else if (e is CounterTutorialEvent)
		{
			ObserveTutorial(ActionName.Counter);
		}
		else if (e is DashPassWallTutorialEvent)
		{
			ObserveTutorial(ActionName.DashPassWall);
		}
		else if (e is JumpDashAttackTutorialEvent)
		{
			ObserveTutorial(ActionName.JumpDashAttack);
		}
		else if (e is HeatCoreStoneTutorialEvent)
		{
			ObserveTutorial(ActionName.HeatCoreStone);
		}
	}

	private int TotalPageCount()
	{
		if (ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) pair) => pair.former == currentActionName || pair.latter == currentActionName))
		{
			return 2;
		}
		return 1;
	}

	private int CurrentPageCount()
	{
		if (ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) pair) => pair.former == currentActionName))
		{
			return 1;
		}
		if (ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) pair) => pair.latter == currentActionName))
		{
			return 2;
		}
		return 0;
	}

	private bool IsFirstPage()
	{
		return ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) pair) => pair.former == currentActionName);
	}

	private bool IsLastPage()
	{
		return ActionNameUtil.ActionNameHasPage.Any(((ActionName former, ActionName latter) pair) => pair.latter == currentActionName);
	}

	private void SetKeyGuide()
	{
		float num = (shouldHoldConfirmInput.Contains(currentActionName) ? 1.2f : 0f);
		holdInput.Initialize(num);
		confirmKeyGuide.SetData((num <= 0f) ? "UI_KeyGuide_Confirm" : "UI_KeyGuide_Confirm_Hold");
		if (TotalPageCount() == 1)
		{
			previousKeyGuide.Deactivate();
			nextKeyGuide.Deactivate();
			confirmKeyGuide.Activate();
			return;
		}
		if (IsFirstPage())
		{
			previousKeyGuide.Deactivate();
			nextKeyGuide.Activate();
			confirmKeyGuide.Deactivate();
		}
		else if (IsLastPage())
		{
			previousKeyGuide.Activate();
			nextKeyGuide.Deactivate();
			confirmKeyGuide.Activate();
		}
		RebuildLayout();
	}

	private void RebuildLayout()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(confirmKeyGuide.transform.parent.GetComponent<RectTransform>());
	}

	private void OnVideoErrorReceived(VideoPlayer source, string message)
	{
		_videoError = true;
		_videoErrorMsg = message;
		DebugLog.GeneralLogWarning("[Tutorial] VideoPlayer errorReceived: " + message);
	}

	private void OnEnable()
	{
		videoPlayer.errorReceived += OnVideoErrorReceived;
	}

	private void OnDisable()
	{
		videoPlayer.errorReceived -= OnVideoErrorReceived;
	}
}
