using FSM;

namespace SPR;

public class PlayerRunState : PlayerState
{
	private int frameCount;

	private const int StepCycle = 20;

	private bool isFirstStep;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerRunState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		if (prevState == psm.FallState)
		{
			SoundManager.PlayLandingEvent();
			SoundManager.PlayWalkingEvent();
		}
		PlayerMover.SetAnimation("run");
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		isFirstStep = true;
		frameCount = 0;
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	public override void FixedUpdate(float fdt)
	{
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
		PlayerMover.Flip(horizontal);
		if (!psm.TryChangeStateOnIdle())
		{
			if (psm.TryWalk())
			{
				psm.ChangeState(psm.WalkState);
			}
			else if (psm.TryIdle())
			{
				psm.ChangeState(psm.IdleState);
			}
			else if (psm.TryFall())
			{
				psm.ChangeState(psm.FallState);
			}
			else if ((isFirstStep && frameCount == 10) || (!isFirstStep && frameCount == 20))
			{
				Game.Instance.SoundManager.PlayWalkingEvent();
				frameCount = 0;
				isFirstStep = false;
			}
			else
			{
				frameCount++;
			}
		}
	}
}
