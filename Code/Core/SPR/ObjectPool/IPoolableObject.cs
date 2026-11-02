using UnityEngine;

namespace SPR.ObjectPool;

public interface IPoolableObject
{
	string PrefabKey { get; }

	GameObject GameObject { get; }

	void InitializeBeforeUse();

	void ReturnToPool();
}
