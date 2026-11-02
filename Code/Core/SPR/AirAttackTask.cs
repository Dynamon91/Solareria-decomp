using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public class AirAttackTask : AttackTask
{
	private const string SkillDataID = "dagger/attack_air";

	private readonly AttackParameter firstAttackParameter = new AttackParameter("dagger/attack_air_1", "dagger/attack_air", 0, (AttackTask.pcp.daggerAirFrame.Predelay + 1, AttackTask.pcp.daggerAirFrame.All - AttackTask.pcp.daggerAirFrame.Postdelay), AttackAttribute.Default);

	public override StateOption StateOption => StateOption.GravityFree;

	private static Vector2 FirstVelocity => new Vector2(playerDirection, 1f) * AttackTask.pcp.DaggerAirFirstVelocity;

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
		AttackTask.PlayerMover.ApplyRelativeVelocity(FirstVelocity, faceJump: false);
		Game.Instance.SoundManager.PlayAttackEvent("event:/SFX/Player/Combat/SFX_Player_Attack_Ground", 0);
		AttackTask.PlayerMover.SetAnimation(firstAttackParameter.AnimName, loop: false);
		AttackTask.PlayerMover.ClearDamagedList();
		int currentFrame = 0;
		(int min, int max) range = firstAttackParameter.AttackFrameRange;
		while (!AttackTask.PlayerMover.IsAnimComplete(0))
		{
			if (currentFrame >= range.min && currentFrame <= range.max)
			{
				AttackTask.PlayerMover.HandleAttackHitBox(firstAttackParameter.DataID, firstAttackParameter.Index, firstAttackParameter.AttackAttribute);
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return;
			}
			currentFrame++;
		}
		AttackTask.PlayerMover.SetAnimation("jump_down", loop: false);
	}
}
