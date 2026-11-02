using FSM;

namespace SPR;

public class PlayerWalkState : PlayerState
{
	private int frameCount;

	private const int StepCycle = 40;

	private const int StepEffectFrame = 15;

	private bool isWet;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerWalkState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		if (prevState == psm.FallState)
		{
			SoundManager.PlayWalkingEvent();
		}
		isWet = PlayerState.PlayerController.ForceWalk;
		string animationName = (PlayerState.PlayerController.IsEndingCutscene ? "walk_looking_up" : (isWet ? "damaged_walk" : "walk"));
		PlayerMover.SetAnimation(animationName);
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		frameCount = 0;
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	public override void FixedUpdate(float fdt)
	{
		if (isWet && !PlayerState.PlayerController.ForceWalk)
		{
			PlayerMover.SetAnimation("walk");
		}
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
		PlayerMover.Flip(horizontal);
		if (psm.TryChangeStateOnIdle())
		{
			return;
		}
		if (psm.TryRun())
		{
			psm.ChangeState(psm.RunState);
			return;
		}
		if (psm.TryIdle())
		{
			psm.ChangeState(psm.IdleState);
			return;
		}
		if (psm.TryFall())
		{
			psm.ChangeState(psm.FallState);
			return;
		}
		if (frameCount == 15)
		{
			Game.Instance.SoundManager.PlayWalkingEvent();
			PlayerState.PlayerController.PlayWalkingEffectOnEnding(playFire: true);
		}
		if (frameCount == 40)
		{
			frameCount = 0;
		}
		frameCount++;
	}
}
