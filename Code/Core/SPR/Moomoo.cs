using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.FSM;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class Moomoo : HighCreature
{
	[Header("공격 패턴")]
	[SerializeField]
	private GenericBossAttackSequence<Moomoo> horn;

	[SerializeField]
	private GenericBossAttackSequence<Moomoo> charge;

	[SerializeField]
	private GenericBossAttackSequence<Moomoo> stompOnBeat;

	[SerializeField]
	private GenericBossAttackSequence<Moomoo> stompOffBeat;

	[Header("SFX")]
	[SerializeField]
	private EventReference collisionEvent;

	[SerializeField]
	private EventReference jumpEvent;

	[SerializeField]
	private EventReference smashEvent;

	private bool knockBackApplied;

	private CinemachineImpulseSource ImpulseSource { get; set; }

	public override string Name => "Moomoo";

	public override string GroggyLoopAnim => "groggy";

	public override EnemyState DefaultState => base.PatrolState;

	public override Area MyArea => GetComponentInParent<MoomooArea>();

	public EventReference CollisionEvent => collisionEvent;

	public EventReference JumpEvent => jumpEvent;

	public EventReference SmashEvent => smashEvent;

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void Activate()
	{
		base.gameObject.SetActive(value: true);
		SetConnectedPath();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.StateMachine.ChangeState(null);
		if (TryGetComponent<CinemachineImpulseSource>(out var component))
		{
			ImpulseSource = component;
		}
		SetAnimation("idle");
		AddAction(ActionTiming.Begin, "HornEffect", HornEffect);
		AddAction(ActionTiming.Begin, "SpawnWave", delegate(Parameter[] param)
		{
			SpawnWave(param[0].Float);
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		bool result = base.TryTakeDamage(hitInfo);
		if ((float)base.Health / (float)maxHealth <= 0.5f)
		{
			Enemy.SoundManager.SetEnemyWaveParameter(2, 3);
		}
		return result;
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("Charge") && !Enemy.Player.IsImmortal)
		{
			hitInfo.SetIgnoreStun(value: false);
			if (Enemy.Player.CanParry(hitInfo))
			{
				base.StateMachine.ChangeState(new MoomooHitWallInChargeState(this));
			}
			else
			{
				base.StateMachine.ChangeState(new MoomooHitPlayerInChargeState(this));
			}
			base.VfxController.StopAll();
		}
	}

	public override void OnPlatformHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("Charge"))
		{
			base.StateMachine.ChangeState(new MoomooHitWallInChargeState(this));
			base.VfxController.StopAll();
		}
	}

	public void EmitQuake()
	{
		ImpulseSource.GenerateImpulse();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { horn, charge, stompOnBeat, stompOffBeat };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return base.GenerateCurrentAttackSequences();
	}

	protected override void OnDie()
	{
		base.OnDie();
		LazySingleton<MessageSystem>.Instance.Publish(new MoomooDeathEvent());
		Enemy.SoundManager.StopBGM();
		DamageEffectAsync().Forget();
		MyArea.CastObject<LegendaryArea>().PlayClearTimeline(VisualCenter).Forget();
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		List<int> list = new List<int>();
		if (IsPlayerInAttackBound(stompOnBeat))
		{
			list.AddRange(Enumerable.Repeat(0, stompOnBeat.GetPower(this)));
		}
		if (IsPlayerInAttackBound(charge))
		{
			list.AddRange(Enumerable.Repeat(1, charge.GetPower(this)));
		}
		if (IsPlayerInAttackBound(horn))
		{
			list.AddRange(Enumerable.Repeat(2, horn.GetPower(this)));
		}
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { stompOnBeat, charge, horn };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].RisePower();
			}
		}
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<AttackSequence>
			{
				stompOnBeat,
				CryptoRandom.RandomBool() ? stompOnBeat : stompOffBeat
			}, 
			1 => new List<AttackSequence> { charge }, 
			2 => new List<AttackSequence> { horn }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private void HornEffect()
	{
		PoolableTimeLimitObject poolableTimeLimitObject = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Moomoo_PetalBomb.prefab", VisualBottom);
		poolableTimeLimitObject.Initialize("Moomoo_PetalBomb.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		poolableTimeLimitObject.GetComponent<ParticleSystem>().Play();
		poolableTimeLimitObject.transform.localScale = new Vector3(FacingDirection.GetSignX(), 1f, 1f);
	}

	private void SpawnWave(float speed)
	{
		ImpulseSource.GenerateImpulse();
		Vector3 boneWorldPosition = GetBoneWorldPosition("Leg_Front_B3");
		PoolableTimeLimitObject poolableTimeLimitObject = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("VFX/Moomoo_StompDust.prefab", new Vector3(boneWorldPosition.x, VisualBottom.y));
		poolableTimeLimitObject.Initialize("VFX/Moomoo_StompDust.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		poolableTimeLimitObject.transform.localScale = new Vector3(FacingDirection.GetSignX(), 1f, 1f);
		LazySingleton<MessageSystem>.Instance.Publish(new MoomooVineEvent());
	}
}
