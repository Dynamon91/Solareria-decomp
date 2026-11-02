using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

[Serializable]
public class FlameWaveEffect : HeatEffect
{
	[SerializeField]
	private int minRadius;

	[SerializeField]
	private int maxRadius;

	[SerializeField]
	private int waveSpeed;

	[Header("넉백")]
	[SerializeField]
	private int knockBackSpeed;

	[SerializeField]
	private int knockBackFrame;

	public FlameWaveEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
	}

	protected override async UniTask ActionAsync(CancellationToken token)
	{
		PlayerFlameWave playerFlameWave = Game.Instance.ObjectPoolManager.GetObject<PlayerFlameWave>("Player Flame Wave.prefab", HeatEffect.PlayerMover.VisualCenter);
		playerFlameWave.Initialize(minRadius, maxRadius, knockBackSpeed, knockBackFrame);
		playerFlameWave.EmitAsync(waveSpeed).Forget();
		HeatEffect.PlayerMover.PSM.SetStateOptionActive(StateOption.Immortal, active: true);
		for (int i = 0; i < 40; i++)
		{
			if (token.IsCancellationRequested)
			{
				break;
			}
			if (HeatEffect.PlayerController.isDead)
			{
				break;
			}
			await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
		}
		HeatEffect.PlayerMover.PSM.SetStateOptionActive(StateOption.Immortal, active: false);
	}

	public void DrawRadiusGizmos(PlayerController player)
	{
		Gizmos.color = Color.magenta;
		Vector3 position = player.transform.position;
		Gizmos.DrawWireSphere(position, minRadius);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(position, maxRadius);
	}
}
