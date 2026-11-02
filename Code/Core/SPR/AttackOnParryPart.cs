namespace SPR;

public class AttackOnParryPart : SimpleStatEffectPart, IParryEvent, IDamagedEvent
{
	private StatEffect statEffect;

	private int durationFrame;

	private int currentFrame;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		statEffect = new StatEffect(partEffectData, base.Level);
		durationFrame = partEffectData.DurationFrames[base.Level];
	}

	public override void OnFixedUpdate()
	{
		if (currentFrame >= 0)
		{
			currentFrame++;
			if (currentFrame >= durationFrame)
			{
				currentFrame = -1;
				statEffect.Clear();
			}
		}
	}

	public override void Reset()
	{
		statEffect.Clear();
		currentFrame = -1;
	}

	public void OnParry(Enemy enemy)
	{
		statEffect.Apply();
		currentFrame = 0;
	}

	public void OnDamaged(IAttackSource source, int damage)
	{
		statEffect.Clear();
		currentFrame = -1;
	}
}
