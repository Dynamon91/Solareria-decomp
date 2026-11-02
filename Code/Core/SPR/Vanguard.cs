using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class Vanguard : HighCreature
{
	[Header("스탯 파라미터")]
	[SerializeField]
	private int defaultDef;

	[SerializeField]
	private int guardDef;

	[Header("가드")]
	[SerializeField]
	private int guardRegenHealth;

	[Header("그랩")]
	[SerializeField]
	private int fireDrainValue = 30;

	[Header("공격 패턴")]
	[SerializeField]
	private GenericBossAttackSequence<Vanguard> spinTwiceAttack;

	[SerializeField]
	private GenericBossAttackSequence<Vanguard> spinReverseAttack;

	[SerializeField]
	private GenericBossAttackSequence<Vanguard> grabAttack;

	[SerializeField]
	private GenericBossAttackSequence<Vanguard> fireDrain;

	[SerializeField]
	private GenericBossAttackSequence<Vanguard> guard;

	[SerializeField]
	private GenericBossAttackSequence<Vanguard> attack3;

	[Header("Sound")]
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference playerGroundEvent;

	private bool isGuard;

	private VanguardFireDrainState fireDrainState;

	private CancellationTokenSource guardCancelTokenSource;

	private HitInfo takeDamageInfo;

	public override string Name => "Vanguard";

	public override string GroggyStartAnim => "groggy";

	public override string GroggyLoopAnim => "groggy";

	protected override int OriginDef
	{
		get
		{
			if (!isGuard)
			{
				return base.EnemyStatData.Def[0];
			}
			return base.EnemyStatData.Def[1];
		}
	}

	public override Area MyArea => GetArea(3);

	public override bool CanMakeStuck
	{
		get
		{
			if (isGuard && FacingDirection.GetSignX() == (Enemy.Player.VisualCenter - VisualCenter).x.GetSign())
			{
				return !takeDamageInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck);
			}
			return false;
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.PatrolState = new VanguardPatrolState(this);
		fireDrainState = new VanguardFireDrainState(this, fireDrain);
		base.StateMachine.ChangeState(DefaultState);
		AddAction(ActionTiming.End, "TryGrabPlayer", (Action)delegate
		{
			if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState)
			{
				base.StateMachine.ChangeState(fireDrainState);
			}
		});
		AddAction(ActionTiming.OnFrame, "HoldPlayer", HoldPlayerOnFrame);
		AddAction(ActionTiming.End, "HoldPlayer", (Action)delegate
		{
			UniTask.WaitUntil(() => Enemy.Player.IsOnGround).GetAwaiter().OnCompleted(delegate
			{
				Enemy.SoundManager.PlayOneShotAttached(playerGroundEvent, Enemy.Player.transform);
			});
			Enemy.Player.PSM.ChangeState(Enemy.Player.PSM.DownState);
		});
		AddAction(ActionTiming.Begin, "Guard", delegate(int duration)
		{
			Guard(duration).Forget();
		});
		guardCancelTokenSource = new CancellationTokenSource();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		takeDamageInfo = hitInfo;
		if (CanMakeStuck)
		{
			health += guardRegenHealth;
			OnChangeHealth(guardRegenHealth);
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override void OnEnterBounceState()
	{
		base.OnEnterBounceState();
		guardCancelTokenSource.Cancel();
		guardCancelTokenSource = new CancellationTokenSource();
		isGuard = false;
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		guardCancelTokenSource.Cancel();
		guardCancelTokenSource = new CancellationTokenSource();
		isGuard = false;
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("Grab"))
		{
			if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState)
			{
				HoldPlayerOnFrame();
				return;
			}
			if (Enemy.Player.CanHit && Enemy.Player.CanGrab)
			{
				Enemy.Player.PSM.ChangeState(new PlayerStaggerHitState(Enemy.Player.PSM, delegate
				{
					Enemy.Player.SetGravityFree(active: true);
				}, delegate
				{
					Enemy.Player.SetGravityFree(active: false);
				})
				{
					Duration = fireDrain.Duration,
					NextState = Enemy.Player.PSM.DownState,
					TargetPosition = GetBoneWorldPosition("Finger_R_2_1")
				});
			}
		}
		if (hitInfo.AttackName.Contains("FireDrain"))
		{
			FireDrain(fireDrainValue);
		}
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		guardCancelTokenSource.Cancel();
		guardCancelTokenSource = new CancellationTokenSource();
		isGuard = false;
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		if (CanMakeStuck)
		{
			TryExplodeDashFlame();
			Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
		else
		{
			base.OnDamaged(hitInfo);
		}
	}

	protected override void OnDie()
	{
		base.OnDie();
		VanguardArea vanguardArea = MyArea.CastObject<VanguardArea>();
		DamageEffectAsync().Forget();
		vanguardArea.OpenDoor();
		vanguardArea.PlayClearTimeline(VisualCenter).Forget();
		ClearPlayerStagger();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { spinTwiceAttack, spinReverseAttack, grabAttack, attack3 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return base.GenerateCurrentAttackSequences();
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		List<int> list = new List<int>();
		if (IsPlayerInAttackBound(spinTwiceAttack))
		{
			list.AddRange(Enumerable.Repeat(0, spinTwiceAttack.GetPower(this)));
		}
		if (IsPlayerInAttackBound(spinReverseAttack))
		{
			list.AddRange(Enumerable.Repeat(1, spinReverseAttack.GetPower(this)));
		}
		if (IsPlayerInAttackBound(grabAttack))
		{
			list.AddRange(Enumerable.Repeat(2, grabAttack.GetPower(this)));
		}
		if (IsPlayerInAttackBound(guard))
		{
			list.AddRange(Enumerable.Repeat(3, guard.GetPower(this)));
		}
		if (IsPlayerInAttackBound(attack3))
		{
			list.AddRange(Enumerable.Repeat(4, attack3.GetPower(this)));
		}
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence> { spinTwiceAttack, spinReverseAttack, grabAttack, guard, attack3 };
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].RisePower();
			}
		}
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<AttackSequence> { spinTwiceAttack }, 
			1 => new List<AttackSequence> { spinReverseAttack }, 
			2 => new List<AttackSequence> { grabAttack }, 
			3 => new List<AttackSequence> { guard }, 
			4 => new List<AttackSequence> { attack3 }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public void FireDrain(int recovery)
	{
		HitInfo hitInfo = new HitInfo(this, "FireDrain");
		Enemy.Player.TryTakeDamage(hitInfo);
		health += recovery;
		OnChangeHealth(recovery);
	}

	private void HoldPlayerOnFrame()
	{
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = GetBoneWorldPosition("Finger_R_2_1");
		}
	}

	private async UniTaskVoid Guard(int duration)
	{
		isGuard = true;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(duration, guardCancelTokenSource.Token);
		isGuard = false;
	}
}
