namespace SPR;

public class OnePotionPart : WarmhornPart
{
	private int potionRegenFrame;

	private int currentFrame;

	public override void OnEquip()
	{
		base.OnEquip();
		potionRegenFrame = base.PartData.PartEffectDatasForEffects[1].DurationFrames[base.Level];
		Part.Player.PlayerStat.GetStat<WarmhornCount>().FixPotionCount(shouldFix: true);
		currentFrame = 0;
	}

	public override void Reset()
	{
		currentFrame = 0;
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.Player.PlayerStat.GetStat<WarmhornCount>().FixPotionCount(shouldFix: false);
	}

	public override void OnHeal()
	{
		currentFrame = potionRegenFrame;
	}

	public override void OnFixedUpdate()
	{
		if (currentFrame > 0)
		{
			currentFrame--;
			if (currentFrame == 0)
			{
				Part.Player.PlayerStat.GetStat<WarmhornCount>().ResetPotion();
			}
		}
	}
}
