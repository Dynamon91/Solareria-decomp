using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ObjectPool : MonoBehaviour
{
	private readonly Stack<IPoolableObject> availableObjects = new Stack<IPoolableObject>();

	private List<IPoolableObject> pooledObjects = new List<IPoolableObject>();

	private AsyncOperationHandle<GameObject> objectHandle;

	public int PooledCount => pooledObjects.Count;

	public async UniTask InitializeAsync(string key, int initialSize)
	{
		objectHandle = Addressables.LoadAssetAsync<GameObject>(key);
		await objectHandle.Task;
		Initialize_Internal(key, initialSize);
	}

	public void Initialize(string key, int initialSize)
	{
		objectHandle = Addressables.LoadAssetAsync<GameObject>(key);
		objectHandle.WaitForCompletion();
		Initialize_Internal(key, initialSize);
	}

	private void Initialize_Internal(string key, int initialSize)
	{
		if (objectHandle.Status != AsyncOperationStatus.Succeeded)
		{
			Debug.LogError("Failed " + key + " object pool initializing");
			return;
		}
		base.name = key + " Pool";
		for (int i = 0; i < initialSize; i++)
		{
			CreateObject();
		}
	}

	private void CreateObject()
	{
		IPoolableObject component = UnityEngine.Object.Instantiate(objectHandle.Result, base.transform).GetComponent<IPoolableObject>();
		GameObject obj = component.GameObject;
		obj.transform.SetParent(base.transform);
		obj.SetActive(value: false);
		availableObjects.Push(component);
	}

	public IPoolableObject GetOrCreate(Vector3 pos, Quaternion rot, Transform usingTransform = null)
	{
		if (availableObjects.Count == 0)
		{
			CreateObject();
		}
		IPoolableObject poolableObject = availableObjects.Pop();
		Transform transform = poolableObject.GameObject.transform;
		transform.position = pos;
		transform.rotation = rot;
		if (!usingTransform.IsNull())
		{
			transform.SetParent(usingTransform);
		}
		poolableObject.InitializeBeforeUse();
		pooledObjects.Add(poolableObject);
		return poolableObject;
	}

	public T GetOrCreate<T>(Vector3 pos, Quaternion rot, Transform usingTransform = null) where T : class, IPoolableObject
	{
		return GetOrCreate(pos, rot, usingTransform) as T;
	}

	public void Return(IPoolableObject po)
	{
		if (!availableObjects.Contains(po))
		{
			bool flag = true;
			try
			{
				GameObject obj = po.GameObject;
				obj.transform.SetParent(base.transform);
				obj.SetActive(value: false);
			}
			catch (MissingReferenceException)
			{
				flag = false;
				DebugLog.GeneralLogWarning($"풀링 오브젝트 반환 시도. (type: {po.GetType()}) Missing!");
			}
			catch (NullReferenceException)
			{
				flag = false;
				DebugLog.GeneralLogWarning($"풀링 오브젝트 반환 시도. (type: {po.GetType()}) Null!");
			}
			pooledObjects.Remove(po);
			if (flag)
			{
				availableObjects.Push(po);
			}
		}
	}

	public void ReturnAll()
	{
		for (int num = pooledObjects.Count - 1; num >= 0; num--)
		{
			Return(pooledObjects[num]);
		}
	}

	public void Dispose()
	{
		while (availableObjects.Count > 0)
		{
			IPoolableObject poolableObject = availableObjects.Pop();
			try
			{
				UnityEngine.Object.Destroy(poolableObject.GameObject);
			}
			catch (MissingReferenceException)
			{
				DebugLog.GeneralLogWarning($"풀링 오브젝트 해제 시도. (type: {poolableObject.GetType()}) Missing!");
			}
			catch (NullReferenceException)
			{
				DebugLog.GeneralLogWarning($"풀링 오브젝트 해제 시도. (type: {poolableObject.GetType()}) Null!");
			}
		}
		foreach (IPoolableObject pooledObject in pooledObjects)
		{
			try
			{
				UnityEngine.Object.Destroy(pooledObject.GameObject);
			}
			catch (MissingReferenceException)
			{
				DebugLog.GeneralLogWarning($"풀링 오브젝트 해제 시도. (type: {pooledObject.GetType()}) Missing!");
			}
			catch (NullReferenceException)
			{
				DebugLog.GeneralLogWarning($"풀링 오브젝트 반환 시도. (type: {pooledObject.GetType()}) Null!");
			}
		}
		pooledObjects?.Clear();
	}
}
