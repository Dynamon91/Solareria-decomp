using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SPR;

[Serializable]
public class BlockStuckEffect : HeatEffect
{
	public BlockStuckEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
	}

	protected override UniTask ActionAsync(CancellationToken token)
	{
		HeatEffect.PlayerMover.PSM.SetStateOptionActive(StateOption.BlockStuck, active: true);
		return UniTask.CompletedTask;
	}

	public override void Cancel()
	{
		if (PlayerStat.HeatLevel < targetLevel)
		{
			HeatEffect.PlayerMover.PSM.SetStateOptionActive(StateOption.BlockStuck, active: false);
		}
	}
}
