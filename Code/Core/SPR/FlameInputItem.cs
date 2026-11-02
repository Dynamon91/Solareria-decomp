using UnityEngine;

namespace SPR;

public class FlameInputItem : InputItem
{
	private enum KeyState
	{
		Up,
		TryDown,
		Hold
	}

	private KeyState keyState;

	private bool onFlameState;

	private readonly float earlyInputAcceptTime;

	public float HoldDuration { get; private set; }

	public FlameInputItem(IState state)
		: base(state)
	{
		HoldDuration = -1f;
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
		keyState = KeyState.Up;
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.overheat)
		{
			if (keyState == KeyState.TryDown)
			{
				return;
			}
			switch (keyState)
			{
			case KeyState.Up:
				if (!InputItem.PlayerMover.PSM.CanControl)
				{
					keyState = KeyState.TryDown;
				}
				else if (InputItem.PlayerMover.PSM.CanControl)
				{
					keyState = KeyState.Hold;
					HoldDuration = 0f;
					base.InputTime = currentTime;
				}
				break;
			case KeyState.Hold:
				HoldDuration += Time.deltaTime;
				break;
			}
		}
		else
		{
			keyState = KeyState.Up;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (base.LastSuccessTime > InputItem.PlayerMover.LastGroundedTime)
		{
			return false;
		}
		return currentTime - base.InputTime <= earlyInputAcceptTime;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		onFlameState = true;
	}

	public void OnHoldKey()
	{
		keyState = KeyState.Hold;
		HoldDuration = 0f;
		base.InputTime = Time.time;
	}
}
