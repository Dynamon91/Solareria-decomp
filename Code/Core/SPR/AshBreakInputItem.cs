using UnityEngine;

namespace SPR;

public class AshBreakInputItem : StaminaInputItem
{
	private bool keyDown;

	private float lastFinishedTime;

	private readonly float earlyInputAcceptTime;

	public AshBreakInputItem(IState state)
		: base(state)
	{
		lastFinishedTime = -999f;
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.skillA)
		{
			if (!keyDown)
			{
				keyDown = true;
				base.InputTime = currentTime;
				SetInputThisFrame();
			}
		}
		else
		{
			keyDown = false;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (lastFinishedTime > InputItem.PlayerMover.LastGroundedTime)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyInputAcceptTime)
		{
			return false;
		}
		return CheckStaminaCondition();
	}

	public void OnFinishedAshBreak()
	{
		lastFinishedTime = Time.time;
	}
}
