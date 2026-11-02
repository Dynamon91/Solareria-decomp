using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR;
using SPR.Achievement;
using SPR.Cutscene;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using SPR.UI.Menu;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour, IControllable
{
	public float horizontal;

	public int vertical;

	public bool jump;

	public bool attackDown;

	public bool attackUp;

	public bool attack;

	public bool dashDown;

	public bool dashUp;

	public bool healDown;

	public bool interact;

	public bool skillA;

	public bool skillB;

	public bool action;

	public bool heat;

	public bool heatDown;

	public bool overheat;

	public bool guardUp;

	public bool guardDown;

	public bool specialParry;

	public bool isDead;

	public float sightHorizontal;

	public float sightVertical;

	public bool teleportDown;

	public bool teleportUp;

	[Space]
	[SerializeField]
	private HeatEffectController heatEffectController = new HeatEffectController();

	[SerializeField]
	private CameraPanningController cameraPanningController = new CameraPanningController();

	private CapsuleCollider2D cc;

	private SoulDropInfo soulDropInfo;

	private bool useSpecialDropInfo;

	private const float WalkingSpeed = 0.26f;

	private EndingCutsceneProducer endingCutsceneProducer;

	private CapsuleCollider2D CapsuleCollider => cc ?? (cc = GetComponent<CapsuleCollider2D>());

	public PlayerPart PlayerPart { get; private set; }

	public PlayerStat PlayerStat { get; private set; }

	public PlayerMover mover { get; private set; }

	public IInteractable Interactable { get; private set; }

	public StatusEffectController StatusEffectController { get; private set; }

	public HeatEffectController HeatEffectController => heatEffectController;

	public PlayerPopUpCanvas PlayerPopUpCanvas { get; private set; }

	public bool IsInSceneLoadSequence { get; set; }

	public bool ForceWalk { get; set; }

	private static InputManager InputManager => Game.Instance.InputManager;

	private static FieldController Field => Game.Instance.FieldController;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static MenuCanvas MenuCanvas => Game.Instance.UIManager.MenuCanvas;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	private bool CanControl
	{
		get
		{
			if (!isDead)
			{
				return InputManager.IsCurrentControlTarget(this);
			}
			return false;
		}
	}

	public bool IsValid => !this.IsNull();

	public bool IsEndingCutscene => !endingCutsceneProducer.IsNull();

	private void Awake()
	{
		mover = GetComponent<PlayerMover>();
		UnityEngine.Object.DontDestroyOnLoad(this);
		StatusEffectController = new StatusEffectController(mover);
		heatEffectController.Initialize();
	}

	public void InitializePlayer()
	{
		PlayerPart = new PlayerPart();
		PlayerStat = new PlayerStat(mover, PlayerPart);
		mover.PSM.Initialize();
		mover.ApplyDifficulty(Game.Instance.SaveData.Difficulty);
	}

	public string ControllerName()
	{
		return "Player";
	}

	public UniTask Activate()
	{
		PlayerStat.ApplyAllTraits();
		PlayerStat.ApplyAllFoods();
		PlayerPart.Initialize(PlayerData);
		FieldCanvas.PlayerStatusBoard.UpdateBarSize(instant: true);
		InitializePopupCanvasAsync().Forget();
		return default(UniTask);
	}

	private async UniTask InitializePopupCanvasAsync()
	{
		if (PlayerPopUpCanvas.IsNull())
		{
			PlayerPopUpCanvas = await AddressableHelper.InstantiateAsync<PlayerPopUpCanvas>("Player Effect Canvas.prefab", base.transform);
			PlayerPopUpCanvas.Initialize(4f);
		}
	}

	private void Update()
	{
		mover.PSM.UpdateInput();
	}

	private void FixedUpdate()
	{
		mover.PSM.UpdateInput();
		if (!Game.Instance.DevController.IsDevMode)
		{
			mover.Move();
			_ = Time.fixedDeltaTime;
			if (!isDead && !IsInSceneLoadSequence && Game.FieldID.SectorName != SectorName.None)
			{
				PlayerPart.OnFixedUpdate();
				PlayerStat.OnFixedUpdate();
			}
			cameraPanningController.Control(GetSightOffset());
			if (isDead)
			{
				StatusEffectController.Clear();
			}
			StatusEffectController.Running();
		}
	}

	public void Control()
	{
		if (IsEndingCutscene)
		{
			ControlOnEnding();
			return;
		}
		bool canControl = CanControl;
		horizontal = ((!canControl || !(Mathf.Abs(InputManager.horizontal) >= pcp.WalkHorizontalInputThreshold)) ? 0f : (ForceWalk ? ((float)InputManager.horizontal.GetSign() * pcp.WalkHorizontalInputThreshold) : InputManager.horizontal));
		vertical = (canControl ? InputManager.vertical : 0);
		jump = canControl && (jump || InputManager.jumpDown) && !InputManager.jumpUp;
		attackDown = canControl && InputManager.attackDown;
		attackUp = canControl && InputManager.attackUp;
		attack = canControl && InputManager.attack;
		dashDown = canControl && InputManager.dashDown;
		dashUp = canControl && InputManager.dashUp;
		healDown = canControl && InputManager.healDown;
		skillA = canControl && InputManager.skillA;
		skillB = canControl && InputManager.skillB;
		overheat = canControl && InputManager.overheat;
		action = canControl && InputManager.action;
		heat = canControl && InputManager.heat;
		heatDown = canControl && InputManager.heatDown;
		guardDown = canControl && InputManager.guardDown && !InputManager.guardUp && !InputManager.ctrl;
		guardUp = canControl && InputManager.guardUp;
		specialParry = canControl && InputManager.specialParry;
		sightHorizontal = (canControl ? InputManager.sightHorizontal : 0f);
		sightVertical = (canControl ? InputManager.sightVertical : 0f);
		teleportDown = canControl && InputManager.heatDown;
		teleportUp = canControl && InputManager.heatUp;
		interact = canControl && InputManager.interactDown;
		CheckInteractable();
		if (interact && Interactable != null)
		{
			Interactable.Interact();
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		}
		OpenUI();
		if (canControl && InputManager.pauseDown)
		{
			InputManager.StackControlTarget(Game.Instance.UIManager.FieldCanvas.PausePanel).Forget();
		}
	}

	private void ControlOnEnding()
	{
		bool canControl = CanControl;
		horizontal = ((canControl && Mathf.Abs(InputManager.horizontal) >= pcp.WalkHorizontalInputThreshold) ? (InputManager.horizontal * 0.26f) : 0f);
		vertical = 0;
		jump = false;
		attackDown = false;
		attackUp = false;
		attack = false;
		dashDown = false;
		dashUp = false;
		healDown = false;
		interact = false;
		skillA = false;
		skillB = false;
		overheat = false;
		action = false;
		heat = false;
		heatDown = false;
		guardDown = false;
		guardUp = false;
		specialParry = false;
		sightHorizontal = 0f;
		sightVertical = 0f;
		teleportDown = false;
		teleportUp = false;
	}

	public static void OpenUI()
	{
		if (Field.GetCutsceneProducers().Any((CutsceneProducer producer) => producer.IsProducing) || Game.Instance.GraphicManager.ParryActionDirector.IsTutorialOn)
		{
			return;
		}
		bool menuDown = InputManager.menuDown;
		bool map = InputManager.map;
		_ = InputManager.pauseDown;
		if (map)
		{
			if (FieldCanvas.MessageBoard.IsPlaying)
			{
				MenuCategory targetMenu = FieldCanvas.MessageBoard.TargetMenu;
				if (FieldCanvas.MessageBoard.TryGetMemoryData(out var category, out var id))
				{
					MenuCanvas.MemoryPanel.ShowCurrentData(category, id);
				}
				MenuCanvas.SetDefaultOpenCategory(targetMenu);
			}
			else
			{
				MenuCanvas.SetDefaultOpenCategory(MenuCategory.Map);
			}
			InputManager.StackControlTarget(MenuCanvas).Forget();
		}
		else if (menuDown)
		{
			InputManager.StackControlTarget(MenuCanvas).Forget();
		}
	}

	public UniTask Pause()
	{
		ResetAllInput();
		Interactable?.InteractExit();
		Interactable = null;
		mover.PSM.CancelInput();
		return default(UniTask);
	}

	public UniTask Restart()
	{
		ResetAllInput();
		if (FieldID.IsField(SceneManager.GetActiveScene()) && Field.GetCutsceneProducers().Any((CutsceneProducer producer) => producer.IsProducingOnlyPlayableDirector))
		{
			return default(UniTask);
		}
		FieldCanvas.ShowAllAsync(instant: false).Forget();
		AchievementHelper.CheckTriggers();
		return default(UniTask);
	}

	public UniTask Deactivate()
	{
		StatusEffectController.Clear();
		StatusEffectController.Running();
		ResetAllInput();
		Interactable?.InteractExit();
		Interactable = null;
		PlayerStat.ResetAllTraits();
		mover.OnDeactivate();
		return default(UniTask);
	}

	public bool StopMove()
	{
		ResetAllInput();
		if (mover.IsNull())
		{
			return false;
		}
		mover.ResetRelativeVelocity();
		return true;
	}

	public void StopMoveAndForceToIdleState()
	{
		if (StopMove())
		{
			mover.SetAnimationInstantly("idle");
			mover.PSM.ChangeState(mover.PSM.IdleState);
			mover.UnloadRideable();
		}
	}

	public async UniTask MovePositionAsync(Vector2 targetPosition, bool isRunning, bool silentY, CancellationToken token = default(CancellationToken))
	{
		UniTask uniTask = MoveXPositionAsync(targetPosition.x, isRunning, token);
		UniTask uniTask2 = (silentY ? MoveYPositionSilentlyAsync(targetPosition.y, token) : MoveYPositionAsync(targetPosition.y, token));
		await UniTask.WhenAll(uniTask, uniTask2);
	}

	public async UniTask MoveXPositionAsync(float targetX, bool isRunning, CancellationToken token = default(CancellationToken))
	{
		float x = base.transform.position.x;
		float initialDelta = targetX - x;
		if (Mathf.Abs(initialDelta) < 0.01f)
		{
			return;
		}
		int moveDirection = initialDelta.GetSign();
		float moveSpeed = (isRunning ? 1f : 0.26f);
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				horizontal = 0f;
				return;
			}
			if (initialDelta.GetSign() != (targetX - base.transform.position.x).GetSign())
			{
				break;
			}
			horizontal = (float)moveDirection * moveSpeed;
			await UniTask.Yield();
		}
		base.transform.position = new Vector2(targetX, base.transform.position.y);
		horizontal = 0f;
	}

	public async UniTask MoveYPositionAsync(float targetY, CancellationToken token = default(CancellationToken))
	{
		float y = base.transform.position.y;
		float initialDelta = targetY - y;
		if (Mathf.Abs(initialDelta) < 0.01f)
		{
			return;
		}
		bool moveUp = initialDelta.GetSign() > 0;
		if (moveUp)
		{
			mover.ApplyRelativeVelocity(0f, pcp.JumpVelocity.y, faceJump: false);
			mover.PSM.SetStateOptionActive(StateOption.GravityFree, active: true);
			mover.SetAnimation("jump_up_loop");
		}
		else
		{
			if (!IsInSceneLoadSequence)
			{
				mover.SetAnimation("jump_down_loop");
			}
			mover.PSM.SetStateOptionActive(StateOption.GravityFree, active: false);
		}
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if (initialDelta.GetSign() != (targetY - base.transform.position.y).GetSign())
			{
				break;
			}
			jump = moveUp;
			await UniTask.Yield();
		}
		if (moveUp)
		{
			mover.ResetRelativeVelocity();
		}
		base.transform.position = new Vector2(base.transform.position.x, targetY);
		mover.PSM.SetStateOptionActive(StateOption.GravityFree, active: false);
		jump = false;
	}

	private async UniTask MoveYPositionSilentlyAsync(float targetY, CancellationToken token = default(CancellationToken))
	{
		float y = base.transform.position.y;
		float f = targetY - y;
		if (!(Mathf.Abs(f) < 0.01f))
		{
			mover.PSM.SetStateOptionActive(StateOption.GravityFree, active: true);
			float duration = Mathf.Abs(f);
			await base.transform.DOMoveY(targetY, duration).SetEase(Ease.Linear).WaitAsync(token);
			mover.PSM.SetStateOptionActive(StateOption.GravityFree, active: false);
		}
	}

	public void SetPosition(Vector2 position, bool isFacingRight = true)
	{
		if (position == Util.DisablePosition)
		{
			StopMove();
			base.gameObject.SetActive(value: false);
		}
		else
		{
			base.transform.position = position;
			mover.Flip(isFacingRight ? 1 : (-1));
			base.gameObject.SetActive(value: true);
		}
	}

	public async UniTask SetPositionAsync(Vector2 position, Action onEnd, bool isFacingRight = true)
	{
		NullControllable nullControllable = new NullControllable("Set Position");
		await InputManager.StackControlTarget(nullControllable);
		await Game.Instance.UIManager.FadeCanvas.FadeOut(0.5f);
		SetPosition(position, isFacingRight);
		mover.PSM.ChangeState(mover.PSM.IdleState);
		await UniTask.Yield();
		Game.Instance.UIManager.FadeCanvas.FadeIn(1f).Forget();
		await UniTaskExtension.WaitForSeconds(0.5f);
		InputManager.RemoveControlTarget(nullControllable).Forget();
		onEnd?.Invoke();
	}

	public async UniTask KillPlayer(IAttackSource source)
	{
		if (isDead)
		{
			return;
		}
		FieldCanvas.PlayerStatusBoard.EffectBoard.HideAllIcons();
		isDead = true;
		mover.PSM.ChangeState(mover.PSM.DieState);
		PlayerStat.ResidualHeat.RemoveResidualHeat(PlayerStat.ResidualHeat.Value);
		PlayerStat.GetStat<Health>().Darkness = 0f;
		float loseRate = PlayerStat.GetStat<GoodsProtection>().LoseRate;
		int num = Mathf.CeilToInt((float)PlayerStat.CurrentExp * loseRate);
		int num2 = Mathf.CeilToInt((float)PlayerStat.Money * loseRate);
		PlayerData.AddDeathCount(source, mover.VisualBottom);
		PlayerStat.AddExperience(-num);
		PlayerStat.AddMoney(-num2);
		PlayerPart.Reset();
		StatusEffectController.Clear(force: true);
		if (!soulDropInfo.IsValid || !useSpecialDropInfo)
		{
			if (Game.FieldID.Equals(SectorName.WorldsEnd, 3))
			{
				SetSoulDropPosition(new SoulDropInfo(areaBox: new AreaBox(new Vector2Int(-300, -25), new Vector2Int(450, 25)), worldPos: new Vector2(314f, -19f), fieldID: new FieldID(SectorName.WorldsEnd, 2)));
			}
			else if (Game.FieldID.Equals(SectorName.WatermirrorBasin, 28))
			{
				SetSoulDropPosition(new SoulDropInfo(areaBox: new AreaBox(new Vector2Int(-95, -225), new Vector2Int(95, -36)), worldPos: new Vector2(-10f, -116f), fieldID: new FieldID(SectorName.WatermirrorBasin, 25)));
			}
			else
			{
				AreaBox areaBox = Field.MainArea.AreaBox;
				Vector2 playerDropPosition = Field.CurrentArea.PlayerDropPosition;
				SetSoulDropPosition(new SoulDropInfo(Game.Instance.CurrentFieldID, playerDropPosition, areaBox));
			}
		}
		Game.Instance.SaveData.PlayerData.SetDropData(soulDropInfo.FieldID, soulDropInfo.WorldPosition, soulDropInfo.AreaNormalizedPosition, num, num2);
		await Game.Instance.Respawn(source);
		isDead = false;
		mover.PSM.ChangeState(mover.PSM.IdleState);
	}

	public void SetSoulDropPosition(SoulDropInfo info)
	{
		soulDropInfo = info;
	}

	public void SetSpecialDropInfo(bool value)
	{
		useSpecialDropInfo = value;
	}

	private void CheckInteractable()
	{
		if (mover.PSM.ParryAttackInputItem.Active | (mover.PSM.CurrentState == mover.PSM.PyronAttackState))
		{
			Interactable?.InteractExit();
			Interactable = null;
			return;
		}
		ContactFilter2D contactFilter = default(ContactFilter2D).NoFilter();
		List<Collider2D> list = new List<Collider2D>();
		Physics2D.OverlapCollider(CapsuleCollider, contactFilter, list);
		IInteractable interactable = Interactable;
		IInteractable interactable2 = interactable;
		List<IInteractable> list2 = new List<IInteractable>();
		foreach (Collider2D item in list)
		{
			if (item.TryGetComponent<IInteractable>(out var component))
			{
				list2.Add(component);
			}
		}
		bool flag = interactable != null && list2.Contains(interactable);
		int num = (flag ? interactable.InteractableOrder : (-1));
		foreach (IInteractable item2 in list2)
		{
			if (item2.InteractableOrder > num)
			{
				interactable2 = item2;
				num = interactable2.InteractableOrder;
			}
		}
		if (interactable2 != interactable)
		{
			interactable2.InteractEnter();
			if (interactable != null && !interactable.IsNull)
			{
				interactable.InteractExit();
			}
		}
		else if (!flag)
		{
			if (interactable != null && !interactable.IsNull)
			{
				interactable.InteractExit();
			}
			interactable2 = null;
		}
		Interactable = interactable2;
	}

	private void ResetAllInput()
	{
		horizontal = 0f;
		vertical = 0;
		jump = false;
		attackDown = false;
		attackUp = false;
		attack = false;
		dashDown = false;
		dashUp = false;
		healDown = false;
		interact = false;
		skillA = false;
		skillB = false;
		action = false;
		heat = false;
		heatDown = false;
		overheat = false;
		guardDown = false;
		guardUp = false;
		specialParry = false;
		sightHorizontal = 0f;
		sightVertical = 0f;
		teleportDown = false;
		teleportUp = false;
	}

	public Vector2 GetSightOffset()
	{
		return new Vector2(sightHorizontal, sightVertical);
	}

	public async UniTask AdjustPlayerPositionAsync(Vector3 position, bool isRunning = false, bool silentY = true)
	{
		await MovePositionAsync(position, isRunning, silentY);
		if (!mover.IsFacingRight)
		{
			mover.Flip(Direction.Right);
			await UniTaskExtension.WaitForSeconds(0.3f);
		}
	}

	private void OnDrawGizmosSelected()
	{
		heatEffectController.DrawGizmos(this);
	}

	public void SetEndingCutsceneProducer(EndingCutsceneProducer endingCutscene)
	{
		endingCutsceneProducer = endingCutscene;
	}

	public void PlayWalkingEffectOnEnding(bool playFire = false)
	{
		if (IsEndingCutscene)
		{
			endingCutsceneProducer.PlayWalkingEffect(playFire);
		}
	}
}
