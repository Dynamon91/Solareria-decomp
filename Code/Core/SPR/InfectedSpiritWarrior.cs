using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Field;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class InfectedSpiritWarrior : HighCreature
{
	private enum ElementType
	{
		[InspectorName("불")]
		Fire,
		[InspectorName("물 (사용 불가)")]
		Water,
		[InspectorName("어둠")]
		Darkness,
		[InspectorName("바람 (사용 불가)")]
		Wind,
		[InspectorName("전기")]
		Electricity,
		[InspectorName("곰팡이 (사용 불가)")]
		Mold,
		[InspectorName("땅")]
		Earth
	}

	[Header("원소 속성")]
	[SerializeField]
	private ElementType elementType;

	[Header("땅 속성")]
	[SerializeField]
	private Zone earthAttack3GrabZone;

	[SerializeField]
	private float earthAttack3Speed;

	[SerializeField]
	private float chargeAttackDelay = 1f;

	[SerializeField]
	private EventReference projectileCrashEvent;

	[Header("Sequences")]
	[SerializeField]
	private GenericBossAttackSequence<InfectedSpiritWarrior> attack1;

	[SerializeField]
	private GenericBossAttackSequence<InfectedSpiritWarrior> attack2;

	[SerializeField]
	private GenericBossAttackSequence<InfectedSpiritWarrior> attack3;

	[Header("References")]
	[SerializeField]
	private Transform attack1LaunchPosition;

	private Vector3 attack3ChargeStartPosition;

	private IswElementField attackField;

	private float fieldRange;

	private CancellationTokenSource cts = new CancellationTokenSource();

	public override string ID => Name;

	public override string Name => "InfectedSpiritWarrior_" + elementType;

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void Activate()
	{
		SetConnectedPath();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.StateMachine.ChangeState(null);
		AddAction(ActionTiming.Begin, "Attack1Launch", delegate(Parameter[] p)
		{
			Attack1Launch(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "Attack2Move", Fade);
		AddAction(ActionTiming.Begin, "Attack3Charge", Attack3ChargeStart);
		AddAction(ActionTiming.OnFrame, "Attack3Charge", Attack3Charge);
		AddAction(ActionTiming.End, "Attack3Charge", GenerateField);
		AddAction(ActionTiming.Begin, "GenerateLightning", delegate(int duration, Parameter[] p)
		{
			GenerateLightning(duration, p[0].Int).Forget();
		});
		foreach (Transform item in base.transform)
		{
			if (item.TryGetComponent<SkeletonGhost>(out var component))
			{
				component.ghostingEnabled = false;
			}
		}
	}

	public override void OnEnterBounceState()
	{
		base.OnEnterBounceState();
		if ((object)attackField != null && attackField.gameObject.activeSelf)
		{
			GenerateField();
		}
		cts.Cancel();
	}

	public override void ResetStunCount()
	{
		cts = new CancellationTokenSource();
		base.ResetStunCount();
	}

	protected override void OnDie()
	{
		base.OnDie();
		SpiritWarriorArea spiritWarriorArea = MyArea.CastObject<SpiritWarriorArea>();
		DamageEffectAsync().Forget();
		spiritWarriorArea.OnCreatureDie();
		spiritWarriorArea.PlayClearTimeline(VisualCenter).Forget();
		ClearPlayerStagger();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack1, attack2, attack3 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return base.GenerateCurrentAttackSequences();
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		List<int> list = new List<int>();
		if (IsPlayerInAttackBound(attack1))
		{
			list.AddRange(Enumerable.Repeat(0, attack1.GetPower(this)));
		}
		if (IsPlayerInAttackBound(attack2))
		{
			list.AddRange(Enumerable.Repeat(1, attack2.GetPower(this)));
		}
		if (IsPlayerInAttackBound(attack3))
		{
			list.AddRange(Enumerable.Repeat(2, attack3.GetPower(this)));
		}
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { attack1, attack2, attack3 };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].RisePower();
			}
		}
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<AttackSequence> { attack1 }, 
			1 => new List<AttackSequence> { attack2 }, 
			2 => new List<AttackSequence> { attack3 }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = earthAttack3GrabZone.GetZoneOf(this);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}

	private void Attack1Launch(float speed)
	{
		switch (elementType)
		{
		case ElementType.Fire:
			Enemy.PoolManager.GetObject<IswFireProjectile>("Infected Spirit Warrior Fire Projectile.prefab", attack1LaunchPosition.position).Initialize(FacingDirection.ToVector2(), speed);
			break;
		case ElementType.Darkness:
			Enemy.PoolManager.GetObject<IswDarknessProjectile>("Infected Spirit Warrior Darkness Projectile.prefab", attack1LaunchPosition.position).Initialize(FacingDirection.ToVector2(), speed);
			break;
		case ElementType.Water:
		case ElementType.Wind:
		case ElementType.Mold:
		case ElementType.Earth:
		{
			Projectile projectile = Enemy.PoolManager.GetObject<Projectile>("Infected Spirit Warrior Earth Projectile.prefab", attack1LaunchPosition.position);
			projectile.Launch(FacingDirection.ToVector2(), speed);
			projectile.OnReturn += delegate
			{
				Enemy.PoolManager.GetObject<PoolingParticle>("Infected_Spirit_Earth_attack01_Crash.prefab", projectile.transform.position);
				Enemy.SoundManager.PlayOneShotAttached(projectileCrashEvent, projectile.transform);
			};
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		case ElementType.Electricity:
			break;
		}
	}

	private void Attack3ChargeStart()
	{
		switch (elementType)
		{
		case ElementType.Fire:
			attack3ChargeStartPosition = base.transform.position;
			attackField = Enemy.PoolManager.GetObject<IswElementField>("Infected Spirit Warrior Fire Field.prefab", VisualBottom);
			break;
		case ElementType.Darkness:
			attack3ChargeStartPosition = base.transform.position;
			attackField = Enemy.PoolManager.GetObject<IswElementField>("Infected Spirit Warrior Darkness Field.prefab", VisualBottom);
			break;
		case ElementType.Electricity:
			Enemy.PoolManager.GetObject<IswLightning>("Infected Spirit Warrior_Lightning.prefab", VisualBottom);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case ElementType.Water:
		case ElementType.Wind:
		case ElementType.Mold:
		case ElementType.Earth:
			break;
		}
	}

	private async UniTaskVoid GenerateLightning(int duration, int delay)
	{
		Vector2 startPos = VisualBottom;
		await UniTask.WaitForSeconds(Util.FrameToSeconds(duration), ignoreTimeScale: false, PlayerLoopTiming.Update, cts.Token);
		Vector2 endPos = VisualBottom;
		await UniTask.WaitForSeconds(Util.FrameToSeconds(delay), ignoreTimeScale: false, PlayerLoopTiming.Update, cts.Token);
		Enemy.PoolManager.GetObject<IswLightning>("Infected Spirit Warrior_Lightning.prefab", (startPos + endPos) / 2f);
	}

	private void Attack3Charge()
	{
		switch (elementType)
		{
		case ElementType.Fire:
		case ElementType.Darkness:
		{
			int signX = FacingDirection.GetSignX();
			fieldRange = Vector2.Distance(VisualBottom, attack3ChargeStartPosition);
			Vector3 position = attackField.transform.position;
			position.x = attack3ChargeStartPosition.x + fieldRange / 2f * (float)signX;
			attackField.transform.position = position;
			break;
		}
		case ElementType.Water:
		case ElementType.Wind:
		case ElementType.Mold:
		case ElementType.Earth:
		{
			Zone zoneOf = earthAttack3GrabZone.GetZoneOf(this);
			Collider2D[] array = AttackUtility.OverlapBoxAll(zoneOf.Center, zoneOf.Size, LayerUtility.PlayerLayer);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].TryGetComponent<PlayerMover>(out var _) && Enemy.Player.CanHit)
				{
					Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM, delegate
					{
						Enemy.Player.SetGravityFree(active: true);
					}, delegate
					{
						Enemy.Player.SetGravityFree(active: false);
					})
					{
						Duration = 9999,
						NextState = null
					});
					base.StateMachine.ChangeState(new IswEarthChargeState(this, earthAttack3Speed, chargeAttackDelay));
					break;
				}
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		case ElementType.Electricity:
			break;
		}
	}

	private void GenerateField()
	{
		switch (elementType)
		{
		case ElementType.Fire:
		case ElementType.Darkness:
		{
			int signX = FacingDirection.GetSignX();
			fieldRange = Vector2.Distance(VisualBottom, attack3ChargeStartPosition);
			Vector3 position = attackField.transform.position;
			position.x = attack3ChargeStartPosition.x + fieldRange / 2f * (float)signX;
			attackField.transform.position = position;
			attackField.SetScaleX(fieldRange);
			attackField.EnableAttackCycle().Forget();
			break;
		}
		case ElementType.Water:
		case ElementType.Wind:
		case ElementType.Electricity:
		case ElementType.Mold:
		case ElementType.Earth:
			break;
		}
	}

	private void Fade(int duration)
	{
		DOTween.Sequence().Append(base.SkeletonAnimation.Skeleton.DOAlpha(0f, Util.FrameToSeconds(duration) / 2f).SetEase(Ease.Linear)).Append(base.SkeletonAnimation.Skeleton.DOAlpha(1f, Util.FrameToSeconds(duration) / 2f).SetEase(Ease.Linear));
	}
}
