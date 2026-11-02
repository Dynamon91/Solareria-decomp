using System.Threading;
using Cysharp.Threading.Tasks;

namespace SPR;

public class BurstAttackTask : AttackTask
{
	private const string AnimName = "dagger/execution";

	public const string SkillDataID1 = "burst_1";

	public const string SkillDataID2 = "burst_2";

	private string skillID1 = "burst_1";

	private string skillID2 = "burst_2";

	private static Enemy TargetEnemy => AttackTask.PlayerMover.PSM.BurstInputItem.TargetEnemy;

	public override StateOption StateOption => StateOption.None;

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
		AttackTask.PlayerMover.ClearDamagedList();
		AttackTask.PlayerMover.SetAnimation("dagger/execution_move", loop: false);
		AttackTask.PlayerMover.ApplyRelativeVelocity(AttackTask.PlayerMover.FacingDirection, AttackTask.pcp.DaggerExecutionSpeed, faceJump: false);
		AttackTask.PlayerMover.HandleAttackHitBoxFrom(TargetEnemy.VisualCenter, skillID1, 0, AttackAttribute.Default);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(Util.SecondsToFrame(AttackTask.PlayerMover.GetAnimDuration("dagger/execution_move")), token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		AttackTask.PlayerMover.ResetRelativeVelocity();
		AttackTask.PlayerMover.SetAnimation("dagger/execution_end", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(AttackTask.pcp.DaggerExecutionDelayFrame, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		int count = Game.DataManager.GetSkillData(skillID2).Coefficients.Count;
		int cycle = 24 / count;
		AttackTask.PlayerMover.ClearDamagedList();
		AttackTask.PlayerMover.HandleAttackHitBoxFrom(TargetEnemy.VisualCenter, skillID2, 0, AttackAttribute.Default);
		if (token.IsCancellationRequested)
		{
			return;
		}
		for (int i = 1; i < count; i++)
		{
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(cycle, token);
			if (token.IsCancellationRequested)
			{
				return;
			}
			AttackTask.PlayerMover.ClearDamagedList();
			AttackTask.PlayerMover.HandleAttackHitBoxFrom(TargetEnemy.VisualCenter, skillID2, i, AttackAttribute.Default);
		}
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(AttackTask.pcp.daggerExecutionPostdelayFrame, token);
	}
}
