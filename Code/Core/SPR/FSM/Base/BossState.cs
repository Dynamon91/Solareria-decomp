using Library;

namespace SPR.FSM.Base;

public abstract class BossState : EnemyState
{
	protected BossCharacter Boss { get; }

	public BossState(Enemy enemy)
		: base(enemy)
	{
		Boss = enemy.CastObject<BossCharacter>();
	}
}
