using Library;
using SPR.FSM;

namespace SPR;

public class MoomooVineAttackState : EnemyState
{
	private readonly AttackSequence attackSequence;

	private readonly MoomooVine vine;

	public MoomooVineAttackState(Enemy enemy, AttackSequence attackSequence)
		: base(enemy)
	{
		vine = enemy.CastObject<MoomooVine>();
		this.attackSequence = attackSequence;
	}

	public override void Enter(IState prevState)
	{
		vine.SetAnimationGroup("idle");
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
			vine.SetRandomAnimationGroup();
		}
		if (attackSequence.IsDone)
		{
			vine.StateMachine.ChangeState(vine.DefaultState);
		}
		attackSequence.FixedUpdate(vine, fdt);
	}
}
