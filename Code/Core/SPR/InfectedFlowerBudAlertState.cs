using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class InfectedFlowerBudAlertState : AlertState
{
	public InfectedFlowerBudAlertState(Creature creature, int minFrame, int maxFrame)
		: base(creature, minFrame, maxFrame)
	{
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SetAnimation("idle");
		Timer = new RandomFrameTimer(base.Min, base.Max);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (Timer.IsDone)
		{
			ChangeState(base.Creature.PatrolState);
		}
		Timer.Tick();
	}
}
