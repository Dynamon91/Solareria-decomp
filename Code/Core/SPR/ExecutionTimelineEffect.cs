using UnityEngine;

namespace SPR;

public class ExecutionTimelineEffect : TimelineEffect
{
	private bool isInitialized;

	public override void Initialize()
	{
		base.Initialize();
		isInitialized = true;
	}

	public override void Play(bool loop = false, float time = 0f)
	{
		if (isInitialized)
		{
			Enemy targetEnemy = Game.Instance.Player.mover.PSM.BurstInputItem.TargetEnemy;
			if (targetEnemy != null)
			{
				Vector3 position = base.transform.position;
				position.x = targetEnemy.VisualCenter.x;
				base.transform.position = position;
			}
			base.Play(loop, time);
		}
	}
}
