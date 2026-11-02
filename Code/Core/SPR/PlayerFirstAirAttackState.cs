using System;
using System.Collections.Generic;

namespace SPR;

public class PlayerFirstAirAttackState : PlayerAirAttackState
{
	private const string EffectName = "dagger/attack_air_1";

	public PlayerFirstAirAttackState(PlayerStateMachine machine)
		: base(machine, "dagger/attack_air_1")
	{
	}

	protected override AttackTask GetAttackTask()
	{
		return new AirAttackTask();
	}

	protected override List<Type> GetInputItemTypes()
	{
		return new List<Type>
		{
			typeof(SecondAirAttackInputItem),
			typeof(JumpUpInputItem),
			typeof(DoubleJumpUpInputItem),
			typeof(AshBreakInputItem),
			typeof(AshBlastInputItem),
			typeof(FastDownInputItem),
			typeof(ChargeAttackInputItem),
			typeof(JumpDashInputItem),
			typeof(RollingInputItem),
			typeof(GuardInputItem),
			typeof(SpecialParryInputItem),
			typeof(OverheatInputItem)
		};
	}
}
