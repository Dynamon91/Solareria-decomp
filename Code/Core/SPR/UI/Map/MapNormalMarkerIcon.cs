using Cysharp.Threading.Tasks;
using Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR.UI.Map;

public class MapNormalMarkerIcon : MapMarkerIcon, IPoolableObject
{
	public string PrefabKey => "Map Normal Marker Icon.prefab";

	public GameObject GameObject => base.gameObject;

	public static async UniTaskVoid PreloadSprites()
	{
		for (int t = -1; t >= -5; t--)
		{
			await AddressableHelper.PreloadSpriteAsync(Address.MapMarkerSprite(t));
		}
	}

	public void InitializeBeforeUse()
	{
		iconImage.SetAlpha(0f);
		ResetInitialize();
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		base.gameObject.SetActive(value: false);
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void SetPositionScale(float scale)
	{
		base.RectTransform.anchoredPosition = base.WorldPositionConstant;
		base.RectTransform.localScale = new Vector3(scale, scale, 1f);
		TryInitialize();
	}

	protected override void OnSetActiveTrue()
	{
		iconImage.SetAlpha(1f);
	}

	public async UniTask SetDataAsync(int markerType, Vector2 worldPosition)
	{
		base.MarkerType = markerType;
		worldPositionConstant = worldPosition;
		Sprite sprite = await AddressableHelper.GetSpriteAsync(Address.MapMarkerSprite(base.MarkerType));
		iconImage.sprite = sprite;
	}

	protected override void AfterDeactivate()
	{
		ReturnToPool();
	}
}
