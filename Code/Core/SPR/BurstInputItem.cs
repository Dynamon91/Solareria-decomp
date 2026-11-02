using Library;
using UnityEngine;

namespace SPR;

public class BurstInputItem : InputItem
{
	private readonly int distanceStandard;

	private readonly float earlyAttackInputTime;

	private static FieldController FieldController => Game.Instance.FieldController;

	public Enemy CachedTarget { get; private set; }

	public Enemy TargetEnemy { get; set; }

	public BurstInputItem(IState state)
		: base(state)
	{
		earlyAttackInputTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
		distanceStandard = InputItem.pcp.ExecutionEnabledDistance;
	}

	public override void UpdateInput(float currentTime)
	{
		if (!InputItem.PlayerMover.IsOnGround)
		{
			return;
		}
		float num = distanceStandard + 1;
		Enemy enemy = null;
		foreach (Enemy enemy2 in FieldController.Enemies)
		{
			if (!enemy2.IsNull() && enemy2.IsDefenseless && !InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
			{
				float num2 = Mathf.Abs(enemy2.NearestEdgePoint.x - InputItem.PlayerMover.VisualBottom.x);
				if ((object)enemy == null || !(num2 >= num))
				{
					enemy = enemy2;
					num = num2;
				}
			}
		}
		bool flag = (object)CachedTarget != null;
		if (flag && InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			DebugLog.GeneralLog($"{CachedTarget} 버스트 불가");
			CachedTarget.ActivateBurstIcon(value: false);
			CachedTarget = null;
			return;
		}
		if (num > (float)distanceStandard)
		{
			if (flag)
			{
				DebugLog.GeneralLog($"{CachedTarget} 버스트 불가");
				CachedTarget.ActivateBurstIcon(value: false);
				CachedTarget = null;
			}
			return;
		}
		if (!flag || CachedTarget != enemy)
		{
			if (flag)
			{
				CachedTarget.ActivateBurstIcon(value: false);
			}
			CachedTarget = enemy;
			enemy.ActivateBurstIcon(value: true);
			DebugLog.GeneralLog($"{CachedTarget} 버스트 가능");
		}
		if (InputItem.PlayerController.action)
		{
			base.InputTime = currentTime;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (GetSuccessDeltaTime(currentTime) <= earlyAttackInputTime)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyAttackInputTime)
		{
			return false;
		}
		return CachedTarget?.IsDefenseless ?? false;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		TargetEnemy = CachedTarget;
		TargetEnemy.ActivateBurstIcon(value: true, isClicked: true);
	}
}
