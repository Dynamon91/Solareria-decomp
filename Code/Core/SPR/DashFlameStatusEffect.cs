using System;
using FMOD.Studio;
using Library;
using UnityEngine;

namespace SPR;

public class DashFlameStatusEffect : StatusEffect, IAttackSource
{
	private Enemy enemy;

	private MeshRenderer mesh;

	private Action damageEffectAction;

	private EventInstance loopSound;

	private DashAttackEffect dashAttackEffect;

	public override StatusEffectType EffectType { get; protected set; } = StatusEffectType.DashFlame;

	public override bool IsStackable => false;

	public string Name => "Dash Flame";

	public Direction FacingDirection => Direction.None;

	public DashFlameStatusEffect(IDamageable target, MeshRenderer mesh, int durationFrame, Action damageEffectAction)
		: base(target, durationFrame)
	{
		this.mesh = mesh;
		this.damageEffectAction = damageEffectAction;
	}

	protected override void ApplyEffect()
	{
		if (!dashAttackEffect.IsNull())
		{
			ActivateDashAttackEffect(value: false);
		}
	}

	public override void OnAdd()
	{
		enemy = Target.CastObject<Enemy>();
		ActivateDashAttackEffect(value: true);
	}

	public override void Update(StatusEffect elementEffect)
	{
	}

	public override void OnRemove(StatusEffectController statusEffectController, bool forceRemove)
	{
		if (!dashAttackEffect.IsNull())
		{
			ActivateDashAttackEffect(value: false);
		}
	}

	public void Explode()
	{
		if (!dashAttackEffect.IsInExplosion)
		{
			dashAttackEffect.PlayExplosionAsync(damageEffectAction).Forget();
			Game.Instance.Player.mover.Attack(new PlayerMover.AttackParameter(enemy, "dash_attack", 0, AttackAttribute.Default | AttackAttribute.BlockStuck, this));
			StatusEffect.SoundManager.PlayOneShotAttached("event:/SFX/Player/Skill/SFX_Player_DodgeSpark_Burst", dashAttackEffect.transform);
			StopLoopSound();
		}
	}

	private void ActivateDashAttackEffect(bool value)
	{
		if (value)
		{
			DashAttackEffect dashAttackEffect = Game.Instance.ObjectPoolManager.GetObject<DashAttackEffect>("Dash Attack Effect", enemy.transform.position, enemy.transform);
			dashAttackEffect.Initialize(enemy);
			dashAttackEffect.PlayReady();
			this.dashAttackEffect = dashAttackEffect;
			loopSound = StatusEffect.SoundManager.PlayOneShotAttached("event:/SFX/Player/Skill/SFX_Player_DodgeSpark_Mark", dashAttackEffect.transform);
		}
		else if (!this.dashAttackEffect.IsInExplosion)
		{
			this.dashAttackEffect.ReturnToPool();
			this.dashAttackEffect = null;
			StopLoopSound();
		}
	}

	private void StopLoopSound()
	{
		if (loopSound.isValid())
		{
			loopSound.stop(STOP_MODE.ALLOWFADEOUT);
			loopSound.release();
		}
	}
}
