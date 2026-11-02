using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Field;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR;

public class EarthKeeper : HighCreature, IEventListener
{
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference armorDestroyedEvent;

	[Header("땅 파기")]
	[SerializeField]
	private float digDepth;

	[SerializeField]
	private float digChanceRise;

	[Header("Sequences")]
	[SerializeField]
	private GenericBossAttackSequence<EarthKeeper> attack;

	[SerializeField]
	private GenericAttackSequence<EarthKeeper> dig;

	[SerializeField]
	private GenericBossAttackSequence<EarthKeeper> smash;

	private bool isImmortal;

	private int digChance;

	public override string Name => "EarthKeeper";

	public override string GroggyEndAnim => string.Empty;

	public override bool CanMakeStuck => IsArmored;

	public override bool IsDefenseless
	{
		get
		{
			if (IsInGroggyState && IsInExecutionRange())
			{
				return !CanMakeStuck;
			}
			return false;
		}
	}

	protected override Vector2 BurstIconPosition => VisualCenter + Vector2.down * Bounds.size.y / 2f;

	private bool IsArmored { get; set; }

	protected override int OriginDef
	{
		get
		{
			if (!IsArmored)
			{
				return base.EnemyStatData.Def[0];
			}
			return base.EnemyStatData.Def[1];
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		LazySingleton<MessageSystem>.Instance.Subscribe<LeverPullEvent>(this);
		IsArmored = true;
		digChance = 33;
		TimelineBeginActions.Add("Dig", delegate(int duration)
		{
			Dig(Util.FrameToSeconds(duration));
		});
		TimelineBeginActions.Add("MovePlayerPos", delegate
		{
			MoveToPlayerPos();
		});
		TimelineBeginActions.Add("Rise", delegate(int duration)
		{
			Rise(Util.FrameToSeconds(duration));
		});
		TimelineBeginActions.Add("Immortal", delegate
		{
			isImmortal = true;
		});
		TimelineEndActions.Add("Immortal", delegate
		{
			isImmortal = false;
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (isImmortal)
		{
			return false;
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			BrokenArmor().Forget();
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override bool IsPlayerInSightBound()
	{
		bool num = base.IsPlayerInSightBound();
		if (num)
		{
			LazySingleton<MessageSystem>.Instance.Publish(new LegendaryBoundsActivateEvent());
		}
		return num;
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { dig, smash, attack };
	}

	protected override List<AttackSequence> GetRandomSequence()
	{
		List<int> list = new List<int>();
		if (IsArmored)
		{
			if (IsPlayerInAttackBound(attack))
			{
				list.AddRange(Enumerable.Repeat(0, attack.GetPower(this)));
			}
			if (IsPlayerInAttackBound(smash))
			{
				list.AddRange(Enumerable.Repeat(1, smash.GetPower(this)));
			}
			return list[CryptoRandom.Range(0, list.Count)] switch
			{
				0 => new List<AttackSequence> { attack }, 
				1 => new List<AttackSequence> { smash }, 
				_ => new List<AttackSequence>(), 
			};
		}
		int count = (100 - digChance) / 2;
		if (IsPlayerInAttackBound(attack))
		{
			list.AddRange(Enumerable.Repeat(0, count));
		}
		if (IsPlayerInAttackBound(smash))
		{
			list.AddRange(Enumerable.Repeat(1, count));
		}
		if (IsPlayerInAttackBound(dig))
		{
			list.AddRange(Enumerable.Repeat(2, digChance));
		}
		switch (list[CryptoRandom.Range(0, list.Count)])
		{
		case 0:
			digChance += Mathf.RoundToInt(digChanceRise);
			return new List<AttackSequence> { attack };
		case 1:
			digChance += Mathf.RoundToInt(digChanceRise);
			return new List<AttackSequence> { smash };
		case 2:
			digChance = 1;
			return new List<AttackSequence> { dig };
		default:
			return new List<AttackSequence>();
		}
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		SetGroggyAnimation().Forget();
	}

	protected override void OnExitGroggyState()
	{
		base.OnExitGroggyState();
		SetAnimation("idle");
	}

	protected override void OnChangeHealth(int delta)
	{
		base.OnChangeHealth(delta);
		HighCreature.BossBoard.UpdateStunPoint(base.CurrentStun);
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		if (!IsArmored || hitInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck) || hitInfo.AttackAttribute.HasFlag(AttackAttribute.FixedDamage))
		{
			base.OnDamaged(hitInfo);
			return;
		}
		TryExplodeDashFlame();
		if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.ParryGroggy))
		{
			LazySingleton<MessageSystem>.Instance.Publish(new EarthGuardStuckTutorialEvent());
			Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
	}

	protected override void OnDie()
	{
		base.OnDie();
		MyArea.CastObject<LegendaryArea>().PlayClearTimeline(VisualCenter).Forget();
		DamageEffectAsync().Forget();
		Game.Instance.SaveData.WanderingData.SetTutorialDone(ActionName.EnemyGuard);
		LazySingleton<MessageSystem>.Instance.UnSubscribe<LeverPullEvent>(this);
	}

	private async UniTaskVoid SetGroggyAnimation()
	{
		SetAnimation("groggy_start", loop: false);
		await UniTask.WaitWhile(() => !IsAnimComplete(0));
		if (!IsDead)
		{
			AddAnimation("groggy_loop");
		}
	}

	private void Dig(float seconds)
	{
		base.SkeletonRenderer.transform.DOLocalMoveY(0f - digDepth, seconds).SetEase(Ease.OutCubic);
	}

	private void Rise(float seconds)
	{
		SetSkin("earthkeeper");
		RemoveAnimation(1);
		IsArmored = true;
		base.SkeletonRenderer.transform.DOLocalMoveY(0f, seconds).SetEase(Ease.Linear);
	}

	private void MoveToPlayerPos()
	{
		Vector3 vector = new Vector3(Enemy.Player.VisualBottom.x, VisualBottom.y);
		base.transform.position = (CanReach(vector) ? vector : ((Vector3)VisualBottom));
	}

	private async UniTaskVoid BrokenArmor()
	{
		if (IsArmored)
		{
			ReturnPaleBlow();
			DisableVFXAll();
			base.StateMachine.ChangeState(new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration(base.BounceAnim)), useVFX: false, resetStun: false));
			SetAnimation("broken", loop: false, 1f, 1);
			Enemy.SoundManager.PlayOneShotAttached(armorDestroyedEvent, base.transform);
			IsArmored = false;
			await UniTask.WaitUntil(() => IsAnimComplete(1, "broken"));
			SetSkin("groggy");
		}
	}

	public void OnEvent(global::Library.Event e)
	{
		if (!IsDead && e is LeverPullEvent)
		{
			PullLever().Forget();
		}
	}

	private async UniTaskVoid PullLever()
	{
		if ((!(base.StateMachine.CurrentState is AttackState attackState) || attackState.CurrentSequence != dig) && !(base.StateMachine.CurrentState is GroggyState))
		{
			await UniTask.WaitWhile(() => base.StateMachine.CurrentState is AttackState);
			ReturnPaleBlow();
			DisableVFXAll();
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { dig }, forceAttack: true));
		}
	}
}
