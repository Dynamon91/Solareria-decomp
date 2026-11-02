using SPR.FSM.Base;

namespace SPR;

public class GrakalChargeAttackState : CreatureState
{
	private readonly Grakal grakal;

	private readonly AttackSequence chargeAttack;

	public GrakalChargeAttackState(Grakal grakal, AttackSequence chargeAttack)
		: base(grakal)
	{
		this.grakal = grakal;
		this.chargeAttack = chargeAttack;
	}

	public override void Enter(IState prevState)
	{
		chargeAttack.ResetFrame(base.Creature);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (chargeAttack.IsFirstFrame)
		{
			chargeAttack.Start(base.Creature);
		}
		if (chargeAttack.IsDone)
		{
			ChangeState(base.Creature.AlertState);
		}
		chargeAttack.FixedUpdate(base.Creature, fdt);
	}
}
