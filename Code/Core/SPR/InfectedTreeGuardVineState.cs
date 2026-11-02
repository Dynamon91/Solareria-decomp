using Library;
using SPR.FSM;

namespace SPR;

public class InfectedTreeGuardVineState : EnemyState
{
	private readonly AttackSequence attackSequence;

	private readonly InfectedTreeGuardVine vine;

	public InfectedTreeGuardVineState(Enemy enemy, AttackSequence attackSequence)
		: base(enemy)
	{
		vine = enemy.CastObject<InfectedTreeGuardVine>();
		this.attackSequence = attackSequence;
	}

	public override void Enter(IState prevState)
	{
		attackSequence.ResetFrame(vine);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (attackSequence.IsFirstFrame)
		{
			attackSequence.Start(vine);
		}
		if (attackSequence.IsDone)
		{
			vine.ReturnToPool();
		}
		attackSequence.FixedUpdate(vine, fdt);
	}
}
