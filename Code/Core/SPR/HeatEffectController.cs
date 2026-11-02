using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SPR;

[Serializable]
public class HeatEffectController
{
	public readonly struct HeatEffectDirector
	{
		private readonly HeatEffect targetEffect;

		public HeatEffectDirector(HeatEffect targetEffect)
		{
			this.targetEffect = targetEffect;
		}

		public HeatEffectDirector SetParameters<T>(params T[] parameters)
		{
			targetEffect.SetParameters(parameters);
			return this;
		}

		public void TryInvoke(CancellationToken token = default(CancellationToken))
		{
			targetEffect.Invoke(PlayerStat.HeatLevel, token);
		}

		public void TryInvoke(int level, CancellationToken token = default(CancellationToken))
		{
			targetEffect.Invoke(level, token);
		}

		public void TryCancel()
		{
			targetEffect.Cancel();
		}
	}

	[Header("과열 충격파")]
	[SerializeField]
	private ShockwaveEffect overheatWaveEffect = new ShockwaveEffect("overheat_wave");

	[Header("강 파이론 액션 쉴드")]
	[SerializeField]
	private ShieldEffect pyronShieldEffect = new ShieldEffect("heavy_pyron_shield");

	[Header("열기 버스트 액션")]
	[SerializeField]
	private float burstDistanceDelta = -2f;

	[SerializeField]
	private List<int> burstDamageFrames;

	[SerializeField]
	private List<Vector2> burstPositions = new List<Vector2>(3)
	{
		new Vector2(-2f, 3f),
		Vector2.zero,
		new Vector2(2f, -2.5f),
		Vector2.zero
	};

	private List<HeatEffect> heatEffects;

	public IReadOnlyList<int> BurstDamageFrames => burstDamageFrames;

	public IReadOnlyList<Vector2> BurstPositions => burstPositions;

	public float BurstDistanceDelta => burstDistanceDelta;

	public void DrawGizmos(PlayerController player)
	{
		overheatWaveEffect.DrawGizmos(player);
	}

	public void Initialize()
	{
		heatEffects = new List<HeatEffect> { overheatWaveEffect, pyronShieldEffect };
	}

	public HeatEffectDirector FindHeatEffect(string id)
	{
		HeatEffect heatEffect = heatEffects.FirstOrDefault((HeatEffect e) => e.ID == id);
		if (heatEffect != null)
		{
			return new HeatEffectDirector(heatEffect);
		}
		DebugLog.GeneralLogError("ID:" + id + " 에 해당하는 열기 효과가 존재하지 않습니다.");
		return default(HeatEffectDirector);
	}
}
