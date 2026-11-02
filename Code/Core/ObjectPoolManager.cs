using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectPoolManager : Manager
{
	private readonly Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();

	private List<UniTask> oftenUsedPoolCreateTasks = new List<UniTask>();

	private const int DefaultInitialSize = 5;

	private readonly List<string> OftenUsedPrefabAddresses = new List<string> { "Jump.prefab", "Landing dust.prefab", "VFX/Enemy_Damaged.prefab", "VFX/Hit_Circle.prefab", "VFX/Enemy_Damaged_4.prefab", "VFX/Enemy_BlackFog.prefab", "Fever_Readyloop.prefab", "Money.prefab", "Fever Flame.prefab" };

	protected override void OnInitialize()
	{
		foreach (string oftenUsedPrefabAddress in OftenUsedPrefabAddresses)
		{
			UniTask<ObjectPool> uniTask = CreatePoolAsync(oftenUsedPrefabAddress);
			oftenUsedPoolCreateTasks.Add(uniTask);
		}
	}

	public override async UniTask OnSceneLoaded(Scene loadedScene)
	{
		ResetPools();
		for (int i = oftenUsedPoolCreateTasks.Count - 1; i >= 0; i--)
		{
			await oftenUsedPoolCreateTasks[i];
			oftenUsedPoolCreateTasks.RemoveAt(i);
		}
	}

	public async UniTask<ObjectPool> CreatePoolAsync(string key, int initialSize = 5)
	{
		ObjectPool pool = new GameObject().AddComponent<ObjectPool>();
		pool.transform.SetParent(base.transform);
		if (!pools.TryAdd(key, pool))
		{
			return null;
		}
		await pool.InitializeAsync(key, initialSize);
		return pool;
	}

	public ObjectPool CreatePool(string key, int initialSize = 5)
	{
		if (pools.ContainsKey(key))
		{
			DebugLog.GeneralLogError(key + " 오브젝트 풀은 이미 생성되어 있습니다.");
			return null;
		}
		ObjectPool objectPool = new GameObject().AddComponent<ObjectPool>();
		objectPool.transform.SetParent(base.transform);
		objectPool.Initialize(key, initialSize);
		pools.Add(key, objectPool);
		return objectPool;
	}

	public IPoolableObject GetObject(string key, Vector3 pos, Quaternion rot, Transform parent)
	{
		if (!pools.TryGetValue(key, out var value))
		{
			value = CreatePool(key);
		}
		return value.GetOrCreate(pos, rot, parent);
	}

	public IPoolableObject GetObject(string key)
	{
		return GetObject(key, Vector3.zero, Quaternion.identity, null);
	}

	public IPoolableObject GetObject(string key, Vector3 pos)
	{
		return GetObject(key, pos, Quaternion.identity, null);
	}

	public IPoolableObject GetObject(string key, Vector3 pos, Transform parent)
	{
		return GetObject(key, pos, Quaternion.identity, parent);
	}

	public IPoolableObject GetObject(string key, Vector3 pos, Quaternion rot)
	{
		return GetObject(key, pos, rot, null);
	}

	public T GetObject<T>(string key, Vector3 pos, Quaternion rot, Transform parent) where T : class, IPoolableObject
	{
		if (!pools.TryGetValue(key, out var value))
		{
			value = CreatePool(key);
		}
		return value.GetOrCreate<T>(pos, rot, parent);
	}

	public T GetObject<T>(string key) where T : class, IPoolableObject
	{
		return GetObject<T>(key, Vector3.zero, Quaternion.identity, null);
	}

	public T GetObject<T>(string key, Transform parent) where T : class, IPoolableObject
	{
		return GetObject<T>(key, Vector3.zero, Quaternion.identity, parent);
	}

	public T GetObject<T>(string key, Vector3 pos) where T : class, IPoolableObject
	{
		return GetObject<T>(key, pos, Quaternion.identity, null);
	}

	public T GetObject<T>(string key, Vector3 pos, Transform parent) where T : class, IPoolableObject
	{
		return GetObject<T>(key, pos, Quaternion.identity, parent);
	}

	public T GetObject<T>(string key, Vector3 pos, Quaternion rot) where T : class, IPoolableObject
	{
		return GetObject<T>(key, pos, rot, null);
	}

	public int GetActiveObjectCount(string key)
	{
		if (!pools.TryGetValue(key, out var value))
		{
			return 0;
		}
		return value.PooledCount;
	}

	public ObjectPool GetPool(IPoolableObject po)
	{
		string prefabKey = po.PrefabKey;
		if (!pools.TryGetValue(prefabKey, out var value))
		{
			return CreatePool(prefabKey);
		}
		return value;
	}

	public void Return(IPoolableObject po)
	{
		GetPool(po).Return(po);
	}

	public void ReturnAll()
	{
		pools.Values.ForEach(delegate(ObjectPool pool)
		{
			pool.ReturnAll();
		});
	}

	public void DisposePool(string key)
	{
		if (!pools.TryGetValue(key, out var value))
		{
			Debug.LogError(key + " Object Pool이 존재하지 않습니다.");
		}
		else
		{
			value.Dispose();
		}
	}

	public bool HasPool(string key)
	{
		return pools.ContainsKey(key);
	}

	private void ResetPools()
	{
		ReturnAll();
		foreach (KeyValuePair<string, ObjectPool> item in pools.Where((KeyValuePair<string, ObjectPool> pool) => OftenUsedPrefabAddresses.All((string key) => key != pool.Key)))
		{
			item.Value.Dispose();
		}
	}
}
