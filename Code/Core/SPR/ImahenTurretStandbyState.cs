using Library;
using SPR.FSM;

namespace SPR;

public class ImahenTurretStandbyState : EnemyState
{
	private readonly ImahenTurretBase turretBase;

	public ImahenTurretStandbyState(Enemy enemy)
		: base(enemy)
	{
		turretBase = enemy.CastObject<ImahenTurretBase>();
	}

	public override void Enter(IState prevState)
	{
		turretBase.SetAnimation("standby");
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
	}
}
