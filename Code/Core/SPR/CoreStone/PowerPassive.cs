using Library;

namespace SPR.CoreStone;

public class PowerPassive : CoreStonePassive
{
	private PlayerStateMachine PSM => Game.Instance.Player.mover.PSM;

	protected override void ApplyFirstPassive()
	{
		PSM.PyronAttackState.CastObject<PlayerParryActionState>().SetSkillID("parry_action_power");
		PSM.HyperPyronAttackState.CastObject<PlayerParryActionState>().SetSkillID("parry_heavy_attack_power");
	}

	protected override void ApplySecondPassive()
	{
		PSM.BurstState.CastObject<PlayerBurstState>().SetSkillID("burst_1_power", "burst_2_power");
		PSM.HyperBurstState.CastObject<PlayerBurstState>().SetSkillID("burst_heat_1_power", "burst_heat_2_power");
	}

	protected override void ResetFirstPassive()
	{
		PSM.PyronAttackState.CastObject<PlayerParryActionState>().SetSkillID("parry_action");
		PSM.HyperPyronAttackState.CastObject<PlayerParryActionState>().SetSkillID("parry_heavy_attack");
	}

	protected override void ResetSecondPassive()
	{
		PSM.BurstState.CastObject<PlayerBurstState>().SetSkillID("burst_1", "burst_2");
		PSM.HyperBurstState.CastObject<PlayerBurstState>().SetSkillID("burst_heat_1", "burst_heat_2");
	}
}
