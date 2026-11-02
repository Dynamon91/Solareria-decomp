using Cysharp.Threading.Tasks;

namespace SPR;

public class EnemyBurnStatusEffect : StatusEffect, IAttackSource
{
	public string Name => "";

	public Direction FacingDirection => Direction.None;

	public override StatusEffectType EffectType { get; protected set; }

	public override bool IsStackable => false;

	public float Damage { get; private set; }

	public float FireDamage { get; private set; }

	private int CycleFrame { get; set; }

	public EnemyBurnStatusEffect(IDamageable target, float damage, float fireDamage, int cycleFrame, int durationFrame)
		: base(target, durationFrame)
	{
		Damage = damage;
		FireDamage = fireDamage;
		CycleFrame = cycleFrame;
	}

	protected override void ApplyEffect()
	{
	}

	public override void OnAdd()
	{
		if (Target is Enemy enemy)
		{
			enemy.PlayDeathFire();
		}
	}

	protected override void OnRunning()
	{
		if (base.CurrentFrame % CycleFrame == 0)
		{
			var (num, num2) = Game.Instance.Player.PlayerStat.GetDamageInteger(random: true, Damage, FireDamage);
			Target.TryTakeDamage(new HitInfo(this, num, num2, 0f, AttackDirection.Both, AttackAttribute.Continuous | AttackAttribute.FixedDamage));
			StatusEffect.SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_Burn");
		}
	}

	public override void Update(StatusEffect elementEffect)
	{
		if (elementEffect is EnemyBurnStatusEffect enemyBurnStatusEffect)
		{
			CycleFrame = enemyBurnStatusEffect.CycleFrame;
			base.DurationFrame = enemyBurnStatusEffect.DurationFrame;
			base.CurrentFrame = 0;
			Damage = enemyBurnStatusEffect.Damage;
			FireDamage = enemyBurnStatusEffect.FireDamage;
		}
	}

	public override void OnRemove(StatusEffectController statusEffectController, bool forceRemove)
	{
		if (Target is Enemy enemy)
		{
			enemy.StopDeathFireAsync().Forget();
		}
	}
}
