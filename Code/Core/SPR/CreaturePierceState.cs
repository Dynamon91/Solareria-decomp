using Library;
using SPR.Library;

namespace SPR;

public class CreaturePierceState : EnemyPierceState
{
	private readonly IPierce piercedEnemy;

	private readonly HitInfo dotHitInfo;

	private readonly FrameTimer dotTimer;

	public CreaturePierceState(Enemy enemy, HitInfo hitInfo, HitInfo dotHitInfo, int pauseFrame, int cycleFrame)
		: base(enemy, hitInfo, pauseFrame)
	{
		piercedEnemy = enemy.CastObject<IPierce>();
		this.dotHitInfo = dotHitInfo;
		dotTimer = new FrameTimer(cycleFrame);
	}

	public override void Enter(IState prevState)
	{
		base.Enter(prevState);
		piercedEnemy?.OnEnterPierce();
		base.Enemy.EnableVFX("stiffen");
	}

	public override void Exit(IState nextState)
	{
		base.Exit(nextState);
		piercedEnemy?.OnExitPierce();
	}

	public override void FixedUpdate(float fdt)
	{
		base.FixedUpdate(fdt);
		if (!base.Enemy.IsDead && dotTimer.IsDone)
		{
			base.Enemy.TryTakeDamage(dotHitInfo);
			dotTimer.Reset();
		}
		dotTimer.Tick();
	}
}
