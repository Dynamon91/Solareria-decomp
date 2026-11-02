using Library;
using SPR.FSM;
using SPR.Library;

namespace SPR;

public class ImahenTurretIdleState : EnemyState
{
	private readonly ImahenTurretBase turretBase;

	private RandomFrameTimer timer;

	public ImahenTurretIdleState(Enemy enemy, int minWaitFrame, int maxWaitFrame)
		: base(enemy)
	{
		turretBase = enemy.CastObject<ImahenTurretBase>();
		timer = new RandomFrameTimer(minWaitFrame, maxWaitFrame);
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SetAnimation("idle");
		timer.Reset();
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (turretBase.IsPlayerInSightBound())
		{
			if (timer.IsDone)
			{
				ChangeState(turretBase.TurretAttackState);
				return;
			}
			timer.Tick();
			turretBase.Aim();
		}
	}
}
