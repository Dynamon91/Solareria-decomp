using SPR.FSM.Base;

namespace SPR;

public class VanguardFireDrainState : CreatureState
{
	private readonly BossAttackSequence fireDrainSequence;

	public VanguardFireDrainState(Creature enemy, BossAttackSequence fireDrainSequence)
		: base(enemy)
	{
		this.fireDrainSequence = fireDrainSequence;
	}

	public override void Enter(IState prevState)
	{
		base.Creature.LookPlayer();
		fireDrainSequence.ResetFrame(base.Creature);
	}

	public override void Exit(IState nextState)
	{
		base.Creature.StopHealTween(GetType());
	}

	public override void FixedUpdate(float fdt)
	{
		if (fireDrainSequence.IsFirstFrame)
		{
			fireDrainSequence.Start(base.Creature);
			base.Creature.PlayHealTween(loop: true, GetType());
		}
		if (fireDrainSequence.IsDone)
		{
			ChangeState(base.Creature.AlertState);
		}
		fireDrainSequence.FixedUpdate(base.Creature, fdt);
	}
}
