using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class Soowoo : BossCharacter
{
	private enum Pattern
	{
		None,
		DownStrike,
		Push,
		UpperStrike,
		Wahaha,
		Combo,
		Surf
	}

	[Header("디버깅")]
	[SerializeField]
	private Pattern pattern;

	[Header("내려찍기")]
	[Tooltip("물기둥 대기 시간")]
	[SerializeField]
	private int waterRisingWaitDuration;

	[Tooltip("물기둥의 지속 시간")]
	[SerializeField]
	private int waterRisingDuration;

	[Header("밀어내기")]
	[Tooltip("물기둥 대기 시간")]
	[SerializeField]
	private float playerFlyAwayVelocityX;

	[Tooltip("물기둥 대기 시간")]
	[SerializeField]
	private float playerFlyAwayVelocityY;

	[Header("와하하하하")]
	[Tooltip("제자리 점프 높이")]
	[SerializeField]
	private float standingJumpHeight;

	[Tooltip("제자리 점프 시간")]
	[SerializeField]
	private int standingJumpDuration;

	[SerializeField]
	private AnimationCurve jumpCurve;

	[Header("파도타기")]
	[Tooltip("파도타기 점프 시간")]
	[SerializeField]
	private int surfJumpDuration;

	[Header("패시브 - 곡괭이")]
	[Tooltip("파편 최소 높이")]
	[SerializeField]
	private float minPower;

	[Tooltip("파편 최대 높이")]
	[SerializeField]
	private float maxPower;

	[Tooltip("파편 비산 각도")]
	[SerializeField]
	private int maxAngle;

	[Tooltip("파편 개수")]
	[SerializeField]
	private int fragmentCount;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<Soowoo> downStrike;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> push;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> upperStrikeMove;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> upperStrike;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> wahaha;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> combo;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> surfJump;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> surfP1;

	[SerializeField]
	private GenericBossAttackSequence<Soowoo> surfFinish;

	[Header("Sound Event")]
	[SerializeField]
	private EventReference passiveEvent;

	public const int OpeningStateIndex = 0;

	public const int RetryInFirstPhaseStateIndex = 1;

	public const int DyingStateIndex = 2;

	public const int DyingWaterfallStateIndex = 3;

	public const int AfterDyingStateIndex = 4;

	private AreaBox areaBox;

	private Vector2 leftEnd;

	private Vector2 rightEnd;

	public override string Name => "Soowoo";

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DyingState { get; set; }

	private void LateUpdate()
	{
		if (base.StateMachine.CurrentState == base.IdleState || (base.StateMachine.CurrentState == base.AttackState && base.AttackState.CurrentSequence != surfJump && base.AttackState.CurrentSequence != surfP1 && base.AttackState.CurrentSequence != surfFinish))
		{
			AdjustXPosition(areaBox);
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		DyingState = new CutsceneState(this, 2);
		AddAction(ActionTiming.Begin, "RisingWater", RisingWater);
		AddAction(ActionTiming.Begin, "StandingJump", WahahaJump);
		AddAction(ActionTiming.Begin, "JumpToSurfPosition", JumpToSurfPosition);
		AddAction(ActionTiming.Begin, "SpreadFragments", SpreadFragments);
		AddAction(ActionTiming.Begin, "MoveCenter", (Action)delegate
		{
			base.IsImmortal = true;
			base.transform.position = new Vector3(areaBox.Center.x, base.SpawnPosition.y);
		});
		AddAction(ActionTiming.End, "MoveCenter", (Action)delegate
		{
			base.IsImmortal = false;
		});
		AddAction(ActionTiming.OnFrame, "CheckPlayerDistance", delegate(Parameter[] p)
		{
			CheckPlayerDistance(p[0].Float);
		});
		SetAnimation("bubble_idle", loop: true, 1f, 1);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (base.IsImmortal || Game.Instance.Player.isDead)
		{
			return false;
		}
		int num = CalculateDamage(hitInfo.Damage, hitInfo.FireDamage);
		int value = health.Value;
		if (health.Value > 0)
		{
			if (base.Phase == 1 && base.Phase2Health > 0 && health.Value - num <= 0)
			{
				OnChangeHealth(health.Value - value);
				health = new ClampedInt(1, 0, maxHealth);
				OnDamaged(hitInfo);
				return false;
			}
			if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HyperBurst) || (HasStatusEffect<ExecutionStatusEffect>() && !IsExecutionFinished()))
			{
				num = ((health.Value > num) ? num : (health.Value - 1));
			}
			health -= num;
			OnChangeHealth(health.Value - value);
			if (health.Value <= 0)
			{
				Die();
				return true;
			}
		}
		if (base.StateMachine.CurrentState is CutsceneState)
		{
			if (Groggy.Value - hitInfo.GroggyDamage < phaseTransitionGroggyLimit)
			{
				hitInfo.SetGroggyDamage(Mathf.Max(0f, Groggy.Value - phaseTransitionGroggyLimit));
			}
		}
		else if (base.StateMachine.CurrentState is BossAttackState bossAttackState && (bossAttackState.CurrentSequence == surfJump || bossAttackState.CurrentSequence == surfP1 || bossAttackState.CurrentSequence == surfFinish) && Groggy.Value - hitInfo.GroggyDamage < 1f)
		{
			hitInfo.SetGroggyDamage(Mathf.Max(0f, Groggy.Value - 1f));
		}
		TakeGroggyDamage(hitInfo);
		if (num != 0)
		{
			OnDamaged(hitInfo);
		}
		return true;
	}

	public override void OnEnterExecution()
	{
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
		foreach (ParticleSystem particleSystem in componentsInChildren)
		{
			if (particleSystem.isPlaying)
			{
				particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}
		}
		base.OnEnterExecution();
	}

	protected override void Die()
	{
		EnterDie().Forget();
	}

	protected override void OnEnterGroggyState()
	{
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
		foreach (ParticleSystem particleSystem in componentsInChildren)
		{
			if (particleSystem.isPlaying)
			{
				particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}
		}
		base.OnEnterGroggyState();
	}

	public void SetArea(AreaBox areaBox)
	{
		this.areaBox = areaBox;
		leftEnd = MyArea.AreaBox.LowerLeft;
		rightEnd = MyArea.AreaBox.UpperRight;
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { downStrike, push, upperStrike, wahaha, combo, surfJump };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		if (pattern != Pattern.None)
		{
			return pattern switch
			{
				Pattern.DownStrike => new List<BossAttackSequence> { downStrike }, 
				Pattern.Push => new List<BossAttackSequence> { push }, 
				Pattern.UpperStrike => new List<BossAttackSequence> { upperStrikeMove, upperStrike }, 
				Pattern.Wahaha => new List<BossAttackSequence> { wahaha }, 
				Pattern.Combo => new List<BossAttackSequence> { combo }, 
				Pattern.Surf => new List<BossAttackSequence> { surfJump, surfP1, surfFinish }, 
				_ => new List<BossAttackSequence>(), 
			};
		}
		return GetSequence();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			2 => DyingState, 
			_ => null, 
		};
	}

	private List<BossAttackSequence> GetSequence()
	{
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, downStrike.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, push.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, upperStrikeMove.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, wahaha.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, combo.GetPower(this)));
		list.AddRange(Enumerable.Repeat(5, surfJump.GetPower(this)));
		int num = list[CryptoRandom.Range(0, list.Count)];
		for (int i = 0; i < base.AllAttackSequences.Count; i++)
		{
			if (i != num)
			{
				base.AllAttackSequences[i].CastObject<BossAttackSequence>().RisePower();
			}
		}
		return num switch
		{
			0 => new List<BossAttackSequence> { downStrike }, 
			1 => new List<BossAttackSequence> { push }, 
			2 => new List<BossAttackSequence> { upperStrikeMove, upperStrike }, 
			3 => new List<BossAttackSequence> { wahaha }, 
			4 => new List<BossAttackSequence> { combo }, 
			5 => new List<BossAttackSequence> { surfJump, surfP1, surfFinish }, 
			_ => new List<BossAttackSequence>(), 
		};
	}

	private void RisingWater()
	{
		if (base.Phase != 1)
		{
			Vector2 vector = new Vector2(GetBoneWorldPosition("weapon_main_ct_vt").x, base.SpawnPosition.y);
			SoowooRisingWater soowooRisingWater = Enemy.PoolManager.GetObject<SoowooRisingWater>("Soowoo Rising Water.prefab", vector);
			Vector2 vector2 = vector + new Vector2(0f, soowooRisingWater.transform.localScale.y / 2f);
			soowooRisingWater.transform.position = vector2;
			soowooRisingWater.Initialize(waterRisingWaitDuration, waterRisingDuration).Forget();
		}
	}

	private void WahahaJump()
	{
		ShortcutExtensions.DOJump(endValue: new Vector2(Enemy.Player.VisualBottom.x, base.SpawnPosition.y), target: base.transform, jumpPower: standingJumpHeight, numJumps: 1, duration: Util.FrameToSeconds(standingJumpDuration)).SetEase(jumpCurve);
		PoolableTimeLimitObject poolableTimeLimitObject = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Piece.prefab", VisualBottom);
		poolableTimeLimitObject.GetComponent<ParticleSystem>().Play();
		poolableTimeLimitObject.Initialize("Piece.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
	}

	private void JumpToSurfPosition()
	{
		Vector3 vector = new Vector3((MyArea.AreaBox.Center.x < Enemy.Player.VisualBottom.x) ? leftEnd.x : rightEnd.x, base.SpawnPosition.y);
		LookAt(vector);
		base.transform.DOJump(vector, standingJumpHeight, 1, Util.FrameToSeconds(surfJumpDuration)).SetEase(Ease.Linear);
	}

	private void SpreadFragments()
	{
		if (base.Phase != 1)
		{
			for (int i = 0; i < fragmentCount; i++)
			{
				float power = UnityEngine.Random.Range(minPower, maxPower);
				Vector2 direction = BasicExtension.ToDirectionVector(CryptoRandom.Range(maxAngle, 180 - maxAngle));
				Vector3 boneWorldPosition = GetBoneWorldPosition("weapon_main_ct_vt");
				Enemy.PoolManager.GetObject<SoowooFragment>("Soowoo Fragment.prefab", boneWorldPosition).Initialize(direction, power, 60f);
			}
			Enemy.SoundManager.PlayOneShotAttached(passiveEvent, base.transform);
		}
	}

	private void CheckPlayerDistance(float distance)
	{
		if (Vector2.Distance(Enemy.Player.Position, base.Position) <= distance)
		{
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { upperStrike }));
		}
	}
}
