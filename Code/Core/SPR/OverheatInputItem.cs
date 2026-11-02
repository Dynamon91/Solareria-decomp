using System;

namespace SPR;

public class OverheatInputItem : InputItem, IHeatEffect
{
	private readonly float earlyAttackInputTime;

	public HeatSkillType HeatSkillType => HeatSkillType.Ignition;

	public event Action OnUse = delegate
	{
	};

	public OverheatInputItem(IState state)
		: base(state)
	{
		earlyAttackInputTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.overheat)
		{
			base.InputTime = currentTime;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (!PlayerHeat.CanUseSkill(HeatSkillType))
		{
			return false;
		}
		if (!InputItem.PlayerMover.IsOnGround)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyAttackInputTime)
		{
			return false;
		}
		if (GetSuccessDeltaTime(currentTime) < earlyAttackInputTime)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.CurrentState is PlayerStaggerHitState)
		{
			return false;
		}
		return true;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		this.OnUse?.Invoke();
	}
}
