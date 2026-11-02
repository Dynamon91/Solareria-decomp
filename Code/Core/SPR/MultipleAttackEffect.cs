using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

[Serializable]
public class MultipleAttackEffect : HeatEffect, IAttackSource
{
	[SerializeField]
	private List<int> damageFrames;

	public string Name => base.ID;

	public Direction FacingDirection => Direction.None;

	public MultipleAttackEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
	}

	protected override async UniTask ActionAsync(CancellationToken token)
	{
		HeatEffect.PlayerMover.EnableVFX(base.ID);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(damageFrames[0], token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		int attackCount = 0;
		int i = damageFrames[0];
		while (true)
		{
			int num = i;
			List<int> list = damageFrames;
			if (num <= list[list.Count - 1])
			{
				if (damageFrames.Contains(i))
				{
					HeatEffect.PlayerMover.ClearDamagedList();
					HeatEffect.PlayerMover.HandleAttackHitBox(base.ID, attackCount, AttackAttribute.Default | AttackAttribute.BlockStuck, 0f, this);
					attackCount++;
				}
				await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
				if (token.IsCancellationRequested)
				{
					break;
				}
				i++;
				continue;
			}
			break;
		}
	}
}
