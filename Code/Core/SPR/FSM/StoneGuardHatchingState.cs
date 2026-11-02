using SPR.FSM.Base;

namespace SPR.FSM;

public class StoneGuardHatchingState : CreatureState
{
	private readonly Creature stoneGuard;

	public StoneGuardHatchingState(Creature enemy)
		: base(enemy)
	{
		stoneGuard = enemy;
	}

	public override void Enter(IState prevState)
	{
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		stoneGuard.SetAnimation("stone_end", loop: false);
		if (stoneGuard.IsAnimComplete(0))
		{
			ChangeState(base.Creature.PatrolState);
		}
	}
}
