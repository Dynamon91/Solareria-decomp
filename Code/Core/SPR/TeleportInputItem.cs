using SPR.UI.Field;
using UnityEngine;

namespace SPR;

public class TeleportInputItem : InputItem
{
	private enum State
	{
		None,
		Charging,
		Success
	}

	private State chargeState;

	private float previousTime;

	public float HoldDuration { get; private set; }

	public bool IsSuccessCharging { private get; set; }

	private static InputManager InputManager => Game.Instance.InputManager;

	private static TeleportBoard TeleportBoard => Game.Instance.UIManager.FieldCanvas.TeleportBoard;

	public bool IsHolding => HoldDuration >= 0f;

	private bool IsKeyUp => InputItem.PlayerController.teleportUp;

	private bool IsIdleStates
	{
		get
		{
			IState currentState = InputItem.PlayerMover.PSM.CurrentState;
			return currentState is PlayerIdleState || currentState is PlayerWalkState || currentState is PlayerRunState;
		}
	}

	private float ChargePredelay => PlayerControlParameter.Instance.teleportPredelay;

	private static float TeleportChargeDuration => 3f;

	public TeleportInputItem(IState state)
		: base(state)
	{
		HoldDuration = -1f;
		chargeState = State.None;
	}

	public override void UpdateInput(float currentTime)
	{
		if (CanStart())
		{
			previousTime = currentTime;
			HoldDuration = 0f;
		}
		else
		{
			if (!IsHolding)
			{
				return;
			}
			if (CheckCancel(out var idleState))
			{
				CancelCharge(idleState);
				return;
			}
			HoldDuration += (currentTime - previousTime) * Time.timeScale;
			previousTime = currentTime;
			if (HoldDuration < ChargePredelay)
			{
				return;
			}
			if (HoldDuration < ChargePredelay + TeleportChargeDuration)
			{
				if (chargeState == State.Charging)
				{
					return;
				}
				if (chargeState == State.None)
				{
					chargeState = State.Charging;
					Execute();
				}
			}
			if (chargeState == State.Charging)
			{
				chargeState = State.Success;
				base.InputTime = currentTime;
			}
		}
	}

	private bool CanStart()
	{
		if (TeleportBoard.CanOpen && InputItem.PlayerController.teleportDown && IsIdleStates && !IsHolding && InputItem.PlayerMover.IsOnGround && !InputItem.PlayerMover.PSM.GuardInputItem.HoldGuard)
		{
			return !InputItem.PlayerMover.PSM.ChargeAttackInputItem.IsHolding;
		}
		return false;
	}

	private bool CheckCancel(out bool idleState)
	{
		idleState = false;
		if (IsKeyUp)
		{
			idleState = true;
			return true;
		}
		if (!InputManager.IsCurrentControlTarget(InputItem.PlayerController))
		{
			idleState = !IsSuccessCharging;
			return true;
		}
		if (!IsIdleStates && !(InputItem.PlayerMover.PSM.CurrentState is PlayerTeleportState))
		{
			return true;
		}
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return true;
		}
		return false;
	}

	public void CancelCharge(bool idleState = true)
	{
		if (idleState && !IsSuccessCharging && chargeState != State.None)
		{
			InputItem.PlayerMover.PSM.ChangeState(InputItem.PlayerMover.PSM.IdleState);
		}
		chargeState = State.None;
		HoldDuration = -1f;
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (!TeleportBoard.CanOpen)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (!InputItem.PlayerMover.IsOnGround)
		{
			return false;
		}
		if (base.LastSuccessTime >= base.InputTime)
		{
			return false;
		}
		return true;
	}

	protected override void OnActiveChanged()
	{
		if (!base.Active)
		{
			CancelCharge();
		}
	}
}
