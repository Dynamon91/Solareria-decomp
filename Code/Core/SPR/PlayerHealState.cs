using FSM;
using SPR.UI.Field;
using UnityEngine;

namespace SPR;

public class PlayerHealState : PlayerState
{
	private bool isWalking;

	private int frameCount;

	private float elapsedTime;

	private readonly float animDuration;

	public const int HealFrame = 40;

	private const int EffectStartFrame = 10;

	private const string StandAnimName = "heal_stand";

	private const string WalkAnimName = "heal_walk";

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static PotionBoard PotionBoard => Game.Instance.UIManager.FieldCanvas.PotionBoard;

	private static PlayerPart PlayerPart => PlayerState.PlayerController.PlayerPart;

	public bool IsHealed { get; private set; }

	public PlayerHealState(PlayerStateMachine psm)
		: base(psm)
	{
		animDuration = psm.Mover.GetAnimDuration("heal_stand");
	}

	public override void Enter(IState prevState)
	{
		frameCount = 0;
		elapsedTime = 0f;
		isWalking = false;
		IsHealed = false;
		PlayerMover.SetAnimation("heal_stand", loop: false);
		PlayerMover.EnableVFX("heal_start");
		SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Heal_Start");
		PlayerPart.OnStartHeal();
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: true);
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	public override void FixedUpdate(float fdt)
	{
		frameCount++;
		elapsedTime += fdt;
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal * 0.2f);
		PlayerMover.Flip(horizontal);
		ChangeAnimation(horizontal != 0f);
		if (psm.RollingInputItem.CanExecute(Time.fixedTime))
		{
			psm.RollingInputItem.Execute();
			return;
		}
		if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
			return;
		}
		if (frameCount == 10)
		{
			PotionBoard.StartAnimation();
		}
		if (frameCount == 40)
		{
			PlayerState.PlayerStat.GetStat<WarmhornCount>().UsePotion(1);
			PlayerPart.OnHeal();
			PlayerMover.EnableVFX("heal_end");
			psm.SetStateOptionActive(StateOption.Controllable, active: true);
			SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Heal_Effect");
			SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Heal_End");
			IsHealed = true;
		}
		if (frameCount >= 40 && psm.GuardInputItem.CanExecute(Time.time))
		{
			psm.GuardInputItem.Execute();
		}
		else if (elapsedTime >= animDuration)
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
			else if (psm.TryFall())
			{
				psm.ChangeState(psm.FallState);
			}
		}
	}

	public override void Exit(IState nextState)
	{
		IsHealed = false;
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockHeavyCharge, active: false);
		PotionBoard.CancelAnimation();
		PlayerPart.OnEndHeal();
		PlayerMover.DisableVFX("heal_start");
	}

	private void ChangeAnimation(bool walking)
	{
		if (isWalking != walking)
		{
			isWalking = walking;
			string animationName = (walking ? "heal_walk" : "heal_stand");
			PlayerMover.SetAnimation(animationName, loop: false, 1f, 0, elapsedTime);
		}
	}
}
