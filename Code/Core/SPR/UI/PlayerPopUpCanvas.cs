using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR.UI;

public class PlayerPopUpCanvas : MonoBehaviour
{
	[SerializeField]
	private GameObject parent;

	[SerializeField]
	private PlayerEffectText effectText;

	private RectTransform rectTransform;

	private List<PlayerEffectText> effectTextPool = new List<PlayerEffectText>();

	private bool isInitialized;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public void Initialize(float height)
	{
		if (isInitialized)
		{
			return;
		}
		isInitialized = true;
		Vector2 anchoredPosition = RectTransform.anchoredPosition;
		anchoredPosition.x = 0f;
		anchoredPosition.y = height;
		RectTransform.anchoredPosition = anchoredPosition;
		if (effectTextPool.Count == 0)
		{
			effectTextPool.Add(effectText);
		}
		else
		{
			effectTextPool.ForEach(delegate(PlayerEffectText t)
			{
				t.SetActive(value: false);
			});
		}
		SetActive(value: false);
	}

	public async UniTask SetEffectDataAsync(int timerFrame, string foodID)
	{
		PlayerEffectText targetText = GetEffectText();
		targetText.SetData(timerFrame, foodID);
		SetActive(value: true);
		await UniTask.WaitUntil(() => !targetText.IsPending);
		SetActive(value: false);
	}

	public async UniTask SetEffectDatasAsync(int timerFrame, List<StoreItemData> itemDatas)
	{
		SetActive(value: true);
		foreach (StoreItemData itemData in itemDatas)
		{
			GetEffectText().SetData(timerFrame, itemData.ID);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(timerFrame);
		}
		await UniTask.WaitUntil(() => effectTextPool.All((PlayerEffectText t) => !t.IsPending));
		SetActive(value: false);
	}

	private PlayerEffectText GetEffectText()
	{
		for (int i = 0; i < effectTextPool.Count; i++)
		{
			PlayerEffectText playerEffectText = effectTextPool[i];
			if (!playerEffectText.IsPending)
			{
				playerEffectText.Initialize();
				return playerEffectText;
			}
		}
		PlayerEffectText playerEffectText2 = Object.Instantiate(effectText, parent.transform);
		playerEffectText2.Initialize();
		effectTextPool.Add(playerEffectText2);
		return playerEffectText2;
	}

	private void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
	}

	private void Update()
	{
		int sign = Game.Instance.Player.transform.localScale.x.GetSign();
		Vector3 localScale = RectTransform.localScale;
		localScale.x = (float)sign * Mathf.Abs(localScale.x);
		RectTransform.localScale = localScale;
	}

	public static async UniTask ShowStatChangesAsync(List<StoreItemData> itemDatas)
	{
		await Game.Instance.Player.PlayerPopUpCanvas.SetEffectDatasAsync(45, itemDatas);
	}

	public static async UniTask ShowStatChangeAsync(string foodID)
	{
		await Game.Instance.Player.PlayerPopUpCanvas.SetEffectDataAsync(45, foodID);
	}
}
