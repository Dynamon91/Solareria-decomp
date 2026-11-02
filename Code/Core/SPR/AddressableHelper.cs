using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace SPR;

public static class AddressableHelper
{
	private static readonly Dictionary<string, AsyncOperationHandle> Handles = new Dictionary<string, AsyncOperationHandle>();

	private static readonly Dictionary<string, List<UnityEngine.Object>> Assets = new Dictionary<string, List<UnityEngine.Object>>();

	private static readonly Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

	public static async UniTask PreloadAssetAsync(string key)
	{
		if (!Handles.ContainsKey(key))
		{
			AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
			await handle.Task;
			Handles.TryAdd(key, handle);
		}
	}

	public static async UniTaskVoid PreloadAssetsAsync(string label)
	{
		if (!Assets.ContainsKey(label))
		{
			AsyncOperationHandle<IList<UnityEngine.Object>> handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, null);
			await handle.Task;
			Assets.TryAdd(label, handle.Result.ToList());
		}
	}

	public static async UniTaskVoid PreloadAssetsAsync<T>(string label) where T : UnityEngine.Object
	{
		if (!Assets.ContainsKey(label))
		{
			AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(label, null);
			await handle.Task;
			Assets.TryAdd(label, handle.Result.Cast<UnityEngine.Object>().ToList());
		}
	}

	public static async UniTask PreloadSpriteAsync(string address)
	{
		if (address == "???" || string.IsNullOrEmpty(address))
		{
			DebugLog.AddressableLogWarning(address + "에 해당하는 Sprite를 찾을 수 없습니다.");
			address = "ItemSprite/Unknown";
		}
		if (!CachedSprites.TryGetValue("ItemSprite/Unknown", out var value))
		{
			await TryAddCachedSprites("ItemSprite/Unknown");
		}
		if (!CachedSprites.TryGetValue(address, out value))
		{
			await TryAddCachedSprites(address);
		}
	}

	public static async UniTask PreloadSpritesByLabelAsync(string label)
	{
		if (string.IsNullOrEmpty(label))
		{
			DebugLog.AddressableLogWarning("라벨 값이 비어있습니다.");
			return;
		}
		AsyncOperationHandle<IList<IResourceLocation>> asyncOperationHandle = Addressables.LoadResourceLocationsAsync(label, typeof(Sprite));
		IList<IResourceLocation> list;
		try
		{
			list = await asyncOperationHandle.Task;
		}
		catch (Exception ex)
		{
			DebugLog.AddressableLogError("라벨[" + label + "] 리소스 로케이션 조회 실패: " + ex.Message);
			return;
		}
		if (list == null || list.Count == 0)
		{
			DebugLog.AddressableLogWarning("라벨[" + label + "]에 해당하는 Sprite 리소스가 없습니다.");
			return;
		}
		foreach (IResourceLocation item in list)
		{
			await PreloadSpriteAsync(item.PrimaryKey);
		}
	}

	private static async UniTask TryAddCachedSprites(string address)
	{
		if (!CachedSprites.ContainsKey(address))
		{
			AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(address);
			await task;
			CachedSprites.TryAdd(address, task.Result);
		}
	}

	public static async UniTask<Sprite> GetSpriteAsync(string address)
	{
		if (address == "???" || string.IsNullOrEmpty(address))
		{
			DebugLog.AddressableLogWarning(address + "에 해당하는 Sprite를 찾을 수 없습니다.");
			address = "ItemSprite/Unknown";
		}
		if (CachedSprites.TryGetValue(address, out var value))
		{
			return value;
		}
		AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(address);
		await task;
		if (CachedSprites.TryGetValue(address, out value))
		{
			return value;
		}
		CachedSprites.Add(address, task.Result);
		return CachedSprites[address];
	}

	public static Sprite GetSprite_Unsafe(string address)
	{
		if (address == "???" || string.IsNullOrEmpty(address))
		{
			DebugLog.AddressableLogWarning(address + "에 해당하는 Sprite를 찾을 수 없습니다.");
			address = "UnknownIngredient";
		}
		return CachedSprites.GetValueOrDefault(address);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address) where T : UnityEngine.Object
	{
		return await InstantiateAsync<T>(address, Vector2.zero, 0f, null);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address, Transform parent) where T : UnityEngine.Object
	{
		return await InstantiateAsync<T>(address, Vector2.zero, 0f, parent);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address, Transform parent, float rotationZ) where T : UnityEngine.Object
	{
		return await InstantiateAsync<T>(address, Vector2.zero, rotationZ, parent);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address, Vector2 position) where T : UnityEngine.Object
	{
		return await InstantiateAsync<T>(address, position, 0f, null);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address, Vector2 position, Transform parent) where T : UnityEngine.Object
	{
		return await InstantiateAsync<T>(address, position, 0f, parent);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address, Vector2 position, float rotationZ) where T : UnityEngine.Object
	{
		return await InstantiateAsync<T>(address, position, rotationZ, null);
	}

	public static async UniTask<T> InstantiateAsync<T>(string address, Vector2 position, float rotationZ, Transform parent) where T : UnityEngine.Object
	{
		GameObject gameObject;
		if (Handles.TryGetValue(address, out var value))
		{
			gameObject = UnityEngine.Object.Instantiate(value.Result.CastObject<GameObject>());
		}
		else
		{
			AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
			await handle.Task;
			if (handle.Status != AsyncOperationStatus.Succeeded)
			{
				DebugLog.AddressableLogError("Addressable을 생성할 수 없습니다! 키:\"" + address + "\"");
				return null;
			}
			if (!Handles.TryAdd(address, handle))
			{
				DebugLog.AddressableLogWarning("여러 번 로드를 시도하고 있는 에셋이 있습니다. 해당 에셋을 Preload 하세요. 키:" + address);
			}
			gameObject = UnityEngine.Object.Instantiate(handle.Result);
		}
		gameObject.transform.position = position;
		gameObject.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
		gameObject.transform.SetParent(parent);
		if (gameObject.TryGetComponent<T>(out var component))
		{
			return component;
		}
		DebugLog.AddressableLogError("Addressable을 생성했으나 " + typeof(T).Name + "을 찾을 수 없습니다! 키:\"" + address + "\"");
		return null;
	}

	public static T GetAsset<T>(string label, string name) where T : UnityEngine.Object
	{
		UnityEngine.Object obj = Assets[label].Find((UnityEngine.Object x) => x.name == name);
		if ((object)obj == null)
		{
			DebugLog.AddressableLogError(name + " 애셋을 찾을 수 없습니다! 파일 이름이 정확한지, 라벨을 지정했는지 확인해주세요.");
			return null;
		}
		if (!(obj is T))
		{
			DebugLog.AddressableLogError("해당 애셋 \"" + name + "\"는 \"" + typeof(T).Name + "\"타입의 애셋이 아닙니다!");
			return null;
		}
		return Assets[label].Find((UnityEngine.Object x) => x.name == name).CastObject<T>();
	}

	public static bool TryGetAsset<T>(string label, string name, out T asset) where T : UnityEngine.Object
	{
		asset = null;
		if (!Assets.TryGetValue(label, out var value))
		{
			return false;
		}
		UnityEngine.Object obj = value.Find((UnityEngine.Object x) => x.name == name);
		if (!(obj is T))
		{
			return false;
		}
		asset = obj.CastObject<T>();
		return true;
	}

	public static bool HasKey(string key)
	{
		if (!Application.isPlaying)
		{
			return false;
		}
		foreach (IResourceLocator resourceLocator in Addressables.ResourceLocators)
		{
			if (resourceLocator.Locate(key, null, out var _))
			{
				return true;
			}
		}
		return false;
	}

	public static AsyncOperationHandle GetAssetHandle(string address)
	{
		if (Handles.TryGetValue(address, out var value))
		{
			return value;
		}
		AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>(address);
		if (!Handles.TryAdd(address, asyncOperationHandle))
		{
			DebugLog.AddressableLogWarning("여러 번 로드를 시도하고 있는 에셋이 있습니다. 해당 에셋을 Preload 하세요. 키:" + address);
		}
		return asyncOperationHandle;
	}

	public static void UnloadAsset(string address)
	{
		if (Handles.TryGetValue(address, out var value))
		{
			Addressables.Release(value);
			Handles.Remove(address);
		}
	}

	public static void ReleaseInstance(GameObject instance)
	{
		if ((object)instance == null || instance.IsNull())
		{
			DebugLog.AddressableLogError("해제할 인스턴스의 레퍼런스를 찾을 수 없습니다!");
		}
		Addressables.ReleaseInstance(instance);
	}
}
