using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class PoolingObject : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private string prefabKey;

	public string PrefabKey { get; private set; }

	public GameObject GameObject => base.gameObject;

	public virtual void InitializeBeforeUse()
	{
		PrefabKey = prefabKey;
		base.gameObject.SetActive(value: true);
	}

	public virtual void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
