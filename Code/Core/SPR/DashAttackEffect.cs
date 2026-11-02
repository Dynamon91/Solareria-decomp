using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class DashAttackEffect : PoolingObject
{
	private PoolingParticleOnSpineMesh flame;

	private List<PoolingParticle> ashBreaks;

	private Enemy enemy;

	private MeshRenderer meshRenderer;

	private int parentSign;

	public bool IsInExplosion { get; private set; }

	private void Update()
	{
		int sign = base.transform.parent.localScale.x.GetSign();
		if (sign != parentSign)
		{
			parentSign = sign;
			Vector3 localScale = base.transform.localScale;
			localScale.x *= -1f;
			base.transform.localScale = localScale;
		}
	}

	public void Initialize(Enemy enemy)
	{
		IsInExplosion = false;
		meshRenderer = enemy.GetComponentInChildren<MeshRenderer>();
		Bounds bounds = meshRenderer.bounds;
		_ = bounds.min;
		_ = bounds.max;
		flame = Game.Instance.ObjectPoolManager.GetObject<PoolingParticleOnSpineMesh>("Ash Trap 1", base.transform);
		flame.transform.localPosition = Vector3.zero;
		flame.GameObject.SetActive(value: false);
		if (enemy.transform.rotation.z != 0f)
		{
			flame.SetMeshRenderer(meshRenderer);
		}
		else
		{
			flame.SetMesh(enemy.GetComponentInChildren<MeshFilter>().mesh);
			enemy.SkeletonRenderer.OnMeshAndMaterialsUpdated += UpdateFlame;
		}
		this.enemy = enemy;
		parentSign = base.transform.parent.localScale.x.GetSign();
	}

	private void UpdateFlame(SkeletonRenderer r)
	{
		flame.SetMesh(enemy.GetComponentInChildren<MeshFilter>().mesh);
	}

	public void PlayReady()
	{
		flame.GameObject.SetActive(value: true);
		flame.ParticleSystem.SetLoop(enabled: true);
		flame.ParticleSystem.Play();
	}

	public async UniTaskVoid PlayExplosionAsync(Action effectAction)
	{
		IsInExplosion = true;
		flame.ParticleSystem.SetLoop(enabled: false);
		flame.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		UniTaskExtension.WaitForFixedUpdatesOrCancel(5).GetAwaiter().OnCompleted(effectAction);
		Bounds bounds = meshRenderer.bounds;
		Vector3 vector = bounds.min + Vector3.right * 0.5f;
		Vector3 vector2 = bounds.max + Vector3.left * 0.5f;
		PlayerControlParameter instance = PlayerControlParameter.Instance;
		float magnitude = (vector2 - vector).magnitude;
		float num = instance.explosionCountCoefficient / Mathf.Max(magnitude, instance.explosionCountCoefficient);
		int ashBreakCount = Mathf.FloorToInt((vector2.x - vector.x) * (vector2.y - vector.y) * 0.2f * num);
		if (ashBreaks == null)
		{
			ashBreaks = new List<PoolingParticle>();
		}
		float seconds = PlayerControlParameter.Instance.explosionDuration / (float)ashBreakCount;
		for (int i = 0; i < ashBreakCount; i++)
		{
			PoolingParticle poolingParticle = Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("Ash Trap 2", base.transform);
			bounds = meshRenderer.bounds;
			vector = bounds.min + Vector3.right * 0.5f;
			vector2 = bounds.max + Vector3.left * 0.5f;
			Vector2 pos = new Vector2(UnityEngine.Random.Range(vector.x, vector2.x), UnityEngine.Random.Range(vector.y, vector2.y));
			while (ashBreaks.Any((PoolingParticle a) => Vector2.Distance(a.transform.position, pos) <= 0.5f))
			{
				pos = new Vector2(UnityEngine.Random.Range(vector.x, vector2.x), UnityEngine.Random.Range(vector.y, vector2.y));
			}
			poolingParticle.transform.position = pos;
			ashBreaks.Add(poolingParticle);
			poolingParticle.ParticleSystem.Play();
			await UniTask.WaitForSeconds(seconds);
		}
		await UniTask.WaitWhile(() => ashBreaks.TrueForAll((PoolingParticle ashBreak) => ashBreak.ParticleSystem.isPlaying));
		ReturnToPool();
	}

	public override void ReturnToPool()
	{
		IsInExplosion = false;
		if (enemy != null && enemy.transform.rotation.z == 0f)
		{
			enemy.SkeletonRenderer.OnMeshAndMaterialsUpdated -= UpdateFlame;
		}
		flame.SetMesh(null);
		flame.ReturnToPool();
		flame = null;
		if (ashBreaks != null)
		{
			ashBreaks.ForEach(delegate(PoolingParticle ashBreak)
			{
				ashBreak.ReturnToPool();
			});
			ashBreaks.Clear();
		}
		base.ReturnToPool();
	}
}
