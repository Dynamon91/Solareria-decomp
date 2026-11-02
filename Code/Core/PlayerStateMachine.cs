using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FSM;
using SPR;
using SPR.Save;
using UnityEngine;

public class PlayerStateMachine : IStateMachine
{
	public readonly PlayerMover Mover;

	public readonly IState IdleState;

	public readonly IState WalkState;

	public readonly IState RunState;

	public readonly IState JumpUpState;

	public readonly IState DoubleJumpUpState;

	public readonly IState FallState;

	public readonly IState ParkourState;

	public readonly IState LandingHardState;

	public readonly IState BasicGroundAttackState;

	public readonly IState HitState;

	public readonly IState DieState;

	public readonly IState RespawnState;

	public readonly IState FirstAirAttackState;

	public readonly IState SecondAirAttackState;

	public readonly IState HealingState;

	public readonly IState JumpDownState;

	public readonly IState DownState;

	public readonly IState SaveState;

	public readonly IState StiffState;

	public readonly IState AshBreakState;

	public readonly IState GuardState;

	public readonly IState ParryState;

	public readonly IState BrokenGuardState;

	public readonly IState AttackStuckState;

	public readonly IState BurstState;

	public readonly IState KnockBackState;

	public readonly IState AshBlastState;

	public readonly IState StaggerHitState;

	public readonly IState PyronAttackState;

	public readonly IState HyperPyronAttackState;

	public readonly IState MoveSightState;

	public readonly IState OverheatState;

	public readonly IState HyperBurstState;

	public readonly PlayerSpecialParryState SpecialParryState;

	public readonly PlayerChargeAttackState ChargeAttackState;

	public readonly PlayerRollingState RollingState;

	public readonly PlayerJumpDashState JumpDashState;

	public readonly PlayerFastDownState FastDownState;

	public readonly IState CutsceneState;

	public readonly IState TeleportState;

	public readonly IState IdleRestState;

	public readonly JumpUpInputItem JumpUpInputItem;

	public readonly DoubleJumpUpInputItem DoubleJumpUpInputItem;

	public readonly JumpDownInputItem JumpDownInputItem;

	public readonly RollingInputItem RollingInputItem;

	public readonly PyronRollingInputItem PyronRollingInputItem;

	public readonly HealInputItem HealInputItem;

	public readonly BasicAttackInputItem BasicAttackInputItem;

	public readonly FirstAirAttackInputItem FirstAirAttackInputItem;

	public readonly SecondAirAttackInputItem SecondAirAttackInputItem;

	public readonly JumpDashInputItem JumpDashInputItem;

	public readonly ChargeAttackInputItem ChargeAttackInputItem;

	public readonly AshBreakInputItem AshBreakInputItem;

	public readonly GuardInputItem GuardInputItem;

	public readonly ParryInputItem ParryInputItem;

	public readonly BurstInputItem BurstInputItem;

	public readonly AshBlastInputItem AshBlastInputItem;

	public readonly DownRollingInputItem DownRollingInputItem;

	public readonly ParryAttackInputItem ParryAttackInputItem;

	public readonly HyperPyronAttackInputItem HyperPyronAttackInputItem;

	public readonly FastDownInputItem FastDownInputItem;

	public readonly OverheatInputItem OverheatInputItem;

	public readonly SpecialParryInputItem SpecialParryInputItem;

	public readonly HyperBurstInputItem HyperBurstInputItem;

	public readonly TeleportInputItem TeleportInputItem;

	private readonly List<InputItem> inputItems;

	private readonly List<InputItem> idleNextInputItems;

	private PlayerController playerController;

	private readonly List<InputItem> executableInputItems = new List<InputItem>();

	private Action onEndIgnoreDamage;

	public IState PrevState { get; private set; }

	public IState CurrentState { get; private set; }

	public StateOption StateOption { get; private set; }

	public bool CanControl => StateOption.HasFlag(StateOption.Controllable);

	public IReadOnlyList<InputItem> HeatEffectInputItems => inputItems.Where((InputItem i) => i is IHeatEffect).ToList();

	private PlayerController PlayerController => playerController ?? (playerController = Mover.GetComponent<PlayerController>());

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public int IgnoreDamageFrame { get; private set; }

	public int ParryLockFrame { get; private set; } = -1;

	public int ParryAcceptFrame { get; private set; } = -1;

	public bool CanInputParry => ParryLockFrame < 0;

