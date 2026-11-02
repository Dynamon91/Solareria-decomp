using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class PrimordialFlame2IdleState : BossIdleState
{
	private readonly PrimordialFlame2 primordialFlame2;

	private readonly float gravityAcceleration;

	private float gravityForce;

	public PrimordialFlame2IdleState(PrimordialFlame2 boss, float gravityAcceleration)
		: base(boss)
	{
		primordialFlame2 = boss;
		this.gravityAcceleration = gravityAcceleration;
	}

	public override void Enter(IState prevState)
	{
		base.Enter(prevState);
		if (!primordialFlame2.IsGround)
		{
			primordialFlame2.SetAnimation("jump_down_loop");
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (primordialFlame2.TryPhaseTransition())
		{
			return;
		}
		primordialFlame2.OnIdle();
		if (primordialFlame2.CanMove && primordialFlame2.IsGround)
		{
			if (Sequence.IsFirstFrame)
			{
				Sequence.Start(base.Enemy);
			}
			Sequence.FixedUpdate(primordialFlame2, fdt);
		}
		primordialFlame2.AddFatigue(0f - fdt);
		ApplyGravity(fdt);
		if (primordialFlame2.Fatigue == 0f && primordialFlame2.IsGround)
		{
			ChangeState(primordialFlame2.AttackState);
		}
	}

	private void ApplyGravity(float fdt)
	{
		if (primordialFlame2.IsGround)
		{
			gravityForce = 0f;
			return;
		}
		primordialFlame2.Position = Vector2.MoveTowards(primordialFlame2.Position, new Vector2(primordialFlame2.Position.x, primordialFlame2.SpawnPosition.y), gravityForce * fdt);
		gravityForce += gravityAcceleration * fdt;
	}
}
