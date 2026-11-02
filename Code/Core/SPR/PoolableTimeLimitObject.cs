using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class PoolableTimeLimitObject : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private string address;

	private Timer timer;

	public string PrefabKey { get; private set; }

	public GameObject GameObject => base.gameObject;

	public virtual void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void Initialize(float lifetime)
	{
		PrefabKey = address;
		timer = new Timer(lifetime);
	}

	public void Initialize(string key, float lifeTime = 0f)
	{
		PrefabKey = key;
		timer = new Timer(lifeTime);
	}

	private void FixedUpdate()
	{
		if (timer != null && !timer.IsDone)
		{
			timer.Tick(Time.fixedDeltaTime);
			if (timer.IsDone)
			{
				ReturnToPool();
			}
		}
	}

	public void ReturnToPool()
	{
		timer?.Reset();
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
