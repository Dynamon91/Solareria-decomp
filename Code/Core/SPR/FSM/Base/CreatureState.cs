namespace SPR.FSM.Base;

public abstract class CreatureState : EnemyState
{
	protected Creature Creature { get; }

	protected CreatureState(Creature enemy)
		: base(enemy)
	{
		Creature = enemy;
	}
}
