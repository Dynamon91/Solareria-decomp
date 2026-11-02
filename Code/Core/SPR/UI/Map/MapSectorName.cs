using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Map;

public class MapSectorName : MonoBehaviour
{
	[SerializeField]
	private LocaleText mainName;

	[SerializeField]
	private LocaleText subName;

	[SerializeField]
	private MapCollectionInfo collectionInfo;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private CanvasGroup subCanvasGroup;

	private FieldSubtitle currentField;

	private bool targetActive;

	private Sequence subSeq;

	private static string GetSectorKey(string sector)
	{
		return "Sector_" + sector;
	}

	private static string GetSubtitleKey(FieldSubtitle subtitle)
	{
		return $"Subtitle_{subtitle}";
	}

	private void Awake()
	{
		canvasGroup.alpha = 0f;
		subCanvasGroup.alpha = 0f;
	}

	public void SetActive(MapPart mapPart, float duration)
	{
		if (TrySetActive(!mapPart.IsNull(), duration))
		{
			SetData(mapPart.FieldID.GetSubtitle(), duration);
		}
	}

	public void SetActive(FullMapPart fullMapPart, float duration)
	{
		if (TrySetActive(!fullMapPart.IsNull(), duration))
		{
			SetData(fullMapPart.Type, duration);
		}
	}

	private bool TrySetActive(bool value, float duration)
	{
		if (targetActive == value)
		{
			return targetActive;
		}
		if (!targetActive)
		{
			subCanvasGroup.alpha = 0f;
		}
		targetActive = value;
		int num = (value ? 1 : 0);
		if (Mathf.Approximately(duration, 0f))
		{
			canvasGroup.alpha = num;
		}
		else
		{
			canvasGroup.DOKill();
			canvasGroup.DOAlpha(num, duration);
		}
		if (!targetActive)
		{
			currentField = FieldSubtitle.None;
		}
		return targetActive;
	}

	private void SetData(FieldSubtitle subtitle, float duration = 0f)
	{
		if (subtitle != currentField)
		{
			if (subtitle >= FieldSubtitle.Library && subtitle < FieldSubtitle.AshHills)
			{
				mainName.SetData(GetSubtitleKey(FieldSubtitle.Library));
				SetSubName(subtitle, FieldSubtitle.Library, duration);
			}
			else if (subtitle == FieldSubtitle.MintBureau)
			{
				mainName.SetData(GetSubtitleKey(subtitle));
				SetSubName(subtitle, subtitle, duration);
			}
			else
			{
				FieldSubtitle main = (FieldSubtitle)((int)subtitle >> 3 << 3);
				mainName.SetData(GetSectorKey(main.ToString()));
				SetSubName(subtitle, main, duration);
			}
			currentField = subtitle;
			collectionInfo.SetData(subtitle);
		}
	}

	private void SetSubName(FieldSubtitle sub, FieldSubtitle main, float duration = 0f)
	{
		if (sub == main)
		{
			if (Mathf.Approximately(duration, 0f))
			{
				subCanvasGroup.alpha = 0f;
				subName.ClearText();
			}
			else
			{
				subSeq?.Kill();
				subSeq = DOTween.Sequence(subCanvasGroup.DOAlpha(0f, duration));
			}
		}
		else
		{
			subName.SetData(GetSubtitleKey(sub));
			subSeq?.Kill();
			subSeq = DOTween.Sequence(subCanvasGroup.DOAlpha(1f, duration).OnStart(delegate
			{
				subCanvasGroup.alpha = 0f;
			}));
		}
	}
}
