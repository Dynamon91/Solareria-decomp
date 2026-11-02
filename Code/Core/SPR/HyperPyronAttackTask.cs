using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class HyperPyronAttackTask : ParryActionTask
{
	private const string DebuffDataID = "parry_heavy_attack_debuff";

	public const string SkillDataID = "parry_heavy_attack";

	public const string DefaultFlameID = "pyron_heavy_flame";

	private AttackAttribute currentAttribute;

	private Enemy targetEnemy;

	public override StateOption StateOption => StateOption.Immortal | StateOption.GravityFree | StateOption.BlockStuck;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public HyperPyronAttackTask()
		: base("parry_heavy_attack", "pyron_heavy_flame")
	{
	}

	protected override void OnFixedUpdate()
	{
	}

	private bool TryEnemySetGroggy()
	{
		if (targetEnemy.StateMachine.CurrentState is StiffState stiffState)
		{
			if (!targetEnemy.IsDead)
			{
				return stiffState.TryChangeGroggy();
			}
			return false;
		}
		return false;
	}

	private async UniTaskVoid ResetEnemyState()
	{
		if (!(targetEnemy.StateMachine.CurrentState is StiffState stiffState))
		{
			return;
		}
		stiffState.ResetTimeScale();
		if (targetEnemy.IsDead || !stiffState.TryChangeGroggy())
		{
			await UniTask.WaitUntil(() => targetEnemy.IsAnimComplete(0));
			if (!targetEnemy.IsDead)
			{
				targetEnemy.StateMachine.ChangeState(targetEnemy.DefaultState);
			}
		}
	}

	protected override async UniTask AttackProgressAsync(List<IDamageable> damaged, CancellationToken token)
	{
		PlayerStat.AddHeatGauge(-PlayerHeat.GetHeatSkillUsage(HeatSkillType.HyperPyronAction));
		Game.Instance.RuntimeData.AddHadHyperPyronAction();
		targetEnemy = TargetEnemies[0];
		HeatFlame.ShootFlameAsync(targetEnemy, FlameID).Forget();
		PlaySFXAsync(HeatFlame.GetDelayFrame(FlameID)).Forget();
		currentAttribute = AttackAttribute.Default | AttackAttribute.HeavyAttack | AttackAttribute.FixedDamage;
		AttackTask.PlayerMover.PSM.SetStateOptionActive(StateOption, active: true);
		AttackTask.PlayerMover.ResetRelativeVelocity();
		AttackTask.PlayerMover.SetSkin("lance");
		AttackTask.PlayerMover.SetAnimation("parry_heavy_attack", loop: false);
		AttackFrame frame = AttackTask.pcp.parryHeavyAttackFrame;
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frame.Predelay, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		StiffState newState = new StiffState(targetEnemy);
		targetEnemy.StateMachine.ChangeState(newState);
		AttackTask.PlayerMover.ClearDamagedList();
		AttackTask.PlayerMover.Attack(new PlayerMover.AttackParameter(targetEnemy, SkillID, 0, currentAttribute), damaged);
		SoundManager.PlayOneShot("event:/SFX/Player/Skill/SFX_Player_HyperPyron_Charge");
		AttackTask.PlayerMover.GenerateDownImpulse();
		AttackTask.PlayerController.PlayerPart.OnPyronActionAttack(damaged, isHeavyPyron: true);
		MovePlayerAsync(token).Forget();
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frame.Action, token);
		if (token.IsCancellationRequested)
		{
			ResetEnemyState().Forget();
			return;
		}
		AttackTask.PlayerMover.ClearDamagedList();
		AttackTask.PlayerMover.Attack(new PlayerMover.AttackParameter(targetEnemy, SkillID, 1, currentAttribute));
		AttackTask.PlayerMover.GenerateDownImpulse();
		AttackTask.PlayerController.HeatEffectController.FindHeatEffect("heavy_pyron_shield").TryInvoke();
		if (AttackTask.PlayerMover.Damaged.Any((IDamageable d) => d == targetEnemy))
		{
			ApplyEffect();
		}
		TryEnemySetGroggy();
		for (int i = 0; i < frame.Postdelay; i++)
		{
			if (token.IsCancellationRequested)
			{
				ResetEnemyState().Forget();
				return;
			}
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		AttackTask.PlayerMover.PSM.SetStateOptionActive(StateOption, active: false);
		ResetEnemyState().Forget();
		AttackTask.PlayerMover.SetSkin("player");
	}

	private async UniTask MovePlayerAsync(CancellationToken token)
	{
		float num = targetEnemy.transform.position.x - AttackTask.PlayerMover.FacingDirection.ToVector2().x * 8f - AttackTask.PlayerMover.VisualCenter.x;
		int frameCnt = 5;
		float x = num / 5f / Time.fixedDeltaTime;
		AttackTask.PlayerMover.ApplyRelativeVelocity(new Vector2(x, 0f), faceJump: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frameCnt, token);
		if (!token.IsCancellationRequested)
		{
			AttackTask.PlayerMover.ResetRelativeVelocity();
		}
	}

	private void ApplyEffect()
	{
		int duration = Game.DataManager.GetPlayerBurnData("parry_heavy_attack_debuff").Duration;
		float debuffRate = 0.5f;
		targetEnemy.AddDefenseDebuff(duration, debuffRate);
	}

	private async UniTask PlaySFXAsync(int delayFrame)
	{
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(delayFrame);
		SoundManager.PlayOneShot("event:/SFX/Player/Skill/SFX_Player_HyperPyron_Flame");
	}
}
