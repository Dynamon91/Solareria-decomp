public interface IState
{
	void Enter(IState prevState);

	void Exit(IState nextState);

	void FixedUpdate(float fdt);
}
