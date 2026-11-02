using FSM;
using UnityEngine;

namespace SPR;

public class PlayerLandingHardState : PlayerState
{
	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerLandingHardState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		PlayerMover.HorizontalMove(0f);
		PlayerMover.SetAnimation("landing", loop: false);
		SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Landing_Hard", forcedOnSceneLoad: true);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (!PlayerMover.IsOnGround)
		{
			psm.ChangeState(psm.FallState);
		}
		else if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
		}
		else if (PlayerMover.IsAnimComplete(0))
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
}
