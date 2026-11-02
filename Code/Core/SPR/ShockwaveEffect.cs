using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

[Serializable]
public class ShockwaveEffect : HeatEffect
{
	[Space]
	[SerializeField]
	private float waveSpeed;

	[SerializeField]
	private float distance;

	[SerializeField]
	private Vector2 size = new Vector2(3f, 8f);

	[Space]
	[SerializeField]
	protected bool showGizmos;

	public ShockwaveEffect(string id)
		: base(id)
	{
	}

	public override void SetParameters<T>(params T[] parameters)
	{
	}

	protected override async UniTask ActionAsync(CancellationToken token)
	{
		PlayerOverheatWave playerOverheatWave = HeatEffect.PoolManager.GetObject<PlayerOverheatWave>("Player Overheat Wave.prefab", HeatEffect.PlayerMover.VisualBottom);
		playerOverheatWave.Initialize(1, distance, size);
		playerOverheatWave.ShootAsync(waveSpeed).Forget();
		PlayerOverheatWave playerOverheatWave2 = HeatEffect.PoolManager.GetObject<PlayerOverheatWave>("Player Overheat Wave.prefab", HeatEffect.PlayerMover.VisualBottom);
		playerOverheatWave2.Initialize(-1, distance, size);
		playerOverheatWave2.ShootAsync(waveSpeed).Forget();
		await UniTask.CompletedTask;
	}

	public void DrawGizmos(PlayerController player)
	{
		if (showGizmos)
		{
			Color yellow = Color.yellow;
			yellow.a = 0.5f;
			Gizmos.color = yellow;
			Vector3 center = player.transform.position + new Vector3(0f, size.y / 2f);
			Gizmos.DrawWireCube(center, size);
			Gizmos.DrawCube(center, new Vector3(distance * 2f, size.y));
		}
	}
}
