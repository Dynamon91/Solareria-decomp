using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class EffectBoard : MonoBehaviour
{
	[SerializeField]
	private List<StatusEffectIcon> effectIcons;

	private readonly Dictionary<string, Sprite> cachedEffectSprites = new Dictionary<string, Sprite>();

	public void Initialize()
	{
		AddressableHelper.PreloadAssetsAsync<Sprite>("Status Effect").Forget();
		HideAllIcons(instant: true);
	}

	public void ApplyIcon(StatusEffect statusEffect)
	{
		int effectType = (int)statusEffect.EffectType;
		effectIcons[effectType].StartEffect(statusEffect);
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.CastObject<RectTransform>());
	}

	public void UpdateIcons()
	{
		effectIcons.ForEach(delegate(StatusEffectIcon icon)
		{
			if (icon.IsValid)
			{
				icon.TickEffect();
			}
		});
	}

	public void HideAllIcons(bool instant = false)
	{
		effectIcons.ForEach(delegate(StatusEffectIcon icon)
		{
			icon.HideIconWithHighlight(instant);
		});
	}

	public async UniTask<Sprite> GetStatusEffectSpriteAsync(StatusEffectType effectType, int stack = 1, bool active = false)
	{
		string key = Address.StatusEffectSprite(effectType, stack, active);
		if (cachedEffectSprites.TryGetValue(key, out var value))
		{
			return value;
		}
		AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(key);
		await task;
		cachedEffectSprites.Add(key, task.Result);
		return task.Result;
	}
}
