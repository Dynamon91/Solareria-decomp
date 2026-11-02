using UnityEngine;

namespace SPR;

public class HealthRegenPart : SimpleStatEffectPart
{
	private float regenRate;

	private int cycleFrame;

	private int currentFrame;

	private bool isHealing;

	private Health Health => Game.Instance.Player.PlayerStat.GetStat<Health>();

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		regenRate = partEffectData.Rates[base.Level] / 100f;
		cycleFrame = partEffectData.CycleFrame;
		isHealing = !Health.IsFull;
		currentFrame = 0;
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard.StopHealTween(GetType());
	}

	public override void OnFixedUpdate()
	{
		if (!isHealing && !Health.IsFull)
		{
			isHealing = true;
			Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard.PlayHealTween(loop: true, GetType());
		}
		else if (isHealing && Health.IsFull)
		{
			isHealing = false;
			Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard.StopHealTween(GetType());
		}
		currentFrame++;
		if (currentFrame >= cycleFrame)
		{
			Health stat = Game.Instance.Player.PlayerStat.GetStat<Health>();
			stat.AddHealth(Mathf.CeilToInt((float)stat.MaxHealth * regenRate));
			currentFrame = 0;
		}
	}

	public override void Reset()
	{
	}
}
