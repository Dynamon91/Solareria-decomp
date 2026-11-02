using System;

namespace SPR;

public class HyperBurstInputItem : InputItem, IHeatEffect
{
	private readonly float earlyAttackInputTime;

	public const float ExecutionRate = 0.05f;

	private const string SkillDataID = "burst_heat";

	public HeatSkillType HeatSkillType => HeatSkillType.HyperBurst;

	public Enemy TargetEnemy { get; private set; }

	public event Action OnUse = delegate
	{
	};

	public HyperBurstInputItem(IState state)
		: base(state)
	{
		earlyAttackInputTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.heatDown)
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
		if (GetSuccessDeltaTime(currentTime) <= earlyAttackInputTime)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyAttackInputTime)
		{
			return false;
		}
		return InputItem.PlayerMover.PSM.BurstInputItem.CachedTarget?.IsDefenseless ?? false;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		TargetEnemy = InputItem.PlayerMover.PSM.BurstInputItem.CachedTarget;
		TargetEnemy.ActivateBurstIcon(value: true, isClicked: true);
		this.OnUse?.Invoke();
	}

	private int GetDamage()
	{
		int num = 0;
		string text = "burst_heat_1";
		string text2 = "burst_heat_2";
		int count = Game.DataManager.GetSkillData(text).Coefficients.Count;
		int count2 = Game.DataManager.GetSkillData(text2).Coefficients.Count;
		for (int i = 0; i < count; i++)
		{
			(int, int) damageResultWithoutRandom = InputItem.PlayerStat.GetDamageResultWithoutRandom(text, i);
			num += InputItem.PlayerMover.PSM.BurstInputItem.CachedTarget.CalculateDamage(damageResultWithoutRandom.Item1, damageResultWithoutRandom.Item2);
		}
		for (int j = 0; j < count2; j++)
		{
			(int, int) damageResultWithoutRandom2 = InputItem.PlayerStat.GetDamageResultWithoutRandom(text2, j);
			num += InputItem.PlayerMover.PSM.BurstInputItem.CachedTarget.CalculateDamage(damageResultWithoutRandom2.Item1, damageResultWithoutRandom2.Item2);
		}
		return num;
	}
}
