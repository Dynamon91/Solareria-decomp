using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

public class GroundAttackTask : AttackTask
{
	private float attackEndTime;

	private const string SkillDataID = "attack";

	private const int TotalAttackCount = 4;

	public bool IsLastAttackPending { get; private set; }

	public override StateOption StateOption => StateOption.None;

	protected virtual string SkillID => "attack";

	protected virtual int MaxAttackCount => 4;

	public int AttackIndex { get; private set; }

	public void SetAttackEndTime(float time)
	{
		AttackIndex = 0;
		attackEndTime = time;
	}

	protected override void OnFixedUpdate()
	{
	}

	protected override async UniTask AttackAsync(CancellationToken token)
	{
		int attackIndex = AttackIndex;
		Vector3 enterPosition = AttackTask.PlayerMover.transform.position;
		SetAttackIndex();
		if (AttackIndex < 3)
		{
			MoveBeforeAttack();
		}
		int sign = AttackTask.PlayerController.horizontal.GetSign();
		AttackTask.PlayerMover.ApplyRelativeVelocity((AttackIndex < 3) ? (sign * AttackTask.pcp.daggerGroundSpeed) : 0, 0f, faceJump: false);
		if (attackIndex == 0 && attackIndex == AttackIndex)
		{
			AttackTask.PlayerMover.SetAnimation("idle", loop: false);
		}
		string animName = string.Format("{0}_{1}", "attack", AttackIndex + 1);
		AttackFrame frame = AttackTask.pcp.daggerGroundFrames[AttackIndex];
		float timeScale = (float)Util.SecondsToFrame(AttackTask.PlayerMover.GetAnimDuration(animName)) / (float)frame.All;
		AttackTask.PlayerMover.SetAnimation(animName, loop: false, timeScale);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frame.Predelay, token);
		if (token.IsCancellationRequested)
		{
			AttackTask.PlayerMover.DisableVFX(animName);
			return;
		}
		Game.Instance.SoundManager.PlayAttackEvent("event:/SFX/Player/Combat/SFX_Player_Attack_Ground", AttackIndex);
		int attackIndex2 = AttackIndex;
		if (attackIndex2 < 2)
		{
			await ApplyDamageAsync(frame.Action, frame.DamageFrames, token);
		}
		else if (attackIndex2 == 2)
		{
			await ApplyDamageWithKnockBackAsync(frame.Action, frame.DamageFrames, token);
		}
		else
		{
			IsLastAttackPending = true;
			GroundAttackTask groundAttackTask = this;
			Vector2 center = enterPosition;
			List<int> damageFrames = frame.DamageFrames;
			groundAttackTask.ApplyDamageInWorldSpaceAsync(center, damageFrames[damageFrames.Count - 1], frame.DamageFrames).GetAwaiter().OnCompleted(delegate
			{
				IsLastAttackPending = false;
			});
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frame.Action, token);
		}
		if (!token.IsCancellationRequested)
		{
			AttackTask.PlayerMover.HorizontalMove(0f);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frame.Postdelay, token);
			if (!token.IsCancellationRequested)
			{
				attackEndTime = Time.time;
			}
		}
	}

	private async UniTask<List<IDamageable>> ApplyDamageAsync(int totalFrame, ICollection<int> damageFrames, CancellationToken token = default(CancellationToken))
	{
		List<IDamageable> damaged = new List<IDamageable>();
		for (int i = 0; i < totalFrame; i++)
		{
			if (damageFrames.Contains(i))
			{
				ApplyDamage_Internal(damaged, AttackIndex);
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return damaged;
			}
		}
		if (damageFrames.Contains(totalFrame))
		{
			ApplyDamage_Internal(damaged, AttackIndex);
		}
		return damaged;
	}

	protected virtual void ApplyDamage_Internal(List<IDamageable> damaged, int index)
	{
		AttackTask.PlayerMover.ClearDamagedList();
		AttackTask.PlayerMover.HandleAttackHitBox(SkillID, index, AttackAttribute.Default);
		foreach (IDamageable item in AttackTask.PlayerMover.Damaged)
		{
			if (!damaged.Contains(item))
			{
				damaged.Add(item);
			}
		}
	}

	private async UniTask<List<IDamageable>> ApplyDamageWithKnockBackAsync(int totalFrame, ICollection<int> damageFrames, CancellationToken token = default(CancellationToken))
	{
		List<IDamageable> damaged = new List<IDamageable>();
		for (int i = 0; i < totalFrame; i++)
		{
			if (damageFrames.Contains(i))
			{
				ApplyDamage();
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return damaged;
			}
		}
		if (damageFrames.Contains(totalFrame))
		{
			ApplyDamage();
		}
		return damaged;
		void ApplyDamage()
		{
			AttackTask.PlayerMover.ClearDamagedList();
			AttackTask.PlayerMover.HandleAttackHitBox(SkillID, AttackIndex, AttackAttribute.Default);
			foreach (IDamageable item in AttackTask.PlayerMover.Damaged)
			{
				if (!damaged.Contains(item))
				{
					damaged.Add(item);
					if (item is Creature creature)
					{
						Vector2 direction = new Vector2((AttackTask.PlayerMover.VisualCenter.x - creature.VisualCenter.x).GetSign(), 0f);
						creature.KnockBackAsync(10, direction).Forget();
					}
				}
			}
		}
	}

	private async UniTask<List<IDamageable>> ApplyDamageInWorldSpaceAsync(Vector2 center, int totalFrame, ICollection<int> damageFrames)
	{
		List<IDamageable> damaged = new List<IDamageable>();
		string collName = SkillID + "_4";
		for (int i = 0; i < totalFrame; i++)
		{
			if (damageFrames.Contains(i))
			{
				ApplyDamage();
			}
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		if (damageFrames.Contains(totalFrame))
		{
			ApplyDamage();
		}
		return damaged;
		void ApplyDamage()
		{
			AttackTask.PlayerMover.ClearDamagedList();
			AttackTask.PlayerMover.HandleAttackHitBoxFrom(center, SkillID, AttackIndex, AttackAttribute.Default, collName);
			foreach (IDamageable item in AttackTask.PlayerMover.Damaged)
			{
				if (!damaged.Contains(item))
				{
					damaged.Add(item);
					if (item is Creature creature)
					{
						Vector2 direction = new Vector2((creature.VisualCenter.x - AttackTask.PlayerMover.VisualCenter.x).GetSign(), 0f);
						creature.KnockBackAsync(10, direction).Forget();
					}
				}
			}
		}
	}

	private void SetAttackIndex()
	{
		float num = Util.FrameToSeconds(AttackTask.pcp.ContinuousAttackAcceptFrame);
		bool flag = !AttackTask.PlayerMover.LastHitInfo.AttackAttribute.HasFlag(AttackAttribute.Continuous) && Time.time - AttackTask.PlayerMover.LastHitTime < num;
		bool flag2 = Time.time - attackEndTime > num || flag;
		AttackIndex = ((!flag2) ? (AttackIndex + 1) : 0);
		AttackIndex %= MaxAttackCount;
	}

	private void MoveBeforeAttack()
	{
		int sign = AttackTask.PlayerController.horizontal.GetSign();
		if (sign != 0)
		{
			if ((AttackTask.PlayerMover.IsFacingRight ? 1 : (-1)) != sign)
			{
				AttackTask.PlayerMover.Flip(sign);
			}
			else
			{
				AttackTask.PlayerMover.ApplyRelativeVelocity(sign * AttackTask.pcp.MovingOnAttackSpeed, 0f, faceJump: false);
			}
		}
	}
}
