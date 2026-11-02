using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SPR;

[Serializable]
public class SimpleAttackEffect : HeatEffect, IAttackSource
{
	public string Name => base.ID;

	public Direction FacingDirection
	{
		get
		{
			if (!HeatEffect.PlayerMover.IsFacingRight)
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public SimpleAttackEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
	}

	protected override UniTask ActionAsync(CancellationToken token)
	{
		HeatEffect.PlayerMover.EnableVFX(base.ID);
		HeatEffect.PlayerMover.ClearDamagedList();
		HeatEffect.PlayerMover.HandleAttackHitBox(base.ID, 0, AttackAttribute.Default);
		return UniTask.CompletedTask;
	}
}
