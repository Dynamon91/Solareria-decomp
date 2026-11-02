namespace FSM;

public class SimpleStateMachine : IStateMachine
{
	public IState PreviousState { get; private set; }

	public IState CurrentState { get; private set; }

	public void FixedUpdate(float fdt)
	{
		CurrentState?.FixedUpdate(fdt);
	}

	public void ChangeState(IState newState)
	{
		PreviousState = CurrentState;
		CurrentState?.Exit(newState);
		CurrentState = newState;
		CurrentState?.Enter(PreviousState);
	}
}
