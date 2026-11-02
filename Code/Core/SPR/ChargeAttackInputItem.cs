using UnityEngine;

namespace SPR;

public class ChargeAttackInputItem : InputItem
{
	private enum State
	{
		None,
		Charging,
		Success
	}

	private State chargeState;

	private float previousTime;

	private readonly float earlyInputAcceptTime;

	public float HoldDuration { get; private set; }

	private static InputManager InputManager => Game.Instance.InputManager;

	public bool IsSuccessCharging => chargeState == State.Success;

	public bool IsHolding => HoldDuration >= InputItem.pcp.ChargeStartTime;

	private bool IsKeyUp
	{
		get
		{
			if (!InputItem.PlayerController.attackUp)
			{
				return !InputItem.PlayerController.attack;
			}
			return true;
		}
	}

	public ChargeAttackInputItem(IState state)
		: base(state)
	{
		HoldDuration = -1f;
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.earlyChargeAcceptFrame);
		chargeState = State.None;
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.attackDown && HoldDuration < 0f && !InputItem.PlayerMover.PSM.GuardInputItem.HoldGuard)
		{
			previousTime = currentTime;
			HoldDuration = 0f;
		}
		else
		{
			if (HoldDuration < 0f)
			{
				return;
			}
			if (CheckCancel())
			{
				CancelCharge();
				return;
			}
			if (!InputItem.PlayerMover.IsOnGround)
			{
				chargeState = State.None;
				HoldDuration = 0f;
				return;
			}
			HoldDuration += (currentTime - previousTime) * Time.timeScale;
			previousTime = currentTime;
			if (!(HoldDuration < InputItem.pcp.ChargeStartTime) && chargeState == State.None)
			{
				chargeState = State.Success;
				base.InputTime = currentTime;
			}
		}
	}

	private bool CheckCancel()
	{
		if (!InputItem.PlayerMover.PSM.StateOption.HasFlag(StateOption.BlockHeavyCharge) && InputManager.IsCurrentControlTarget(InputItem.PlayerController) && !InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return IsKeyUp;
		}
		return true;
	}

	public void CancelCharge()
	{
		chargeState = State.None;
		HoldDuration = -1f;
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (!InputItem.PlayerMover.IsOnGround)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyInputAcceptTime)
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
