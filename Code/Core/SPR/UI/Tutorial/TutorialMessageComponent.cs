using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using Library;
using Rewired;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Tutorial;

public class TutorialMessageComponent : MonoBehaviour, IControllerEventUpdate, IEventListener
{
	[SerializeField]
	private Sprite iconComplete;

	[SerializeField]
	private Image iconComponent;

	public const float FadeInDuration = 0.6f;

	private const float CompleteDelay = 0.5f;

	public const float FadeOutDuration = 1.2f;

	private static readonly Color CompleteColor = Color.white;

	private bool updateCondition;

	private bool ignoreTimeScale;

	private bool shouldGuardExpire;

	private bool shouldCameraExpire;

	private ActionName actionName;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private LocaleText description;

	private EventInstance activateEvent;

	private Action onFinish;

	private Action onDeactivate;

	private EarthKeeper earthKeeper;

	private Enemy groggyEnemy;

	private bool hasBreakableTree;

	private bool clearedBreakableTree;

	private IAttackSource rageFloodSource;

	private bool clearedRageFlood;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static PlayerController PlayerController => Game.Instance.Player;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private LocaleText Description => description ?? (description = GetComponentInChildren<LocaleText>());

	public ActionName ActionName => actionName;

	private static PlayerStateMachine PSM => Game.Instance.Player.mover.PSM;

	private static PlayerPart PlayerPart => Game.Instance.Player.PlayerPart;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static bool IsControllingPlayer => InputManager.IsCurrentControlTarget(PlayerController);

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	public void Update()
	{
		if (!updateCondition)
		{
			return;
		}
		if (CheckClear())
		{
			CompleteAsync().GetAwaiter().OnCompleted(delegate
			{
				DeactivateAsync().Forget();
			});
		}
		else if (CheckSkip())
		{
			if (actionName == ActionName.Ignition)
			{
				WanderingData.SetTutorialMessageDone(actionName);
			}
			DeactivateNow().Forget();
		}
	}

	private void OnDestroy()
	{
		if (actionName == ActionName.ChargeAttack)
		{
			LazySingleton<MessageSystem>.Instance.UnSubscribe<ChargeAttackTutorialEvent>(this);
		}
		else if (actionName == ActionName.Red)
		{
			LazySingleton<MessageSystem>.Instance.UnSubscribe<RageFloodTutorialEvent>(this);
		}
	}

	public void SetDataAsync(ActionName value, Action onFinish, Action onDeactivate, bool ignoreTimeScale = false)
	{
		actionName = value;
		this.onFinish = onFinish;
		this.onDeactivate = onDeactivate;
		this.ignoreTimeScale = ignoreTimeScale;
		UpdateText();
		WanderingData.SetTutorialMessageProgressing(actionName);
		switch (value)
		{
		case ActionName.EnemyGuard:
			earthKeeper = UnityEngine.Object.FindObjectOfType<EarthKeeper>();
			break;
		case ActionName.Execution:
			groggyEnemy = UnityEngine.Object.FindObjectsOfType<Enemy>().FirstOrDefault((Enemy enemy) => enemy.IsInGroggyState);
			break;
		case ActionName.ChargeAttack:
			LazySingleton<MessageSystem>.Instance.SubscribeUnique<ChargeAttackTutorialEvent>(this);
			hasBreakableTree = UnityEngine.Object.FindObjectsByType<BreakableTree>(FindObjectsSortMode.None).Length != 0;
			clearedBreakableTree = false;
			break;
		case ActionName.Red:
			LazySingleton<MessageSystem>.Instance.SubscribeUnique<RageFloodTutorialEvent>(this);
			rageFloodSource = Game.Instance.FieldController.Enemies.SingleOrDefault((Enemy e) => e is EarthWarrior);
			if (rageFloodSource == null)
			{
				rageFloodSource = PlayerController.mover.LastHitInfo.Source;
			}
			clearedRageFlood = false;
			break;
		}
		ActivateAsync().Forget();
		updateCondition = true;
		shouldGuardExpire = false;
		shouldCameraExpire = false;
	}

	public void OnSceneLoaded()
	{
		FieldID currentFieldID = CurrentFieldID;
		if (currentFieldID.SectorName == SectorName.SilentRuins)
		{
			int sceneIndex = currentFieldID.SceneIndex;
			if (sceneIndex >= 1 && sceneIndex <= 3)
			{
				goto IL_0027;
			}
		}
		shouldGuardExpire = true;
		goto IL_0027;
		IL_0027:
		if (!CurrentFieldID.Equals(SectorName.SilentRuins, 7))
		{
			shouldCameraExpire = true;
		}
		if (actionName == ActionName.EnemyGuard)
		{
			earthKeeper = UnityEngine.Object.FindObjectOfType<EarthKeeper>();
		}
	}

	public async UniTask DeactivateNow(bool isInstant = false)
	{
		await DeactivateAsync(isInstant);
	}

	public async UniTask ActivateAsync()
	{
		activateEvent = SoundManager.PlayOneShot("event:/SFX/UI/System/SFX_UI_Quest_Open");
		CanvasGroup.DOKill();
		CanvasGroup.alpha = 0f;
		await CanvasGroup.DOFade(1f, 0.6f).SetUpdate(ignoreTimeScale).WaitAsync();
	}

	private async UniTask DeactivateAsync(bool isInstant = false)
	{
		updateCondition = false;
		onFinish?.Invoke();
		if (activateEvent.isValid())
		{
			activateEvent.stop(STOP_MODE.IMMEDIATE);
			activateEvent.release();
		}
		CanvasGroup.DOKill();
		CanvasGroup.alpha = ((!isInstant) ? 1 : 0);
		if (!isInstant)
		{
			await UniTask.WaitForSeconds(0.5f);
			await CanvasGroup.DOFade(0f, 1.2f).SetUpdate(ignoreTimeScale).WaitAsync();
		}
		else
		{
			CanvasGroup.alpha = 0f;
		}
		onDeactivate?.Invoke();
	}

