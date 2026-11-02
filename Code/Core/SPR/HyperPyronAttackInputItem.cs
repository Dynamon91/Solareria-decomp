using System;

namespace SPR;

public class HyperPyronAttackInputItem : ParryActionInputItem, IHeatEffect
{
	public HeatSkillType HeatSkillType => HeatSkillType.HyperPyronAction;

	protected override Func<bool> getInput => () => InputItem.PlayerController.heatDown;

	public event Action OnUse = delegate
	{
	};

	public HyperPyronAttackInputItem(IState state)
		: base(state)
	{
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (PlayerHeat.CanUseSkill(HeatSkillType))
		{
			return base.CheckCondition(currentTime);
		}
		return false;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		this.OnUse?.Invoke();
	}
}
