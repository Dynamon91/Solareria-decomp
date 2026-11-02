using SPR.FSM.Base;

namespace SPR;

public class PrimordialFlame2ParryState : BossState
{
	private const int MaxIndex = 3;

	private readonly PrimordialFlame2 primordialFlame;

	private string animation;

	private int index;

	public PrimordialFlame2ParryState(PrimordialFlame2 enemy)
		: base(enemy)
	{
		primordialFlame = enemy;
	}

	public override void Enter(IState prevState)
	{
		if (index >= 3)
		{
			index = 0;
		}
		animation = $"parrying_{index + 1}";
		index++;
		base.Boss.SetAnimationInstantly(animation, loop: false);
		base.Boss.EnableVFX(animation);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (primordialFlame.Fatigue <= 0f)
		{
			ChangeState(base.Boss.DefaultState);
		}
		else
		{
			primordialFlame.AddFatigue(0f - fdt);
		}
	}
}
