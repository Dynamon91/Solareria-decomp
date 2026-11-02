using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class GroundFlame : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem baseParticle;

	[SerializeField]
	private ParticleSystem glowParticle;

	[SerializeField]
	private ParticleSystem emberParticle;

	private BoxCollider2D coll;

	private StudioEventEmitter eventEmitter;

	private bool isActive;

	private int currentFrame;

	private int durationFrame;

	private Action<IDamageable> attackAction;

	private float decayTime;

	private readonly List<IDamageable> damagedList = new List<IDamageable>();

	private const float MaxScale = 9.333f;

	private const float MaxRadius = 4.5f;

	private const float MaxGlowCycles = 2f;

	private const float MaxRateOverTime = 1f;

	private const float MinScale = 4f;

	private const float MinRadius = 2f;

	private const float MinGlowCycles = 1f;

	private const float MinRateOverTime = 3f;

	public string Name => "Ground Flame";

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D BoxCollider => coll ?? (coll = GetComponent<BoxCollider2D>());

	public string PrefabKey => "Ground Flame.prefab";

	public GameObject GameObject => base.gameObject;

	public void Initialize(int durationFrame, float width, Action<IDamageable> action)
	{
		this.durationFrame = durationFrame;
		attackAction = action;
		damagedList.Clear();
		currentFrame = 0;
		Vector2 size = BoxCollider.size;
		size.x = width;
		BoxCollider.size = size;
		Vector3 position = base.transform.position;
		position.y += BoxCollider.size.y / 2f;
		base.transform.position = position;
		width += Game.Instance.Player.mover.ColliderSize.x;
		float width2 = 0.469f * width + 0.125f;
		float num = 0.188f * width + 0.25f;
		float num2 = -0.375f * width + 4.5f;
		SetRadius(baseParticle, width2);
		SetRadius(glowParticle, width2);
		SetRadius(emberParticle, width2);
		ParticleSystem.Burst burst = glowParticle.emission.GetBurst(0);
		burst.cycleCount = (int)num;
		glowParticle.emission.SetBurst(0, burst);
		ParticleSystem.EmissionModule emission = emberParticle.emission;
		emission.rateOverTime = num2;
		ParticleSystem.MainModule main = baseParticle.main;
		ParticleSystem.MainModule main2 = glowParticle.main;
		ParticleSystem.MainModule main3 = emberParticle.main;
		decayTime = Mathf.Max(main.startLifetime.constantMax / main.simulationSpeed, main2.startLifetime.constantMin / main2.simulationSpeed, main3.startLifetime.constantMax / main3.simulationSpeed);
		base.gameObject.SetActive(value: true);
		isActive = true;
		if ((object)eventEmitter == null)
		{
			eventEmitter = GetComponentInChildren<StudioEventEmitter>(includeInactive: true);
		}
	}

	private void SetRadius(ParticleSystem p, float width)
	{
		ParticleSystem.ShapeModule shape = p.shape;
		shape.radius = width;
	}

	private void FixedUpdate()
	{
		if (!isActive)
		{
			return;
		}
		if (currentFrame >= durationFrame)
		{
			DisableAsync().Forget();
			return;
		}
		currentFrame++;
		Collider2D[] array = AttackUtility.OverlapBoxAll(base.transform.position, BoxCollider.size, ~(int)LayerUtility.PlayerLayer);
		if (array.IsNullOrEmpty())
		{
			return;
		}
		Collider2D[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].TryGetComponent<IDamageable>(out var component))
			{
				attackAction?.Invoke(component);
			}
		}
	}

	private async UniTask DisableAsync()
	{
		isActive = false;
		baseParticle.Stop();
		glowParticle.Stop();
		emberParticle.Stop();
		eventEmitter.Stop();
		await UniTask.WaitForSeconds(decayTime);
		ReturnToPool();
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
