using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaSignEffect : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private ParticleSystem particle;

	public string PrefabKey => "Wave Seed.prefab";

	public GameObject GameObject => base.gameObject;

	public void Play()
	{
		base.gameObject.SetActive(value: true);
		particle.Play();
	}

	private void FixedUpdate()
	{
		if (!particle.isPlaying)
		{
			ReturnToPool();
		}
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
