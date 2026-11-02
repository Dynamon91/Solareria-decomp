using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public class SecondAirAttackTask : AttackTask
{
	private const string SkillDataID = "dagger/attack_air";

	public override StateOption StateOption => StateOption.GravityFree;

	private static Vector2 SecondVelocity => new Vector2(playerDirection, 1f) * AttackTask.pcp.DaggerAirSecondVelocity;

	private static int playerDirection
	{
		get
		{
			if (!AttackTask.PlayerMover.IsFacingRight)
			{
				return -1;
			}
			return 1;
		}
	}

	protected override void OnFixedUpdate()
	{
	}

	protected override async UniTask AttackAsync(CancellationToken token)
	{
		Game.Instance.Player.PlayerStat.GetStat<Stamina>().AddStamina(0f - AttackTask.pcp.airAttackStamina);
		AttackTask.PlayerMover.ResetRelativeVelocity();
		AttackTask.PlayerMover.PSM.SetStateOptionActive(StateOption.GravityFree, active: true);
		AttackTask.PlayerMover.SetAnimation("dagger/attack_air_2_start", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(Util.SecondsToFrame(AttackTask.PlayerMover.GetAnimDuration("dagger/attack_air_2_start")), token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		Game.Instance.SoundManager.PlayAttackEvent("event:/SFX/Player/Combat/SFX_Player_Attack_Ground", 0);
		AttackTask.PlayerMover.PSM.SetStateOptionActive(StateOption.GravityFree, active: false);
		AttackTask.PlayerMover.ApplyRelativeVelocity(SecondVelocity, faceJump: false);
		string animationName = "dagger/attack_air_2_loop";
		AttackTask.PlayerMover.SetAnimation(animationName);
		int currentFrame = 0;
		int cycle = AttackTask.pcp.daggerAirAttackCycle;
		while (!AttackTask.PlayerMover.IsOnGround)
		{
			currentFrame %= cycle;
			if (currentFrame == 0)
			{
				AttackTask.PlayerMover.ClearDamagedList();
				AttackTask.PlayerMover.HandleAttackHitBox("dagger/attack_air", 1, AttackAttribute.Default);
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return;
			}
			currentFrame++;
		}
		AttackTask.PlayerMover.ResetRelativeVelocity();
		AttackTask.PlayerMover.SetAnimation("dagger/attack_air_2_end", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(AttackTask.pcp.daggerAirSecondPostdelay, token);
	}
}
