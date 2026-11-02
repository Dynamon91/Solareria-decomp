using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class SoowooRisingWater : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem warningEffect;

	[SerializeField]
	private ParticleSystem effect;

	[SerializeField]
	private EventReference warningEvent;

	[SerializeField]
	private EventReference waterEvent;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private FrameTimer timer;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public string PrefabKey => "Soowoo Rising Water.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "SoowooRisingWater";

	public Direction FacingDirection => Direction.None;

	public void InitializeBeforeUse()
	{
		timer = null;
		damageableList.Clear();
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (timer != null)
		{
			if (timer.IsDone)
			{
				ReturnToPool();
				return;
			}
			timer.Tick();
			HandleOverlap();
		}
	}

	public async UniTaskVoid Initialize(int waitDuration, int duration)
	{
		warningEffect.Play();
		SoundManager.PlayOneShotAttached(warningEvent, base.transform);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(waitDuration);
		warningEffect.Stop();
		timer = new FrameTimer(duration);
		effect.Play();
		SoundManager.PlayOneShotAttached(waterEvent, base.transform);
	}

	private void HandleOverlap()
	{
		Vector3 localScale = base.transform.localScale;
		AttackUtility.AttackBox(base.transform.position, localScale, new HitInfo(this, string.Empty), LayerUtility.EnemyDefaultAttackLayer, damageableList);
	}
}
