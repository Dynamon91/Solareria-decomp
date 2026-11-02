using System;
using Cysharp.Threading.Tasks;
using SPR.L10N;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR.UI.Field;

[Serializable]
public class ItemBoardData
{
	public readonly string Id;

	public readonly string Key;

	public readonly string CountText;

	public readonly Sprite Sprite;

	public readonly bool UseTraitPoint;

	private ItemBoardData(string id, string key, Sprite sprite, bool useTraitPoint, string countText)
	{
		Id = id;
		Key = key;
		Sprite = sprite;
		UseTraitPoint = useTraitPoint;
		CountText = countText;
	}

	public static async UniTask<ItemBoardData> GetDataAsync(string id)
	{
		string key = LocaleKey.ItemName(id);
		Sprite sprite = null;
		bool useTrait = false;
		if (id == "Inflare")
		{
			useTrait = true;
		}
		else
		{
			AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.ItemSprite(id));
			await task;
			sprite = task.Result;
		}
		return new ItemBoardData(id, key, sprite, useTrait, null);
	}

	public static async UniTask<ItemBoardData> GetDataAsync(string id, int count)
	{
		string key = LocaleKey.ItemName(id);
		Sprite sprite = null;
		bool useTrait = false;
		string countString = $"+{count}";
		if (id == "Inflare")
		{
			useTrait = true;
		}
		else
		{
			AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.ItemSprite(id));
			await task;
			sprite = task.Result;
		}
		return new ItemBoardData(id, key, sprite, useTrait, countString);
	}
}
