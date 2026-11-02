using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class PlayerBlitzState : PlayerAsyncState
{
	private float chargeDuration;

	private bool isFlameActive;

	private readonly GroggyFlame groggyFlame;

	private bool IsHeavyBlitzAble => chargeDuration >= PlayerAsyncState.pcp.HeavyBlitzChargeDuration;

	public PlayerBlitzState(PlayerStateMachine psm)
		: base(psm)
	{
		groggyFlame = new GroggyFlame();
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockStaminaRegain | StateOption.BlockHeavyCharge, active: true);
		PlayerMover.ResetRelativeVelocity();
		isFlameActive = false;
		psm.FirstAirAttackInputItem.ResetTime();
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await ChargeAsync(token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		psm.SetStateOptionActive(StateOption.BlockStaminaRegain, active: false);
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		await BlitzAttackAsync(token);
		if (token.IsCancellationRequested || !IsHeavyBlitzAble)
		{
			return;
		}
		int targetFrameCount = Util.SecondsToFrame(PlayerMover.GetAnimDuration("blitz_heavy_up"));
		int num = (PlayerMover.IsFacingRight ? 1 : (-1));
		Vector2 velocity = PlayerAsyncState.pcp.heavyBlitzVelocity;
		velocity.x *= num;
		PlayerMover.ApplyRelativeVelocity(velocity, faceJump: true);
		PlayerMover.SetAnimation("blitz_heavy_up", loop: false);
		await WaitGuaranteedFramesOnMovingAsync(targetFrameCount, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.SetAnimation("blitz_heavy_hold", loop: false);
		targetFrameCount = Util.SecondsToFrame(PlayerMover.GetAnimDuration("blitz_heavy_hold"));
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(targetFrameCount, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.SetAnimation("blitz_heavy_down", loop: false);
		PlayerMover.AddAnimation("blitz_heavy_down_loop");
		PlayerMover.ApplyRelativeVelocity(velocity.x / 4f, 0f - velocity.y, faceJump: false);
		await UniTaskExtension.WaitUntilOrCancel(IsOnGround, token, PlayerLoopTiming.LastFixedUpdate);
		if (token.IsCancellationRequested)
		{
			return;
		}
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.SetAnimation("blitz_heavy_end", loop: false, 1f, 0, 0f, vfxEnable: false);
		UniTask task = HeavyBlitzAttackAsync(token);
		while (!token.IsCancellationRequested && task.Status == UniTaskStatus.Pending)
		{
			if (!isFlameActive && TryGetGroggyEnemy(psm.Mover.Damaged, out var enemy))
			{
				isFlameActive = true;
				groggyFlame.FlameAttackAsync(enemy, new Vector2(enemy.VisualCenter.x, PlayerMover.VisualBottom.y), enemy.Bounds.size).Forget();
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
		}
	}

	private async UniTask WaitGuaranteedFramesOnMovingAsync(int targetFrameCount, CancellationToken token)
	{
		Vector2 startPosition = PlayerMover.VisualCenter;
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if ((PlayerMover.VisualCenter - startPosition).magnitude > 1f)
			{
				break;
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
		}
		int num = 1;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(targetFrameCount - num, token);
	}

	private async UniTask ChargeAsync(CancellationToken token)
	{
		PlayerMover.SetAnimation("blitz_charge", loop: false);
		PlayerMover.AddAnimation("blitz_charge_complete", loop: false);
		PlayerMover.AddAnimation("blitz_charge_complete_loop");
		float staminaUsage = 0f;
		float staminaPerFrame = PlayerAsyncState.pcp.BlitzChargeStaminaPerSecond * Time.fixedDeltaTime;
		chargeDuration = 0f;
		while (true)
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			if (PlayerState.PlayerStat.GetStat<Stamina>().Current > 0f && chargeDuration < PlayerAsyncState.pcp.HeavyBlitzChargeDuration)
			{
				staminaUsage += staminaPerFrame;
				PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(0f - staminaPerFrame);
			}
			if (PlayerState.PlayerStat.GetStat<Stamina>().Current == 0f)
			{
				break;
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
		}
		if (staminaUsage < PlayerAsyncState.pcp.BlitzDefaultStamina)
		{
			PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(0f - PlayerAsyncState.pcp.BlitzDefaultStamina + staminaUsage);
		}
	}

	private async UniTask BlitzAttackAsync(CancellationToken token)
	{
		PlayerMover.SetAnimation(IsHeavyBlitzAble ? "blitz_heavy_start" : "blitz", loop: false);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.blitzPredelayFrame, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		if (chargeDuration < 1f)
		{
			Mathf.Lerp(0.4f, 1f, chargeDuration);
		}
		int frameCount = 0;
		while (!token.IsCancellationRequested && !PlayerMover.IsAnimComplete(0))
		{
			if (PlayerAsyncState.pcp.BlitzAttackFrames.Contains(frameCount))
			{
				PlayerMover.ClearDamagedList();
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
			frameCount++;
		}
	}

	private async UniTask HeavyBlitzAttackAsync(CancellationToken token)
	{
		int currentFrame = 0;
		int attackCount = 0;
		while (!token.IsCancellationRequested && currentFrame < PlayerAsyncState.pcp.heavyBlitzActionFrame)
		{
			if (PlayerAsyncState.pcp.HeavyBlitzAttackFrames.Contains(currentFrame))
			{
				PlayerMover.ClearDamagedList();
				PlayerMover.HandleAttackHitBox("heavy_blitz", attackCount, AttackAttribute.Default);
				attackCount++;
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
			currentFrame++;
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree | StateOption.BlockStaminaRegain | StateOption.BlockHeavyCharge, active: false);
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.DisableVFX("blitz_charge");
	}

	private bool TryGetGroggyEnemy(IEnumerable<IDamageable> damageables, out Enemy enemy)
	{
		enemy = null;
		foreach (IDamageable damageable in damageables)
		{
			if (damageable is Enemy enemy2 && !(enemy2.StateMachine.CurrentState.GetType() != typeof(GroggyState)))
			{
				enemy = enemy2;
				return true;
			}
		}
		return false;
	}

	private bool IsOnGround()
	{
		if (PlayerMover.RelativeVelocity.y < 1f)
		{
			return Physics2D.OverlapCircle(PlayerMover.VisualBottom, 0.125f, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		}
		return false;
	}
}
