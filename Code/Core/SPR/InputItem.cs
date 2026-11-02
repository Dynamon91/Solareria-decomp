using UnityEngine;

namespace SPR;

public abstract class InputItem
{
	private bool active;

	public float InputTime { get; protected set; }

	public float LastSuccessTime { get; protected set; }

	public IState TargetState { get; }

	public bool Active
	{
		get
		{
			return active;
		}
		set
		{
			active = value;
			OnActiveChanged();
		}
	}

	protected static PlayerController PlayerController => Game.Instance.Player;

	protected static PlayerMover PlayerMover => Game.Instance.Player.mover;

	protected static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	protected InputItem(IState state)
	{
		InputTime = -999f;
		LastSuccessTime = -999f;
		TargetState = state;
	}

	public abstract void UpdateInput(float currentTime);

	public bool CanExecute(float currentTime)
	{
		if (Active)
		{
			return CheckCondition(currentTime);
		}
		return false;
	}

	protected abstract bool CheckCondition(float currentTime);

	public void Execute()
	{
		OnExecute();
		PlayerMover.PSM.ChangeState(TargetState);
	}

	protected virtual void OnExecute()
	{
		LastSuccessTime = Time.time;
	}

	protected virtual void OnActiveChanged()
	{
	}

	protected float GetInputDeltaTime(float currentTime)
	{
		return GetDeltaTime(currentTime, InputTime);
	}

	protected float GetSuccessDeltaTime(float currentTime)
	{
		return GetDeltaTime(currentTime, LastSuccessTime);
	}

	protected float GetDeltaTime(float currentTime, float comparedTime)
	{
		return currentTime - comparedTime;
	}
}