	private async UniTask CompleteAsync()
	{
		updateCondition = false;
		WanderingData.SetTutorialMessageDone(actionName);
		iconComponent.sprite = iconComplete;
		iconComponent.color = CompleteColor;
		Description.SetColor(CompleteColor);
		SoundManager.PlayOneShot("event:/SFX/UI/System/SFX_UI_Quest_Success");
		await UniTask.WaitWhile(GetSustainCondition());
	}

	private void UpdateText()
	{
		if (this.actionName == ActionName.None)
		{
			DebugLog.GeneralLogError("TutorialMessageComponent.UpdateText() : 튜토리얼이 할당되어있지 않습니다!");
			return;
		}
		string arg = "";
		ActionName actionName = this.actionName;
		if (actionName == ActionName.Move || actionName == ActionName.Camera)
		{
			arg = (RewiredHelper.IsJoystickConnected ? "_Joystick" : "_Keyboard");
		}
		Description.SetData($"Tutorial_{this.actionName}_Message{arg}");
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs obj)
	{
		UpdateText();
	}

	private static bool IsStateAny(params IState[] states)
	{
		return states.Any((IState state) => PSM.CurrentState == state);
	}

	private bool CheckClear()
	{
		int current;
		return actionName switch
		{
			ActionName.Attack => IsControllingPlayer && IsStateAny(PSM.FirstAirAttackState, PSM.BasicGroundAttackState, PSM.ChargeAttackState), 
			ActionName.Move => IsControllingPlayer && IsStateAny(PSM.WalkState, PSM.RunState), 
			ActionName.Jump => IsControllingPlayer && IsStateAny(PSM.JumpUpState, PSM.JumpDownState), 
			ActionName.Heal => ((PlayerHealState)PSM.HealingState).IsHealed, 
			ActionName.Roll => IsControllingPlayer && IsStateAny(PSM.RollingState, PSM.JumpDashState), 
			ActionName.Guard => IsControllingPlayer && IsStateAny(PSM.ParryState), 
			ActionName.Execution => IsControllingPlayer && IsStateAny(PSM.BurstState, PSM.HyperBurstState), 
			ActionName.TraitPoint => IsStateAny(PSM.SaveState) && PlayerData.CardDataList.Sum((CardData card) => card.RawLevel) > 0, 
			ActionName.EnemyGuard => !earthKeeper.IsNull() && !earthKeeper.CanMakeStuck && IsStateAny(PSM.ChargeAttackState), 
			ActionName.ParryAction => IsControllingPlayer && IsStateAny(PSM.PyronAttackState, PSM.HyperPyronAttackState), 
			ActionName.Camera => IsControllingPlayer && PlayerController.GetSightOffset().magnitude >= pcp.MoveSightInputThreshold, 
			ActionName.ChargeAttack => clearedBreakableTree, 
			ActionName.Ignition => PlayerController.overheat, 
			ActionName.Map => InputManager.IsCurrentControlTarget(UIManager.MenuCanvas.MapPanel), 
			ActionName.Part => PlayerPart.IsEquippedFull, 
			ActionName.WetStatusEffect => IsStateAny(PSM.RollingState, PSM.JumpDashState) && !PlayerController.StatusEffectController.Contain<WetStatusEffect>(), 
			ActionName.Food => PlayerData.HasEverObtainedItem("CookingLog", out current), 
			ActionName.Red => clearedRageFlood, 
			_ => true, 
		};
	}

	public bool CheckSkip()
	{
		switch (actionName)
		{
		case ActionName.Heal:
			return !TutorialCanvas.ShouldShowHealTutorial();
		case ActionName.Guard:
			return shouldGuardExpire;
		case ActionName.Execution:
			return groggyEnemy.IsNull() || !groggyEnemy.IsInGroggyState;
		case ActionName.EnemyGuard:
			return (object)earthKeeper == null || !earthKeeper.CanMakeStuck;
		case ActionName.ParryAction:
			return !Game.Instance.GraphicManager.ParryActionDirector.IsProducing;
		case ActionName.Camera:
			return shouldCameraExpire;
		case ActionName.ChargeAttack:
			return !hasBreakableTree;
		case ActionName.Ignition:
			return IsStateAny(PSM.SaveState);
		case ActionName.WetStatusEffect:
			return !PlayerController.StatusEffectController.Contain<WetStatusEffect>();
		case ActionName.Red:
		{
			IAttackSource attackSource = rageFloodSource;
			return (attackSource == null || attackSource is Enemy { IsDead: not false }) ? true : false;
		}
		default:
			return false;
		}
	}

	private Func<bool> GetSustainCondition()
	{
		return actionName switch
		{
			ActionName.Execution => () => IsStateAny(PSM.BurstState), 
			ActionName.Camera => () => PlayerController.GetSightOffset().magnitude >= pcp.MoveSightInputThreshold, 
			ActionName.TraitPoint => () => !IsControllingPlayer, 
			ActionName.Map => () => !IsControllingPlayer, 
			ActionName.Part => () => !IsControllingPlayer, 
			ActionName.Food => () => !IsControllingPlayer, 
			_ => () => false, 
		};
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is ChargeAttackTutorialEvent)
		{
			if (hasBreakableTree)
			{
				clearedBreakableTree = true;
			}
		}
		else if (e is RageFloodTutorialEvent)
		{
			clearedRageFlood = true;
		}
	}
}
