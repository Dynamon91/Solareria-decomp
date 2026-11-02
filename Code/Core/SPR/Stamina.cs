using StaticData;
using UnityEngine;

namespace SPR;

public class Stamina : Stat
{
	private int spRegainWaitFrame;

	public int MaxStamina => Mathf.CeilToInt(base.Max);

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private float Regain => Stat.PlayerStat.GetStat<StaminaRegain>().Current;

	public Stamina(float initial, Difficulty difficulty)
		: base(StatType.Stamina, initial, difficulty)
	{
	}

	public override float GetCurrentValue()
	{
		return MaxStamina;
	}

	public override float GetPreviewValue(float traitIncreases)
	{
		return Mathf.CeilToInt(base.GetPreviewValue(traitIncreases));
	}

	public void AddStamina(float amount)
	{
		ChangeAmount(amount);
		UpdateStaminaBar();
		if (amount < 0f)
		{
			spRegainWaitFrame = 0;
		}
	}

	public void ResetStamina()
	{
		AddStamina(base.Max);
	}

	public void ResetStaminaRegainCycle()
	{
		spRegainWaitFrame = 0;
	}

	public void RecoverStaminaPerFrame(float dt)
	{
		if (spRegainWaitFrame < pcp.StaminaRegainFrame)
		{
			spRegainWaitFrame++;
		}
		else if (!base.IsFull)
		{
			AddStamina(Regain * dt);
		}
	}

	public void UpdateStaminaBar()
	{
		Stat.FieldCanvas.PlayerStatusBoard.UpdateStaminaBar(base.Current, MaxStamina);
	}

	protected override void OnMaxChanged()
	{
		UpdateStaminaBar();
	}
}
