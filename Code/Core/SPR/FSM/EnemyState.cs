namespace SPR.FSM;

public abstract class EnemyState : IState
{
	protected Enemy Enemy { get; }

	protected static PlayerMover Player => Game.Instance.Player.mover;

	protected EnemyState(Enemy enemy)
	{
		Enemy = enemy;
	}

	public abstract void Enter(IState prevState);

	public abstract void Exit(IState nextState);

	public abstract void FixedUpdate(float fdt);

	protected void ChangeState(EnemyState newState)
	{
		Enemy.StateMachine.ChangeState(newState);
	}
}
