using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

[Serializable]
public class ShieldEffect : HeatEffect
{
	[SerializeField]
	[Range(0f, 1f)]
	private float shieldHealthRate;

	public ShieldEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
	}

	protected override async UniTask ActionAsync(CancellationToken token)
	{
		int health = (int)((float)Game.Instance.Player.PlayerStat.GetStat<Health>().MaxHealth * shieldHealthRate);
		HeatEffect.PlayerMover.PlayerShield.AddShield(health);
		await UniTask.CompletedTask;
	}
}
