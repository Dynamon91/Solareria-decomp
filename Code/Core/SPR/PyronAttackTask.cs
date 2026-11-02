using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public class PyronAttackTask : ParryActionTask
{
	private const string AnimName = "dagger/heavy_attack";

	public const string SkillDataID = "parry_action";

	public const string DefaultFlameID = "pyron_flame";

	public override StateOption StateOption => StateOption.Immortal | StateOption.GravityFree | StateOption.BlockStuck;

	public bool InProgress { get; private set; }

	public PyronAttackTask()
		: base("parry_action", "pyron_flame")
	{
	}

	protected override void OnFixedUpdate()
	{
	}

	protected override async UniTask AttackProgressAsync(List<IDamageable> damaged, CancellationToken token)
	{
		AttackTask.PlayerMover.PSM.SetStateOptionActive(StateOption, active: true);
		AttackTask.PlayerMover.ResetRelativeVelocity();
		AttackTask.PlayerMover.SetAnimation("dagger/heavy_attack", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(AttackTask.pcp.parryActionMovingFrame.Predelay, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Parrying_Attack_X");
		int num = (AttackTask.PlayerMover.IsFacingRight ? 1 : (-1));
		AttackTask.PlayerMover.ApplyRelativeVelocity(num * AttackTask.pcp.parryActionSpeed, 0f, faceJump: false);
		FirstAttackAsync(damaged, token).Forget();
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(AttackTask.pcp.parryActionMovingFrame.Action, token);
		if (!token.IsCancellationRequested)
		{
			AttackTask.PlayerMover.ResetRelativeVelocity();
			if (damaged.Count != 0)
			{
				SecondAttackAsync(damaged).Forget();
			}
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(AttackTask.pcp.parryActionMovingFrame.Postdelay, token);
			AttackTask.PlayerMover.PSM.SetStateOptionActive(StateOption, active: false);
		}
	}

	private async UniTask FirstAttackAsync(List<IDamageable> damaged, CancellationToken token)
	{
		for (int i = 0; i < AttackTask.pcp.parryActionMovingFrame.Action; i++)
		{
			if (damaged.Count != 0)
			{
				break;
			}
			AttackTask.PlayerMover.ClearDamagedList();
			AttackTask.PlayerMover.HandleAttackHitBox(SkillID, 0, AttackAttribute.Default);
			damaged.AddRange(AttackTask.PlayerMover.Damaged.Where((IDamageable damageable) => (damageable.HitType & (HitType.Creature | HitType.Boss)) != 0));
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return;
			}
		}
		if (damaged.Count != 0)
		{
			AttackTask.PlayerController.PlayerPart.OnPyronActionAttack(damaged, isHeavyPyron: false);
		}
		Vector2 vector = ((damaged.Count != 0 && damaged[0] is Character character) ? character.VisualCenter : AttackTask.PlayerMover.VisualCenter);
		PoolableTimeLimitObject poolableTimeLimitObject = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Dagger_HeavyAttack_Hit.prefab", vector);
		poolableTimeLimitObject.Initialize("VFX/Dagger_HeavyAttack_Hit.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
	}

	private async UniTask SecondAttackAsync(List<IDamageable> damaged)
	{
		InProgress = true;
		await HeatFlame.ShootFlameAsync(damaged, FlameID);
		InProgress = false;
	}
}
