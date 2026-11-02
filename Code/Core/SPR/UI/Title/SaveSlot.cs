using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.L10N;
using SPR.Save;
using SPR.UI.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Title;

public class SaveSlot : MonoBehaviour
{
	[Header("Parent")]
	[SerializeField]
	private CanvasGroup emptyParent;

	[Header("Focus")]
	[SerializeField]
	private Image unfocusedCover;

	[SerializeField]
	private Image indicator;

	[Header("UI Components")]
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Image landmarkImage;

	[SerializeField]
	private TextMeshProUGUI levelText;

	[SerializeField]
	private LocaleText saveSectorText;

	[SerializeField]
	private LocaleText saveAltarText;

	[SerializeField]
	private TextMeshProUGUI timeText;

	[SerializeField]
	private TextMeshProUGUI collectRateText;

	[SerializeField]
	private TextMeshProUGUI dateTimeText;

	private RectTransform rect;

	private const float CoverAlpha = 0.7058824f;

	private const float IndicatorAlpha = 0.5803922f;

	private float fadeDuration;

	public SaveData SaveData { get; private set; }

	public RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	public void Initialize(SaveData data, float fadeDuration)
	{
		SaveData = data;
		this.fadeDuration = fadeDuration;
		if (data != null && !data.IsNewSaveData)
		{
			AddressableHelper.PreloadSpriteAsync(Address.LandmarkSprite(SaveData.LastFieldID)).Forget();
		}
	}

	public void Refresh(bool instant)
	{
		ShowData(instant);
	}

	private void ShowData(bool instant)
	{
		Focus(value: false, instant);
		if (SaveData == null || SaveData.IsNewSaveData)
		{
			bool activeSelf = emptyParent.gameObject.activeSelf;
			emptyParent.gameObject.SetActive(value: true);
			if (instant || activeSelf)
			{
				emptyParent.alpha = 1f;
				return;
			}
			emptyParent.alpha = 0f;
			emptyParent.DOAlpha(1f, fadeDuration);
			return;
		}
		FieldID lastFieldID = SaveData.LastFieldID;
		SetImageSpriteAsync(icon, Address.DifficultyIconSprite(SaveData.Difficulty)).Forget();
		SetImageSpriteAsync(landmarkImage, Address.LandmarkSprite(lastFieldID)).Forget();
		levelText.text = SaveData.PlayerData.Level.ToString();
		SectorName sectorNameForPotteryRoad = FieldID.GetSectorNameForPotteryRoad(lastFieldID);
		saveSectorText.SetData(LocaleKey.SectorName(sectorNameForPotteryRoad));
		saveAltarText.SetData(LocaleKey.AltarName(lastFieldID));
		TimeSpan timeSpan = TimeSpan.FromSeconds(SaveData.GetCurrentPlayTime());
		timeText.text = $"{(int)timeSpan.TotalHours:00}H {timeSpan.Minutes:00}M";
		int total;
		int current;
		int globalCollection = MapCollection.GetGlobalCollection(SaveData, out total, out current);
		collectRateText.text = $"{globalCollection}%";
		dateTimeText.text = SaveData.LastDateTime;
		emptyParent.gameObject.SetActive(value: false);
	}

	private async UniTask SetImageSpriteAsync(Image image, string address)
	{
		image.enabled = false;
		image.sprite = await AddressableHelper.GetSpriteAsync(address);
		image.enabled = true;
	}

	public void Focus(bool value, bool instant = false)
	{
		unfocusedCover.DOKill();
		indicator.DOKill();
		float duration = (instant ? 0f : fadeDuration);
		unfocusedCover.DOAlpha(value ? 0f : 0.7058824f, duration);
		indicator.DOAlpha(value ? 0.5803922f : 0f, duration);
	}
}
