using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using SPR.Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class RoyalGuard : HighCreature
{
	[Header("잡기")]
	[SerializeField]
	private int grabHoldDuration = 70;

	[Header("Sequences")]
	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> rise;

	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> thrust;

	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> thrustGrab;

	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> combo1;

	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> combo2;

	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> combo3;

	[SerializeField]
	private GenericBossAttackSequence<RoyalGuard> combo3Thrust;

	[Header("Reference")]
	[SerializeField]
	private Transform grabTransform;

	private RoyalGuardArea area;

	private readonly List<RoyalGuardWaterBomb> bombs = new List<RoyalGuardWaterBomb>();

	private bool invincible;

	private bool needChangeState;

	private Vector2 chargeDirection;

	public override string Name => "RoyalGuard";

	private RoyalGuardArea Area => area ?? (area = GetComponentInParent<RoyalGuardArea>());

	public override void Activate()
	{
		base.gameObject.SetActive(value: true);
		SetConnectedPath();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.StateMachine.ChangeState(null);
		AddAction(ActionTiming.Begin, "FloodWater", delegate(Parameter[] p)
		{
			FloodWater(p[0].Int, p[1].Int, p[2].Float);
		});
		AddAction(ActionTiming.Begin, "RiseCharge", delegate(int duration, Parameter[] p)
		{
			SetChargeDirection(duration, p[0].Float, p[1].Float);
		});
		AddAction(ActionTiming.OnFrame, "RiseCharge", delegate(Parameter[] p)
		{
			RiseCharge(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "SpawnWaterBomb", delegate(Parameter[] p)
		{
			SpawnWaterBomb(p[0].Int, p[1].Int, p[2].Int, p[3].Float).Forget();
		});
		AddAction(ActionTiming.Begin, "Invincible", (Action)delegate
		{
			invincible = true;
		});
		AddAction(ActionTiming.End, "Invincible", (Action)delegate
		{
			invincible = false;
		});
		AddAction(ActionTiming.Begin, "Teleport", delegate(Parameter[] p)
		{
			Teleport(p[0].Int);
		});
		AddAction(ActionTiming.End, "Teleport", (Action<int, Parameter[]>)delegate
		{
			TeleportEnd();
		});
		AddAction(ActionTiming.Begin, "ActiveWall", (Action)delegate
		{
			Area.TransparentWall.enabled = true;
		});
		AddAction(ActionTiming.Begin, "ChangeGrabSequence", ChangeGrabSequence);
		AddAction(ActionTiming.OnFrame, "HoldPlayer", HoldPlayer);
		base.Exp = 0;
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!invincible)
		{
			return base.TryTakeDamage(hitInfo);
		}
		return false;
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (base.StateMachine.CurrentState != base.AttackState || (base.AttackState.CurrentSequence != thrust && base.AttackState.CurrentSequence != combo3Thrust) || !Enemy.Player.CanHit || Enemy.Player.PSM.StateOption.HasFlag(StateOption.Guard) || Enemy.Player.PSM.CurrentState is PlayerBrokenGuardState)
		{
			return;
		}
		invincible = false;
		if (Enemy.Player.CanParry(hitInfo))
		{
			base.StateMachine.ChangeState(new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration(base.BounceAnim)), useVFX: true, resetStun: false));
			return;
		}
		HitInfo breathHitInfo = new HitInfo(this, "Thrust_Breath");
		PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM, delegate
		{
			Enemy.Player.SetGravityFree(active: true);
		}, delegate
		{
			Enemy.Player.SetGravityFree(active: false);
			Enemy.Player.TryTakeDamage(breathHitInfo);
		}, delegate
		{
			Enemy.Player.ApplyKnockBack(base.transform, breathHitInfo.KnockBack);
		})
		{
			Duration = grabHoldDuration,
			CancelByOtherAttack = true,
			NextState = Enemy.Player.PSM.IdleState
		};
		Enemy.Player.PSM.ChangeState(newState);
		needChangeState = true;
		CheckPlayerHitWall(FacingDirection.ToVector2()).Forget();
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		bombs.ForEach(delegate(RoyalGuardWaterBomb x)
		{
			x.ReturnToPool();
		});
		Area.TransparentWall.enabled = false;
		ForceDrop();
		DOTween.Kill(Area.Water.transform);
		Area.Water.transform.DOMoveY(Area.WaterOriginPos.y, Util.FrameToSeconds(5f)).SetEase(Ease.Linear);
		invincible = false;
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		bombs.ForEach(delegate(RoyalGuardWaterBomb x)
		{
			x.ReturnToPool();
		});
		Area.TransparentWall.enabled = false;
		ForceDrop();
		DOTween.Kill(Area.Water.transform);
		Area.Water.transform.DOMoveY(Area.WaterOriginPos.y, Util.FrameToSeconds(5f)).SetEase(Ease.Linear);
		invincible = false;
	}

	public override void OnEnterBounceState()
	{
		bombs.ForEach(delegate(RoyalGuardWaterBomb x)
		{
			x.ReturnToPool();
		});
		Area.TransparentWall.enabled = false;
		ForceDrop();
		DOTween.Kill(Area.Water.transform);
		Area.Water.transform.DOMoveY(Area.WaterOriginPos.y, Util.FrameToSeconds(5f)).SetEase(Ease.Linear);
		invincible = false;
	}

	protected override void OnDie()
	{
		base.OnDie();
		DamageEffectAsync().Forget();
		Area.DisableWaterZone().Forget();
		Area.PlayClearTimeline(VisualCenter).Forget();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { rise, thrust, combo1, combo2, combo3 };
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		Area.TransparentWall.enabled = false;
		return SelectSequence();
	}

	private List<AttackSequence> SelectSequence()
	{
		List<int> list = new List<int>();
		if (IsPlayerInAttackBound(rise))
		{
			list.AddRange(Enumerable.Repeat(0, rise.GetPower(this)));
		}
		if (IsPlayerInAttackBound(thrust))
		{
			list.AddRange(Enumerable.Repeat(1, thrust.GetPower(this)));
		}
		if (IsPlayerInAttackBound(combo1))
		{
			list.AddRange(Enumerable.Repeat(2, combo1.GetPower(this)));
		}
		if (IsPlayerInAttackBound(combo2))
		{
			list.AddRange(Enumerable.Repeat(3, combo2.GetPower(this)));
		}
		if (IsPlayerInAttackBound(combo3))
		{
			list.AddRange(Enumerable.Repeat(4, combo3.GetPower(this)));
		}
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { rise, thrust, combo1, combo2, combo3 };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].RisePower();
			}
		}
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<AttackSequence> { rise }, 
			1 => new List<AttackSequence> { thrust }, 
			2 => new List<AttackSequence> { combo1 }, 
			3 => new List<AttackSequence> { combo2 }, 
			4 => new List<AttackSequence> { combo3, combo3Thrust }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private void FloodWater(int duration, int waitFrame, float yPos)
	{
		List<IDamageable> list = new List<IDamageable>();
		DOTween.Sequence().Append(Area.Water.transform.DOMoveY(yPos, Util.FrameToSeconds(duration))).AppendInterval(Util.FrameToSeconds(waitFrame))
			.OnUpdate(delegate
			{
				Area.HandleWaterOverlap(list);
			})
			.Append(Area.Water.transform.DOMoveY(Area.WaterOriginPos.y, Util.FrameToSeconds(duration)))
			.SetEase(Ease.Linear);
	}

	private void SetChargeDirection(int duration, float speed, float height)
	{
		float num = speed * Time.fixedDeltaTime;
		float num2 = (float)duration * num;
		float x = base.Position.x + (float)FacingDirection.GetSignX() * num2;
		float y = base.SpawnPosition.y + height;
		Vector2 vector = new Vector2(x, y);
		chargeDirection = (vector - base.Position).normalized;
	}

	private void RiseCharge(float speed)
	{
		base.Position += chargeDirection * (speed * Time.fixedDeltaTime);
	}

	private void Teleport(int fadeDuration)
	{
		base.SkeletonRenderer.skeleton.DOColor(new Color(1f, 1f, 1f, 0f), Util.FrameToSeconds(fadeDuration)).SetEase(Ease.Linear);
	}

	private void TeleportEnd()
	{
		base.SkeletonRenderer.skeleton.SetColor(new Color(1f, 1f, 1f, 1f));
		Vector2 position = base.PathNodesList[CryptoRandom.Range(base.DetectPathWidth / 2, base.PathNodesList.Count - base.DetectPathWidth / 2)].Position;
		base.Position = new Vector2(position.x, base.SpawnPosition.y);
	}

	private async UniTaskVoid SpawnWaterBomb(int spawnCount, int spawnInterval, int waitDuration, float speed)
	{
		bombs.ForEach(delegate(RoyalGuardWaterBomb royalGuardWaterBomb2)
		{
			royalGuardWaterBomb2.ReturnToPool();
		});
		int signX = FacingDirection.GetSignX();
		int rangeY = Mathf.RoundToInt(HitBox.size.y);
		for (int i = 0; i < spawnCount; i++)
		{
			float x = VisualBottom.x + (float)(-signX * CryptoRandom.Range(0, Mathf.RoundToInt(HitBox.size.x / 2f)));
			float y = VisualCenter.y + (float)CryptoRandom.Range(0, rangeY);
			Vector3 vector = new Vector3(x, y);
			RoyalGuardWaterBomb royalGuardWaterBomb = Enemy.PoolManager.GetObject<RoyalGuardWaterBomb>("Royal Guard Water Bomb.prefab", new Vector3(vector.x, base.SpawnPosition.y));
			royalGuardWaterBomb.transform.DOMoveY(vector.y, Util.FrameToSeconds(waitDuration)).SetEase(Ease.OutSine);
			royalGuardWaterBomb.Shoot(speed, waitDuration).Forget();
			bombs.Add(royalGuardWaterBomb);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(spawnInterval);
		}
	}

	private void ChangeGrabSequence()
	{
		if (needChangeState)
		{
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { thrustGrab }, forceAttack: true));
			needChangeState = false;
		}
	}

	private void HoldPlayer()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = grabTransform.position;
		}
	}

	private static async UniTaskVoid CheckPlayerHitWall(Vector2 direction)
	{
		while (!Physics2D.Raycast(Enemy.Player.VisualCenter, direction, 0.5f, LayerUtility.PlatformLayer))
		{
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		Enemy.PoolManager.GetObject<PoolingParticle>("RoyalGuard_Combo03_Water_Cannon_Hit.prefab", Enemy.Player.VisualCenter).Play();
	}
}