	public PlayerStateMachine(PlayerMover mover)
	{
		Mover = mover;
		IdleState = new PlayerIdleState(this);
		WalkState = new PlayerWalkState(this);
		RunState = new PlayerRunState(this);
		JumpUpState = new PlayerJumpUpState(this);
		DoubleJumpUpState = new PlayerDoubleJumpUpState(this);
		FallState = new PlayerFallState(this);
		ParkourState = new PlayerParkourState(this);
		LandingHardState = new PlayerLandingHardState(this);
		BasicGroundAttackState = new PlayerGroundAttackState(this);
		RollingState = new PlayerRollingState(this);
		HitState = new PlayerHitState(this);
		DieState = new PlayerDieState(this);
		RespawnState = new PlayerRespawnState(this);
		FirstAirAttackState = new PlayerFirstAirAttackState(this);
		SecondAirAttackState = new PlayerSecondAirAttackState(this);
		JumpDashState = new PlayerJumpDashState(this);
		HealingState = new PlayerHealState(this);
		JumpDownState = new PlayerJumpDownState(this);
		DownState = new PlayerDownState(this);
		SaveState = new PlayerSaveState(this);
		StiffState = new PlayerStiffState(this);
		AshBreakState = new PlayerAshBreakState(this);
		GuardState = new PlayerGuardState(this);
		ParryState = new PlayerParryState(this);
		BrokenGuardState = new PlayerBrokenGuardState(this);
		AttackStuckState = new PlayerAttackStuckState(this);
		BurstState = new PlayerBurstState(this, new BurstAttackTask());
		KnockBackState = new PlayerKnockBackState(this);
		AshBlastState = new PlayerAshBlastState(this);
		StaggerHitState = new PlayerStaggerHitState(this);
		PyronAttackState = new PlayerParryActionState(this, new PyronAttackTask());
		HyperPyronAttackState = new PlayerParryActionState(this, new HyperPyronAttackTask());
		FastDownState = new PlayerFastDownState(this);
		ChargeAttackState = new PlayerChargeAttackState(this);
		MoveSightState = new PlayerMoveSightState(this);
		OverheatState = new PlayerOverheatState(this);
		SpecialParryState = new PlayerSpecialParryState(this);
		HyperBurstState = new PlayerBurstState(this, new HyperBurstAttackTask());
		CutsceneState = new PlayerCutsceneState(this);
		TeleportState = new PlayerTeleportState(this);
		IdleRestState = new PlayerIdleRestState(this);
		JumpUpInputItem = new JumpUpInputItem(JumpUpState);
		DoubleJumpUpInputItem = new DoubleJumpUpInputItem(DoubleJumpUpState);
		JumpDownInputItem = new JumpDownInputItem(JumpDownState);
		BasicAttackInputItem = new BasicAttackInputItem(BasicGroundAttackState);
		RollingInputItem = new RollingInputItem(RollingState);
		HealInputItem = new HealInputItem(HealingState);
		JumpDashInputItem = new JumpDashInputItem(JumpDashState);
		FirstAirAttackInputItem = new FirstAirAttackInputItem(FirstAirAttackState);
		SecondAirAttackInputItem = new SecondAirAttackInputItem(SecondAirAttackState);
		ChargeAttackInputItem = new ChargeAttackInputItem(ChargeAttackState);
		AshBreakInputItem = new AshBreakInputItem(AshBreakState);
		GuardInputItem = new GuardInputItem(GuardState);
		ParryInputItem = new ParryInputItem(ParryState);
		BurstInputItem = new BurstInputItem(BurstState);
		AshBlastInputItem = new AshBlastInputItem(AshBlastState);
		DownRollingInputItem = new DownRollingInputItem(RollingState);
		FastDownInputItem = new FastDownInputItem(FastDownState);
		ParryAttackInputItem = new ParryAttackInputItem(PyronAttackState);
		HyperPyronAttackInputItem = new HyperPyronAttackInputItem(HyperPyronAttackState);
		OverheatInputItem = new OverheatInputItem(OverheatState);
		SpecialParryInputItem = new SpecialParryInputItem(SpecialParryState);
		HyperBurstInputItem = new HyperBurstInputItem(HyperBurstState);
		TeleportInputItem = new TeleportInputItem(TeleportState);
		PyronRollingInputItem = new PyronRollingInputItem(RollingState);
		inputItems = new List<InputItem>
		{
			JumpUpInputItem, DoubleJumpUpInputItem, JumpDownInputItem, HyperBurstInputItem, HyperPyronAttackInputItem, SpecialParryInputItem, OverheatInputItem, BurstInputItem, ParryAttackInputItem, FirstAirAttackInputItem,
			SecondAirAttackInputItem, BasicAttackInputItem, RollingInputItem, HealInputItem, JumpDashInputItem, ChargeAttackInputItem, AshBreakInputItem, GuardInputItem, ParryInputItem, AshBlastInputItem,
			DownRollingInputItem, FastDownInputItem, TeleportInputItem, PyronRollingInputItem
		};
		idleNextInputItems = new List<InputItem>
		{
			JumpDownInputItem, JumpUpInputItem, HyperBurstInputItem, SpecialParryInputItem, OverheatInputItem, BurstInputItem, BasicAttackInputItem, GuardInputItem, RollingInputItem, ChargeAttackInputItem,
			HealInputItem, AshBreakInputItem, AshBlastInputItem, TeleportInputItem
		};
		InitializeInputItems();
		StateOption = StateOption.None;
		PrevState = IdleState;
		ChangeState(IdleState);
	}

