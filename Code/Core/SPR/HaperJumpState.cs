using Library;
using SPR.FSM.Base;

namespace SPR;

public class HaperJumpState : CreatureState
{
	private readonly Haper haper;

	private readonly AttackSequence attackSequence;

	public HaperJumpState(Creature enemy, AttackSequence attackSequence)
		: base(enemy)
	{
		haper = enemy.CastObject<Haper>();
		this.attackSequence = attackSequence;
	}

	public override void Enter(IState prevState)
	{
		attackSequence.ResetFrame(haper);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (attackSequence.IsFirstFrame)
		{
			attackSequence.Start(base.Creature);
		}
		if (attackSequence.IsDone)
		{
			ChangeState(haper.AvoidState);
		}
		attackSequence.FixedUpdate(haper, fdt);
	}
}
