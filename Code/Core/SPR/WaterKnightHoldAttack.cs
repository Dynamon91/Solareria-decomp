using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class WaterKnightHoldAttack : MonoBehaviour, IPoolableObject, IAttackSource
{
	public const string Address = "WaterKnight_HoldAttack.prefab";

	[SerializeField]
	private float maxRange;

	[SerializeField]
	private float maxHeight;

	[SerializeField]
	private ParticleSystem splash1;

	[SerializeField]
	private ParticleSystem splash2;

	[SerializeField]
	private ParticleSystem waterDot1;

	[SerializeField]
	private ParticleSystem waterDot2;

	[SerializeField]
	private MinMax[] splash1Range;

	[SerializeField]
	private MinMax[] splash2Range;

	[SerializeField]
	private MinMax[] waterDot1Range;

	[SerializeField]
	private MinMax[] waterDot2Range;

	[SerializeField]
	private EventReference splashEvent;

	public float MaxRange => maxRange;

	public string PrefabKey => "WaterKnight_HoldAttack.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "WaterAttack";

	public Direction FacingDirection => Direction.Right;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public async UniTaskVoid Splash(float sizeX, float scaleX, int duration)
	{
		if (scaleX <= 0f)
		{
			ReturnToPool();
			return;
		}
		Vector3 localScale = base.transform.localScale;
		localScale.x = scaleX;
		base.transform.localScale = localScale;
		Vector3 center = base.transform.position;
		center.y += maxHeight / 2f;
		Vector2 size = new Vector2(Mathf.Clamp(sizeX, 0f, maxRange), maxHeight);
		SetEmission(splash1, splash1Range, scaleX);
		SetEmission(splash2, splash2Range, scaleX);
		SetEmission(waterDot1, waterDot1Range, scaleX);
		SetEmission(waterDot2, waterDot2Range, scaleX);
		Game.Instance.SoundManager.PlayOneShotAttachedWithSfxLocalVolume(splashEvent, base.transform, scaleX);
		int frame = 0;
		List<IDamageable> list = new List<IDamageable>();
		HitInfo hitInfo = ((size.x >= maxRange) ? new HitInfo(this, "Max") : new HitInfo(this));
		for (; frame < duration; frame++)
		{
			AttackUtility.AttackBox(center, size, hitInfo, LayerUtility.EnemyDefaultAttackLayer, list);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		await UniTask.WaitWhile(() => GetComponent<ParticleSystem>().isPlaying);
		ReturnToPool();
	}

	private static void SetEmission(ParticleSystem particle, MinMax[] range, float normal)
	{
		ParticleSystem.EmissionModule emission = particle.emission;
		ParticleSystem.Burst burst = emission.GetBurst(0);
		ParticleSystem.Burst burst2 = emission.GetBurst(1);
		ParticleSystem.MinMaxCurve count = burst.count;
		count.constant = range[0].min + (range[0].max - range[0].min) * normal;
		burst.count = count;
		emission.SetBurst(0, burst);
		ParticleSystem.MinMaxCurve count2 = burst2.count;
		count2.constant = range[1].min + (range[1].max - range[1].min) * normal;
		burst2.count = count2;
		emission.SetBurst(1, burst2);
	}
}
