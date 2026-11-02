using FSM;

namespace SPR;

public class PlayerEntranceJumpState : PlayerState
{
	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerEntranceJumpState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
		PlayerMover.SetAnimation("jump_down_loop");
	}

	public override void Exit(IState nextState)
	{
		if (nextState is PlayerIdleState || nextState is PlayerWalkState || nextState is PlayerRunState)
		{
			SoundManager.PlayLandingEvent();
		}
	}

	public override void FixedUpdate(float fdt)
	{
		if (psm.TryIdle())
		{
			psm.ChangeState(psm.IdleState);
		}
		else if (psm.TryWalk())
		{
			psm.ChangeState(psm.WalkState);
		}
		else if (psm.TryRun())
		{
			psm.ChangeState(psm.RunState);
		}
	}
}
