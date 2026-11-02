using System;
using FSM;

namespace SPR;

public class PlayerStiffState : PlayerState
{
	public Func<bool> ClearCondition { get; set; }

	public PlayerStiffState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		PlayerMover.SetAnimation("idle");
		PlayerMover.ResetRelativeVelocity();
	}

	public override void Exit(IState nextState)
	{
		ClearCondition = null;
	}

	public override void FixedUpdate(float fdt)
	{
		bool? flag = ClearCondition?.Invoke();
		if (flag.HasValue && flag.Value)
		{
			psm.ChangeState(psm.IdleState);
		}
	}
}
