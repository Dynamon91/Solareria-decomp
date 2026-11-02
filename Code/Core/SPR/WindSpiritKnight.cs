using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class WindSpiritKnight : Creature, IPierce
{
	private enum Pattern
	{
		None,
		Thrust,
		Swing,
		SlashTwice,
		Shield
	}

	[Header("패시브 - 전기 인챈트")]
	[SerializeField]
	private int warningFrame;

	[SerializeField]
	private int lightningDuration;

	[Header("바람 칼날")]
	[SerializeField]
	private Zone windBladeAttackZone;

	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<WindSpiritKnight> thrust;

	[SerializeField]
	private GenericAttackSequence<WindSpiritKnight> swing;

	[SerializeField]
	private GenericAttackSequence<WindSpiritKnight> slashTwice;

	[SerializeField]
	private GenericAttackSequence<WindSpiritKnight> shield;

	[Header("Reference")]
	[SerializeField]
	private ParticleSystem windShield;

	[SerializeField]
	private ParticleSystem windShieldBrake;

	[SerializeField]
	private ParticleSystem dotParticle;

	[Header("SFX")]
	[SerializeField]
	private EventReference lightningEvent;

	[SerializeField]
	private EventReference shieldDestroyEvent;

	[SerializeField]
	private EventReference windBladeEvent;

	private CancellationTokenSource cts;

	public override string Name => "WindSpiritKnight";

	public override bool CanMakeStuck => windShield.gameObject.activeSelf;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		SetAnimation("acc_spin", loop: true, 1f, 1);
		AddAttack(thrust, () => new List<AttackSequence> { thrust });
		AddAttack(swing, () => new List<AttackSequence> { swing });
		AddAttack(slashTwice, () => new List<AttackSequence> { slashTwice });
		AddAttack(shield, () => new List<AttackSequence> { shield });
		AddAction(ActionTiming.Begin, "ActivateShield", ActivateShield);
		AddAction(ActionTiming.Begin, "WindBlade", delegate(Parameter[] p)
		{
			WindBlade(p[0].Int, p[1].Int).Forget();
		});
		cts = new CancellationTokenSource();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (windShield.gameObject.activeSelf && hitInfo.Damage > 0 && (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack) || hitInfo.Source is MyosoCombat))
		{
			DeactivateShield();
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (!Enemy.Player.CanParry(hitInfo) && Enemy.Player.CanHit)
		{
			Vector2 vector = ((Enemy.Player.PSM.CurrentState is PlayerGuardState) ? hitInfo.GuardKnockBack : hitInfo.KnockBack);
			RaycastHit2D raycastHit2D = Physics2D.Raycast(Enemy.Player.VisualCenter, new Vector2(Enemy.Player.LastHitDirection, 0f), vector.x, LayerUtility.PlatformLayer);
			Vector2 position = (raycastHit2D ? new Vector2(raycastHit2D.point.x, Enemy.Player.GetPositionFromGroud().y) : (Enemy.Player.GetPositionFromGroud() + new Vector2((float)Enemy.Player.LastHitDirection * vector.x, 0f)));
			GenerateLightning(position);
		}
	}

	public override void OnEnterBounceState()
	{
		cts?.Cancel();
	}

	public void OnEnterPierce()
	{
		cts?.Cancel();
		dotParticle.Play();
	}

	public void OnExitPierce()
	{
		dotParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
	}

	protected override void OnDie()
	{
		base.OnDie();
		cts?.Cancel();
		RemoveAnimation(1);
		if (windShield.gameObject.activeSelf)
		{
			DeactivateShield();
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { thrust, swing, slashTwice, shield };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		cts = new CancellationTokenSource();
		return base.GetAttackSequences();
	}

	private void GenerateLightning(Vector2 position)
	{
		LightningAttack lightning = Enemy.PoolManager.GetObject<LightningAttack>("Wind Spirit Knight_Lightning.prefab", position);
		lightning.Activate(warningFrame, lightningDuration).Forget();
		UniTaskExtension.WaitForFixedUpdatesOrCancel(warningFrame - 24).GetAwaiter().OnCompleted(delegate
		{
			Enemy.SoundManager.PlayOneShotAttached(lightningEvent, lightning.transform);
		});
	}

	private void ActivateShield()
	{
		windShield.gameObject.SetActive(value: true);
		windShield.Play();
		RemoveAttack(shield);
	}

	private void DeactivateShield()
	{
		windShield.gameObject.SetActive(value: false);
		windShieldBrake.Play(withChildren: true);
		Enemy.SoundManager.PlayOneShotAttached(shieldDestroyEvent, base.transform);
		AddAttack(shield, () => new List<AttackSequence> { shield });
	}

	private async UniTaskVoid WindBlade(int warningDuration, int attackDuration)
	{
		Enemy.PoolManager.GetObject<PoolingParticle>("vfx_WindBlade_Cast.prefab", base.Position, base.transform).Play();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(warningDuration);
		if (!cts.IsCancellationRequested)
		{
			Enemy.PoolManager.GetObject<PoolingParticle>("WindSpiritKnight_Wind_Blade_2.prefab", base.Position, base.transform).Play();
			Enemy.SoundManager.PlayOneShotAttached(windBladeEvent, base.transform);
			List<IDamageable> list = new List<IDamageable>();
			for (int frame = 0; frame < attackDuration; frame++)
			{
				Zone zoneOf = windBladeAttackZone.GetZoneOf(this);
				AttackUtility.AttackBox(zoneOf.Center, zoneOf.Size, new HitInfo(this, "WindBlade"), LayerUtility.PlayerLayer, list);
				await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cts.Token);
			}
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = windBladeAttackZone.GetZoneOf(this);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}
}
