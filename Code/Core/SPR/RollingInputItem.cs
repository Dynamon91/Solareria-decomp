namespace SPR;

public class RollingInputItem : StaminaInputItem
{
	protected readonly float EarlyInputAcceptTime;

	private readonly float lateJumpAcceptTime;

	public RollingInputItem(IState state)
		: base(state)
	{
		EarlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyDashInputAcceptFrame);
		lateJumpAcceptTime = Util.FrameToSeconds(InputItem.pcp.LateJumpAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.dashDown)
		{
			base.InputTime = currentTime;
			SetInputThisFrame();
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (GetDeltaTime(base.InputTime, InputItem.PlayerMover.PSM.JumpUpInputItem.LastSuccessTime) < lateJumpAcceptTime || GetDeltaTime(base.InputTime, InputItem.PlayerMover.LastGroundedTime) > lateJumpAcceptTime)
		{
			return false;
		}
		if (!CheckCoolTime(currentTime))
		{
			return false;
		}
		IState currentState = InputItem.PlayerMover.PSM.CurrentState;
		float num = ((currentState is PlayerHitState || currentState is PlayerBrokenGuardState) ? Util.FrameToSeconds(InputItem.pcp.earlyRollingInputAcceptFrameOnHit) : EarlyInputAcceptTime);
		if (GetInputDeltaTime(currentTime) > num)
		{
			return false;
		}
		return CheckStaminaCondition();
	}

	protected virtual bool CheckCoolTime(float currentTime)
	{
		if (base.LastSuccessTime > 0f && GetSuccessDeltaTime(currentTime) < Util.FrameToSeconds(InputItem.pcp.rollingCoolTimeFrame))
		{
			return false;
		}
		float lastSuccessTime = InputItem.PlayerMover.PSM.DownRollingInputItem.LastSuccessTime;
		if (lastSuccessTime > 0f && GetDeltaTime(currentTime, lastSuccessTime) < Util.FrameToSeconds(InputItem.pcp.rollingCoolTimeFrame))
		{
			return false;
		}
		return true;
	}
}
