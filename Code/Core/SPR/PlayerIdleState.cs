using FSM;
using SPR.Cutscene;

namespace SPR;

public class PlayerIdleState : PlayerState
{
	private IState preState;

	private bool isWet;

	private int frameCount;

	private const string WetIdle = "damaged_loop";

	private const string Idle = "idle";

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerIdleState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		isWet = PlayerState.PlayerController.ForceWalk;
		string animationName = (isWet ? "damaged_loop" : "idle");
		frameCount = 0;
		if (PlayerState.PlayerController.IsEndingCutscene)
		{
			PlayerMover.SetAnimation("cam_up");
		}
		else if (prevState == psm.FallState)
		{
			SoundManager.PlayLandingEvent();
			PlayerMover.SetAnimation("jump_end", loop: false);
			PlayerMover.AddAnimation(animationName);
		}
		else if (prevState == psm.RunState)
		{
			PlayerMover.SetAnimation("run_stop", loop: false);
			PlayerMover.AddAnimation(animationName);
		}
		else if (prevState == psm.FastDownState)
		{
			PlayerMover.AddAnimation(animationName);
		}
		else
		{
			PlayerMover.SetAnimation(animationName);
		}
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	public override void FixedUpdate(float fdt)
	{
		if (isWet && !PlayerState.PlayerController.ForceWalk)
		{
			PlayerMover.SetAnimation("idle");
		}
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
		if (!psm.TryChangeStateOnIdle())
		{
			if (psm.TryRun())
			{
				psm.ChangeState(psm.RunState);
			}
			else if (psm.TryWalk())
			{
				psm.ChangeState(psm.WalkState);
			}
			else if (psm.TryFall())
			{
				psm.ChangeState(psm.FallState);
			}
			else if (PlayerState.PlayerController.GetSightOffset().magnitude >= PlayerControlParameter.Instance.MoveSightInputThreshold)
			{
				psm.ChangeState(psm.MoveSightState);
			}
			else if (TryRest())
			{
				psm.ChangeState(psm.IdleRestState);
			}
		}
	}

	private bool TryRest()
	{
		foreach (CutsceneProducer cutsceneProducer in Game.Instance.FieldController.GetCutsceneProducers())
		{
			if (cutsceneProducer is BossCutsceneProducer bossCutsceneProducer)
			{
				if (bossCutsceneProducer.IsFighting)
				{
					goto IL_0044;
				}
			}
			else if (cutsceneProducer is ChasaCutsceneProducer { IsFighting: not false })
			{
				goto IL_0044;
			}
			bool flag = false;
			goto IL_004c;
			IL_0044:
			flag = true;
			goto IL_004c;
			IL_004c:
			if (flag)
			{
				return false;
			}
		}
		frameCount++;
		if (!Game.Instance.InputManager.IsCurrentControlTarget(PlayerState.PlayerController))
		{
			frameCount = 0;
			return false;
		}
		if (PlayerState.PlayerController.vertical != 0)
		{
			frameCount = 0;
			return false;
		}
		return frameCount >= PlayerControlParameter.Instance.idleRestFrame;
	}
}
