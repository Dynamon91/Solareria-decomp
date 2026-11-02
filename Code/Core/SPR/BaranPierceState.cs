using Library;
using SPR.Library;

namespace SPR;

public class BaranPierceState : EnemyPierceState
{
	private readonly Baran baran;

	private readonly int defenseReduction;

	private readonly FrameTimer timer;

	protected override string HitEventPath => "event:/SFX/Char/Myoso/SFX_Myoso_SpearHit_Baran";

	public BaranPierceState(Enemy enemy, HitInfo hitInfo, int pauseFrame, int defenseReduction)
		: base(enemy, hitInfo, pauseFrame)
	{
		baran = enemy.CastObject<Baran>();
		this.defenseReduction = defenseReduction;
	}

	public override void Enter(IState prevState)
	{
		base.Enter(prevState);
		baran.HitSpear();
		baran.SkeletonAnimation.timeScale = 0f;
		baran.ReduceDefense(defenseReduction);
		baran.OnPierce();
		baran.EnableVFX("stiffen");
	}

	public override void Exit(IState nextState)
	{
		base.Exit(nextState);
		baran.SkeletonAnimation.timeScale = 1f;
		baran.ReduceDefense(0);
	}
}