	public void Initialize()
	{
		InitializeInputItems();
		ChangeState(IdleState);
	}

	private void InitializeInputItems()
	{
		foreach (InputItem item in inputItems)
		{
			PlayerData.SkillData skillData = PlayerData.SkillDataList.FirstOrDefault((PlayerData.SkillData data) => data.typeName == item.GetType().Name);
			if (skillData != null)
			{
				item.Active = skillData.active;
			}
		}
	}

	public void FixedUpdate(float fdt)
	{
		if (IgnoreDamageFrame > 0)
		{
			IgnoreDamageFrame--;
			if (IgnoreDamageFrame == 0)
			{
				SetStateOptionActive(StateOption.SuperArmor | StateOption.IgnoreDamage, active: false);
				onEndIgnoreDamage?.Invoke();
				onEndIgnoreDamage = null;
			}
		}
		if (ParryAcceptFrame >= 0)
		{
			ParryAcceptFrame++;
		}
		if (ParryLockFrame >= 0)
		{
			if (ParryLockFrame < pcp.parryLockFrame)
			{
				ParryLockFrame++;
			}
			else
			{
				ResetParryLock();
			}
		}
		CurrentState?.FixedUpdate(fdt);
	}

	public void ResetParryLock(bool onlyParryLock = false)
	{
		SetParryLockFrame(-1);
		if (!onlyParryLock)
		{
			ParryAcceptFrame = -1;
		}
	}

	public void StartParryLock()
	{
		if (CanInputParry)
		{
			ParryAcceptFrame = 0;
		}
		SetParryLockFrame(0);
	}

	public void ChangeState(IState newState)
	{
		PrevState = CurrentState;
		CurrentState?.Exit(newState);
		CurrentState = newState;
		CurrentState?.Enter(PrevState);
	}

	public void UpdateInput()
	{
		float time = Time.time;
		foreach (InputItem inputItem in inputItems)
		{
			if (inputItem.Active)
			{
				inputItem.UpdateInput(time);
			}
		}
	}

	public void CancelInput()
	{
		ChargeAttackInputItem.CancelCharge();
		TeleportInputItem.CancelCharge();
	}

	public void GroundEvent()
	{
		foreach (InputItem inputItem in inputItems)
		{
			if (inputItem is IGroundInput groundInput)
			{
				groundInput.ResetTime();
			}
		}
	}

	public int MoveDirectionInt(bool includeFacing = false)
	{
		float horizontal = PlayerController.horizontal;
		if (horizontal == 0f && includeFacing)
		{
			if (!Mover.IsFacingRight)
			{
				return -1;
			}
			return 1;
		}
		if (!(horizontal > 0f))
		{
			if (!(horizontal < 0f))
			{
				return 0;
			}
			return -1;
		}
		return 1;
	}

	public bool TryIdle()
	{
		if (!Mover.IsOnGround)
		{
			return false;
		}
		if (Mathf.Abs(Mover.RelativeVelocity.x) >= 0.01f)
		{
			return false;
		}
		return true;
	}

	public bool TryRun()
	{
		if (!Mover.IsOnGround)
		{
			return false;
		}
		if (Mathf.Abs(Mover.RelativeVelocity.x) < 0.01f)
		{
			return false;
		}
		if (Mathf.Abs(PlayerController.horizontal) < pcp.RunHorizontalInputThreshold)
		{
			return false;
		}
		return true;
	}

	public bool TryWalk()
	{
		if (!Mover.IsOnGround)
		{
			return false;
		}
		if (Mathf.Abs(Mover.RelativeVelocity.x) < 0.01f)
		{
			return false;
		}
		if (Mathf.Abs(PlayerController.horizontal) >= pcp.RunHorizontalInputThreshold)
		{
			return false;
		}
		return true;
	}

	public bool TryFall()
	{
		if (Mover.RelativeVelocity.y > -0.01f)
		{
			return false;
		}
		if (Mover.IsOnGround)
		{
			return false;
		}
		if (Mover.IsRiding)
		{
			return false;
		}
		return true;
	}

