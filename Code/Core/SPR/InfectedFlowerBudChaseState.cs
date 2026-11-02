using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class InfectedFlowerBudChaseState : ChaseState
{
	private readonly InfectedFlowerBud infectedFlowerBud;

	private FrameTimer waitTimer;

	private bool keepChase;

	public InfectedFlowerBudChaseState(Creature enemy)
		: base(enemy)
	{
		infectedFlowerBud = enemy.CastObject<InfectedFlowerBud>();
	}

	public override void Enter(IState prevState)
	{
		waitTimer = new FrameTimer(0);
		keepChase = false;
		infectedFlowerBud.SetAnimation("walk");
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (waitTimer.IsRunning)
		{
			infectedFlowerBud.SetAnimation("idle");
			waitTimer.Tick();
			if (waitTimer.IsDone)
			{
				infectedFlowerBud.SetAnimation("walk");
				SetPath(infectedFlowerBud.VisualBottom, infectedFlowerBud.GetRandomDestination());
			}
		}
		if (waitTimer.IsDone && Path.Count == 0)
		{
			if (keepChase)
			{
				if (CryptoRandom.RandomBool())
				{
					ChangeState(base.Creature.AttackState);
				}
				else
				{
					ChangeState(base.Creature.AlertState);
				}
				return;
			}
			if (CryptoRandom.RandomBool())
			{
				waitTimer = new FrameTimer(infectedFlowerBud.GetRandomWaitFrame());
			}
			else
			{
				SetPath(infectedFlowerBud.VisualBottom, infectedFlowerBud.GetRandomDestination());
			}
		}
		if (!keepChase && base.Creature.ShouldChangeToAttackState())
		{
			if (CryptoRandom.RandomBool())
			{
				ChangeState(base.Creature.AttackState);
				return;
			}
			keepChase = true;
		}
		if (!base.Creature.IsPlayerInSightBound() || !base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()))
		{
			ChangeState(base.Creature.PatrolState);
		}
		else
		{
			Move();
		}
	}
}
