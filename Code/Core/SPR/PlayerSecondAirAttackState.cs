using System;
using System.Collections.Generic;

namespace SPR;

public class PlayerSecondAirAttackState : PlayerAirAttackState
{
	private const string EffectName = "dagger/attack_air_2_loop";

	public PlayerSecondAirAttackState(PlayerStateMachine machine)
		: base(machine, "dagger/attack_air_2_loop")
	{
	}

	protected override AttackTask GetAttackTask()
	{
		return new SecondAirAttackTask();
	}

	protected override List<Type> GetInputItemTypes()
	{
		return new List<Type>
		{
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
