using Library;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class FlyCreaturePatrolState : PatrolState
{
	private readonly FlyingCreature flyingCreature;

	public FlyCreaturePatrolState(Creature creature, int minFrame, int maxFrame)
		: base(creature, minFrame, maxFrame)
	{
		flyingCreature = creature.CastObject<FlyingCreature>();
	}

	public override void Enter(IState prevState)
	{
		flyingCreature.SetAnimation(flyingCreature.MoveAnimation);
		ShouldWait = CryptoRandom.RandomBool();
		Timer = new RandomFrameTimer(base.Min, base.Max);
		if (!ShouldWait)
		{
			TargetPosition = flyingCreature.GetPatrolDestination();
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (flyingCreature.IsOverlappedWall(TargetPosition))
		{
			flyingCreature.MoveAirTo(flyingCreature.MoveForwardSpeed, TargetPosition, moveForward: false);
			ChangeState(flyingCreature.PatrolState);
		}
		else if (base.Creature.IsPlayerInSightBound())
		{
			flyingCreature.OnEncounterPlayer();
		}
		else if (ShouldWait)
		{
			flyingCreature.SetAnimation(flyingCreature.IdleAnim);
			Timer.Tick();
			if (Timer.IsDone)
			{
				ChangeState(flyingCreature.PatrolState);
			}
		}
		else if (flyingCreature.MoveAirTo(flyingCreature.MoveForwardSpeed, TargetPosition, moveForward: true))
		{
			ChangeState(flyingCreature.PatrolState);
		}
	}
}
