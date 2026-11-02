using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

[Serializable]
public class GroundFlameEffect : HeatEffect
{
	private int durationFrame;

	private int cycleFrame;

	private int waitFrame;

	private Vector2 startPosition;

	public GroundFlameEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
		waitFrame = Convert.ToInt32(parameters[0]);
	}

	protected override async UniTask ActionAsync(CancellationToken token)
	{
		PlayerBurnData playerBurnData = Game.DataManager.GetPlayerBurnData(base.ID);
		durationFrame = playerBurnData.Duration;
		cycleFrame = playerBurnData.CycleFrame;
		startPosition = HeatEffect.PlayerMover.VisualBottom - HeatEffect.PlayerMover.FacingDirection.ToVector2() * 2f;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(waitFrame, token);
		if (!token.IsCancellationRequested)
		{
			Vector2 vector = HeatEffect.PlayerMover.VisualBottom - startPosition;
			Vector2 vector2 = startPosition + vector / 2f;
			float width = Mathf.Abs(vector.x);
			RollingAttackFlame flame = HeatEffect.PoolManager.GetObject<RollingAttackFlame>("Rolling Attack Flame.prefab", vector2);
			flame.Initialize(durationFrame, cycleFrame, width, delegate(IDamageable damageable, List<IDamageable> list)
			{
				HeatEffect.PlayerMover.Attack(new PlayerMover.AttackParameter(damageable, base.ID, 0, AttackAttribute.Default | AttackAttribute.BlockStuck, flame), list);
			});
		}
	}
}
