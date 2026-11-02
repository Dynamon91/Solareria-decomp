using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

[Serializable]
public class RegainHealthEffect : HeatEffect
{
	[SerializeField]
	private float regainRate;

	private int regainAmount;

	public RegainHealthEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
		T val = parameters[0];
		if (val is int num)
		{
			regainAmount = Mathf.RoundToInt((float)num * regainRate);
		}
	}

	protected override UniTask ActionAsync(CancellationToken token)
	{
		HeatEffect.PlayerController.PlayerStat.GetStat<Health>().AddHealth(regainAmount, healInstantly: true);
		return UniTask.CompletedTask;
	}
}
