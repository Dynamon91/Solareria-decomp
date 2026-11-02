using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

public class HyperBurstAttackTask : AttackTask
{
	public const string SkillDataID1 = "burst_heat_1";

	public const string SkillDataID2 = "burst_heat_2";

	private const int WaitFrame = 45;

	private const int MoveFrame = 11;

	private const AttackAttribute Attribute = AttackAttribute.Default | AttackAttribute.HyperBurst;

	private string skillID1 = "burst_heat_1";

	private string skillID2 = "burst_heat_2";

	public override StateOption StateOption => StateOption.None;

	private static float BurstDistanceDelta => AttackTask.PlayerController.HeatEffectController.BurstDistanceDelta;

	private static IReadOnlyList<int> DamageFrames => AttackTask.PlayerController.HeatEffectController.BurstDamageFrames;

	private static Enemy TargetEnemy => AttackTask.PlayerMover.PSM.HyperBurstInputItem.TargetEnemy;

	public void SetSkillID(string firstID, string secondID)
	{
		skillID1 = firstID;
		skillID2 = secondID;
	}

	protected override void OnFixedUpdate()
	{
	}

	protected override async UniTask AttackAsync(CancellationToken token)
	{
		PlayerStat.AddHeatGauge(-PlayerHeat.GetHeatSkillUsage(HeatSkillType.HyperBurst));
		Game.Instance.RuntimeData.AddHadHyperBurst();
		DamageAsync().Forget();
		AttackTask.PlayerMover.SetAnimation("burst", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(45, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		float speed = (Mathf.Abs(TargetEnemy.VisualBottom.x - AttackTask.PlayerMover.VisualBottom.x) + BurstDistanceDelta) / (11f * Time.fixedDeltaTime);
		AttackTask.PlayerMover.ApplyRelativeVelocity(AttackTask.PlayerMover.FacingDirection, speed, faceJump: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(11, token);
		if (!token.IsCancellationRequested)
		{
			AttackTask.PlayerMover.ResetRelativeVelocity();
			await UniTaskExtension.WaitUntilOrCancel(() => AttackTask.PlayerMover.IsAnimComplete(0), token);
		}
	}

	private async UniTask DamageAsync()
	{
		int frame = 0;
		int index = 0;
		int firstAttackCount = Game.DataManager.GetSkillData(skillID1).Coefficients.Count;
		int secondAttackCount = Game.DataManager.GetSkillData(skillID2).Coefficients.Count;
		while (true)
		{
			if (!IsValid())
			{
				return;
			}
			if (DamageFrames[index] == frame)
			{
				AttackTask.PlayerMover.ClearDamagedList();
				AttackTask.PlayerMover.HandleAttackHitBoxFrom(TargetEnemy.VisualCenter, skillID1, index, AttackAttribute.Default | AttackAttribute.HyperBurst, null, random: false);
				index++;
				if (index >= firstAttackCount)
				{
					break;
				}
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			frame++;
		}
		index = 0;
		while (true)
		{
			if (!IsValid())
			{
				return;
			}
			if (DamageFrames[index + firstAttackCount] == frame)
			{
				AttackTask.PlayerMover.ClearDamagedList();
				AttackTask.PlayerMover.HandleAttackHitBoxFrom(TargetEnemy.VisualCenter, skillID2, index, AttackAttribute.Default | AttackAttribute.HyperBurst, null, random: false);
				index++;
				if (index >= secondAttackCount)
				{
					break;
				}
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			frame++;
		}
		AttackTask.PlayerMover.GenerateDownImpulse();
		TargetEnemy.AddHyperBurstExecutionEffect();
	}
}