	public bool TryParkour(out Vector2 edgePos)
	{
		if (!InputManager.IsCurrentControlTarget(PlayerController))
		{
			edgePos = Vector2.zero;
			return false;
		}
		if (Mover.RelativeVelocity.y <= -0.01f && !Mover.IsOnGround && Mover.TryGetParkourEdgePos(out var edgePos2))
		{
			edgePos = edgePos2;
			return true;
		}
		edgePos = Vector2.zero;
		return false;
	}

	public bool TryLandingHard()
	{
		if (Mover.RelativeVelocity.y > (float)(-pcp.LandingHardSpeedThreshold))
		{
			return false;
		}
		if (!Mover.IsOnGround)
		{
			return false;
		}
		return true;
	}

	public InputItem GetInputItem(Type inputItemType)
	{
		return inputItems.FirstOrDefault((InputItem i) => i.GetType() == inputItemType);
	}

	public InputItem GetInputItem(IState state)
	{
		return inputItems.FirstOrDefault((InputItem i) => i.TargetState == state);
	}

	public void SetStateOptionActive(StateOption option, bool active)
	{
		if (active)
		{
			StateOption |= option;
			return;
		}
		if ((option & (StateOption.SuperArmor | StateOption.IgnoreDamage)) != StateOption.None && IgnoreDamageFrame > 0)
		{
			option &= ~(StateOption.SuperArmor | StateOption.IgnoreDamage);
		}
		StateOption &= ~option;
	}

	public void SetIgnoreDamage(int durationFrame, Action onEnd = null)
	{
		if (IgnoreDamageFrame < durationFrame)
		{
			IgnoreDamageFrame = durationFrame;
			StateOption |= StateOption.SuperArmor | StateOption.IgnoreDamage;
			if (onEnd != null)
			{
				onEndIgnoreDamage = (Action)Delegate.Combine(onEndIgnoreDamage, onEnd);
			}
		}
	}

	public bool TryChangeStateOnIdle()
	{
		return TryChangeSkillState(idleNextInputItems);
	}

	public bool TryChangeSkillState(IEnumerable<InputItem> inputItems)
	{
		float time = Time.time;
		executableInputItems.Clear();
		foreach (InputItem inputItem3 in inputItems)
		{
			if (inputItem3.CanExecute(time))
			{
				executableInputItems.Add(inputItem3);
			}
		}
		if (executableInputItems.Count == 0)
		{
			return false;
		}
		InputItem inputItem = executableInputItems[0];
		for (int i = 1; i < executableInputItems.Count; i++)
		{
			InputItem inputItem2 = executableInputItems[i];
			if (inputItem2.InputTime > inputItem.InputTime)
			{
				inputItem = inputItem2;
			}
		}
		inputItem.Execute();
		return true;
	}

	public void ActivateParryAction(Enemy targetEnemy)
	{
		ParryAttackInputItem.AddEnemy(targetEnemy);
		HyperPyronAttackInputItem.AddEnemy(targetEnemy);
		if (ParryAttackInputItem.Active)
		{
			return;
		}
		List<InputItem> activeInputItems = inputItems.Where((InputItem i) => i.Active && i != ParryInputItem && i != GuardInputItem).ToList();
		activeInputItems.ForEach(delegate(InputItem i)
		{
			i.Active = false;
		});
		UniTask parryActionTask = Game.Instance.GraphicManager.ParryActionDirector.PlayTimelineAsync(PlayerController.mover.VisualCenter, delegate
		{
			ParryAttackInputItem.Active = false;
			HyperPyronAttackInputItem.Active = false;
			activeInputItems.ForEach(delegate(InputItem i)
			{
				i.Active = true;
			});
		});
		TryRolling(parryActionTask);
		ParryAttackInputItem.Active = true;
		if (PlayerHeat.CanActivateSkill(HeatSkillType.HyperPyronAction))
		{
			HyperPyronAttackInputItem.Active = true;
		}
		Game.Instance.RuntimeData.AddCanHyperPyronAction();
		async void TryRolling(UniTask uniTask)
		{
			PyronRollingInputItem.Active = true;
			while (uniTask.Status == UniTaskStatus.Pending)
			{
				if (PyronRollingInputItem.CanExecute(Time.time))
				{
					PyronRollingInputItem.Execute();
					break;
				}
				await UniTask.WaitForSeconds(Time.fixedDeltaTime, ignoreTimeScale: true, PlayerLoopTiming.LastFixedUpdate);
			}
		}
	}

	public void SetParryLockFrame(int frame)
	{
		ParryLockFrame = frame;
	}
}
