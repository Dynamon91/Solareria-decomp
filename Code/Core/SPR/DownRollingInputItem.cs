namespace SPR;

public class DownRollingInputItem : RollingInputItem
{
	public DownRollingInputItem(IState state)
		: base(state)
	{
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		InputItem.PlayerMover.DisableVFX("bullet_time");
	}

	protected override bool CheckCoolTime(float currentTime)
	{
		if (!(base.LastSuccessTime < 0f))
		{
			return GetSuccessDeltaTime(currentTime) >= Util.FrameToSeconds(InputItem.pcp.rollingCoolTimeFrame);
		}
		return true;
	}
}
