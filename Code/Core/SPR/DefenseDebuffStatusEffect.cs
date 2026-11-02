using Library;

namespace SPR;

public class DefenseDebuffStatusEffect : StatusEffect
{
	public override StatusEffectType EffectType { get; protected set; } = StatusEffectType.Defense;

	public override bool IsStackable => false;

	public float DefenseRate { get; private set; }

	public DefenseDebuffStatusEffect(IDamageable target, int durationFrame, float debuffRate)
		: base(target, durationFrame)
	{
		DefenseRate = 1f - debuffRate;
	}

	protected override void ApplyEffect()
	{
	}

	public override void OnAdd()
	{
	}

	public override void Update(StatusEffect elementEffect)
	{
		DefenseDebuffStatusEffect defenseDebuffStatusEffect = elementEffect.CastObject<DefenseDebuffStatusEffect>();
		DefenseRate = defenseDebuffStatusEffect.DefenseRate;
		base.DurationFrame = defenseDebuffStatusEffect.DurationFrame;
		base.CurrentFrame = defenseDebuffStatusEffect.CurrentFrame;
	}

	public override void OnRemove(StatusEffectController statusEffectController, bool forceRemove)
	{
	}
}
