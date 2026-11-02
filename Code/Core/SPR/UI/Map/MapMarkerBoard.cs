using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapMarkerBoard : MonoBehaviour
{
	public const Ease Easing = Ease.InOutQuad;

	private const float ConstantX = -80f;

	[SerializeField]
	private RectTransform normalMarkersParent;

	[SerializeField]
	private RectTransform specialMarkersParent;

	private CanvasGroup canvasGroup;

	private CanvasGroup normalMarkersCanvasGroup;

	private CanvasGroup specialMarkersCanvasGroup;

	private MapCursor markerCursor;

	private List<MapMarkerBoardIcon> normalMarkerIcons;

	private List<MapMarkerBoardIcon> specialMarkerIcons;

	private int normalNum;

	private int specialNum;

	private int normalIndex;

	private int specialIndex;

	private float fadeDuration;

	private Sequence sequence;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static MapData MapData => Game.Instance.SaveData.WorldData.MapData;

	public bool IsSpecialMarker { get; private set; }

	private int CurrentIndex
	{
		get
		{
			if (!IsSpecialMarker)
			{
				return normalIndex;
			}
			return specialIndex;
		}
		set
		{
			if (IsSpecialMarker)
			{
				specialIndex = value;
				if (specialIndex >= specialNum)
				{
					specialIndex -= specialNum;
				}
				return;
			}
			normalIndex = value;
			if (normalIndex >= normalNum)
			{
				normalIndex -= normalNum;
			}
			MapData.LastNormalIndex = normalIndex;
		}
	}

	public int CurrentMarkerType
	{
		get
		{
			if (!IsSpecialMarker)
			{
				return -1 * (normalIndex + 1);
			}
			return specialIndex + 1;
		}
	}

	private int IconNum
	{
		get
		{
			if (!IsSpecialMarker)
			{
				return normalNum;
			}
			return specialNum;
		}
	}

	public bool HasAnySpecialMarker => MapData.HasAnySpecialMarker();

	public void Initialize(float duration)
	{
		if ((object)canvasGroup == null)
		{
			canvasGroup = GetComponent<CanvasGroup>();
		}
		if ((object)normalMarkersCanvasGroup == null)
		{
			normalMarkersCanvasGroup = normalMarkersParent.GetComponent<CanvasGroup>();
		}
		if ((object)specialMarkersCanvasGroup == null)
		{
			specialMarkersCanvasGroup = specialMarkersParent.GetComponent<CanvasGroup>();
		}
		if ((object)markerCursor == null)
		{
			markerCursor = GetComponentInChildren<MapCursor>();
		}
		if (normalMarkerIcons.IsNullOrEmpty())
		{
			normalMarkerIcons = normalMarkersParent.GetComponentsInChildren<MapMarkerBoardIcon>().ToList();
			normalNum = (normalMarkerIcons.Count - 3) / 2;
		}
		if (specialMarkerIcons.IsNullOrEmpty())
		{
			specialMarkerIcons = specialMarkersParent.GetComponentsInChildren<MapMarkerBoardIcon>().ToList();
			specialNum = (specialMarkerIcons.Count - 3) / 2;
		}
		fadeDuration = duration;
	}

	public void SetActive(bool active, float duration, Ease easing)
	{
		if (active)
		{
			IsSpecialMarker = false;
			ChangeMode(instant: true);
			ResetCursor();
			SetNormalData();
			SetSpecialData();
		}
		int num = (active ? 1 : 0);
		int startAlpha = ((!active) ? 1 : 0);
		sequence?.Kill();
		sequence = DOTween.Sequence().Join(canvasGroup.DOAlpha(num, duration).SetEase(easing)).OnStart(delegate
		{
			canvasGroup.alpha = startAlpha;
		});
	}

	public bool TrySetNormalMarker(MapNormalMarkerIcon currentFocus, Vector2 worldConstant, out bool newIcon, out MapNormalMarkerIcon resultIcon)
	{
		newIcon = false;
		resultIcon = null;
		int currentMarkerType = CurrentMarkerType;
		if (!currentFocus.IsNull())
		{
			int markerType = currentFocus.MarkerType;
			int num = 0;
			if (markerType != currentMarkerType)
			{
				int num2 = -currentMarkerType;
				int num3 = -markerType;
				int num4 = ((num2 < num3) ? (num3 - num2) : (num3 - num2 + normalNum));
				int num5 = ((num3 < num2) ? (num3 - num2) : (num3 - num2 - normalNum));
				num = ((-num5 < num4) ? num5 : num4);
				DebugLog.GeneralLog($"current={num2}, focus={num3}, direction={num}");
			}
			if (!MapData.TryRemoveActiveNormalIcon(markerType, currentFocus.WorldPositionConstant))
			{
				return false;
			}
			if (num != 0)
			{
				MoveCursor(-currentMarkerType - 1, num);
			}
			PlayerData.AddAcquiredItem(MapMarkerIcon.GetMarkerName(markerType));
			SetNormalIconColorAndNum(markerType);
			return true;
		}
		if (PlayerData.TryRemoveAcquiredItem(MapMarkerIcon.GetMarkerName(currentMarkerType)))
		{
			MapData.AddActiveNormalIcon(currentMarkerType, worldConstant);
			SetNormalIconColorAndNum(currentMarkerType);
			newIcon = true;
			resultIcon = Game.Instance.ObjectPoolManager.GetObject<MapNormalMarkerIcon>("Map Normal Marker Icon.prefab");
			return true;
		}
		DebugLog.GeneralLogWarning($"지도 일반 마커({currentMarkerType})의 개수가 모자랍니다!");
		return false;
	}

	public bool TrySetSpecialMarker(MapSpecialMarkerIcon icon, out bool result)
	{
		int currentMarkerType = CurrentMarkerType;
		switch (MapData.GetSpecialStatus(currentMarkerType))
		{
		case MapMarkerStatus.Standby:
			result = true;
			MapData.SetSpecialStatus(currentMarkerType, MapMarkerStatus.Active);
			icon.Activate(fadeDuration);
			SetSpecialIconColor(currentMarkerType);
			return true;
		case MapMarkerStatus.Active:
			result = false;
			MapData.SetSpecialStatus(currentMarkerType, MapMarkerStatus.Standby);
			icon.Deactivate(fadeDuration);
			SetSpecialIconColor(currentMarkerType);
			return true;
		default:
			result = false;
			return false;
		}
	}

	public bool TryMoveCursor(int direction, out int index)
	{
		index = CurrentIndex;
		if (sequence.IsActive())
		{
			return false;
		}
		if (direction != -1 && direction != 1)
		{
			DebugLog.GeneralLogError($"마커 UI를 이동시키는 방향({direction})이 1 또는 -1이 아닙니다! 입력을 무시합니다.");
			return false;
		}
		int num = index;
		int num2 = direction;
		while (true)
		{
			if (num + num2 >= IconNum)
			{
				num -= IconNum;
			}
			if (num + num2 < 0)
			{
				num += IconNum;
			}
			if (num + num2 == index)
			{
				return false;
			}
			if (IsSpecialMarker ? ((byte)MapData.GetSpecialStatus(num + num2 + 1) != 0) : PlayerData.HasEverObtainedItem(MapMarkerIcon.GetMarkerName(-num - 1 - num2), out var _))
			{
				break;
			}
			num2 += direction;
		}
		MoveCursor(index, num2);
		index = CurrentIndex;
		return true;
	}

	public bool TryChangeMode(out bool isSpecial)
	{
		isSpecial = IsSpecialMarker;
		if (!HasAnySpecialMarker)
		{
			return false;
		}
		if (sequence.IsActive())
		{
			return false;
		}
		IsSpecialMarker = !IsSpecialMarker;
		ChangeMode();
		isSpecial = IsSpecialMarker;
		return true;
	}

	private void SetData()
	{
	}

	private void SetNormalData()
	{
		for (int i = 0; i < normalNum; i++)
		{
			SetNormalIconData(-1 * (i + 1));
		}
	}

	private void SetSpecialData()
	{
		for (int i = 0; i < specialNum; i++)
		{
			SetSpecialIconData(i + 1);
		}
	}

	private void SetIconData(int markerType)
	{
		if (markerType < 0)
		{
			SetNormalIconData(markerType);
		}
		else
		{
			SetSpecialIconData(markerType);
		}
	}

	private void SetNormalIconData(int markerType)
	{
		int count = MapData.GetNormalIcons(markerType).Count;
		int acquiredItemCount = PlayerData.GetAcquiredItemCount(MapMarkerIcon.GetMarkerName(markerType));
		foreach (MapMarkerBoardIcon item in normalMarkerIcons.Where((MapMarkerBoardIcon icon) => icon.MarkerType == markerType))
		{
			item.SetData(acquiredItemCount + count == 0, acquiredItemCount == 0, acquiredItemCount);
		}
	}

	private void SetNormalIconColorAndNum(int markerType)
	{
		int acquiredItemCount = PlayerData.GetAcquiredItemCount(MapMarkerIcon.GetMarkerName(markerType));
		foreach (MapMarkerBoardIcon item in normalMarkerIcons.Where((MapMarkerBoardIcon icon) => icon.MarkerType == markerType))
		{
			item.SetColor(acquiredItemCount == 0);
			item.SetNum(acquiredItemCount);
		}
	}

	private void SetSpecialIconData(int markerType)
	{
		MapMarkerStatus specialStatus = MapData.GetSpecialStatus(markerType);
		foreach (MapMarkerBoardIcon item in specialMarkerIcons.Where((MapMarkerBoardIcon icon) => icon.MarkerType == markerType))
		{
			item.SetData(specialStatus == MapMarkerStatus.Unavailable, specialStatus == MapMarkerStatus.Active, -1);
		}
	}

	private void SetSpecialIconColor(int markerType)
	{
		MapMarkerStatus specialStatus = MapData.GetSpecialStatus(markerType);
		foreach (MapMarkerBoardIcon item in specialMarkerIcons.Where((MapMarkerBoardIcon icon) => icon.MarkerType == markerType))
		{
			item.SetColor(specialStatus == MapMarkerStatus.Active);
		}
	}

	private void ChangeMode(bool instant = false)
	{
		int num = ((!IsSpecialMarker) ? 1 : 0);
		int num2 = (IsSpecialMarker ? 1 : 0);
		int num3 = (IsSpecialMarker ? (-10) : 0);
		if (instant)
		{
			normalMarkersCanvasGroup.alpha = num;
			specialMarkersCanvasGroup.alpha = num2;
			normalMarkersParent.anchoredPosition = new Vector2(normalMarkersParent.anchoredPosition.x, num3);
		}
		else
		{
			normalMarkersParent.anchoredPosition = new Vector2(normalMarkersParent.anchoredPosition.x, (!IsSpecialMarker) ? (-10) : 0);
			sequence?.Kill();
			sequence = DOTween.Sequence();
			sequence.Join(normalMarkersCanvasGroup.DOFade(num, fadeDuration).SetEase(Ease.InOutQuad)).Join(specialMarkersCanvasGroup.DOFade(num2, fadeDuration).SetEase(Ease.InOutQuad)).Join(normalMarkersParent.DOAnchorPosY(num3, fadeDuration).SetEase(Ease.InOutQuad));
		}
	}

	private void MoveCursor(int initialIndex, int direction)
	{
		RectTransform targetParent = (IsSpecialMarker ? specialMarkersParent : normalMarkersParent);
		int num = initialIndex + direction;
		int num2 = (IconNum - 1) * 2;
		if (initialIndex < 0 || num < 0)
		{
			initialIndex += IconNum;
			num += IconNum;
		}
		else if (initialIndex > num2 || num > num2)
		{
			initialIndex -= IconNum;
			num -= IconNum;
		}
		if (num > IconNum - 1)
		{
			CurrentIndex = num - IconNum;
		}
		CurrentIndex = num;
		sequence?.Kill();
		sequence = DOTween.Sequence();
		sequence.Join(targetParent.DOAnchorPosX((float)num * -80f, fadeDuration).SetEase(Ease.InOutQuad)).OnStart(delegate
		{
			targetParent.anchoredPosition = new Vector2((float)initialIndex * -80f, 0f);
			markerCursor.SetFocus(value: true);
		}).OnComplete(delegate
		{
			targetParent.anchoredPosition = new Vector2((float)CurrentIndex * -80f, 0f);
		});
	}

	private void ResetCursor()
	{
		specialMarkersParent.anchoredPosition = Vector2.zero;
		normalMarkersParent.anchoredPosition = Vector2.zero;
		normalIndex = GetNormalIndex();
		normalMarkersParent.anchoredPosition = new Vector2((float)normalIndex * -80f, 0f);
		specialIndex = 0;
	}

	private int GetNormalIndex()
	{
		int lastNormalIndex = MapData.LastNormalIndex;
		if (lastNormalIndex < 0)
		{
			for (int i = 1; i <= normalNum; i++)
			{
				if (PlayerData.HasEverObtainedItem(MapMarkerIcon.GetMarkerName(-i), out var _))
				{
					return i - 1;
				}
			}
			return 0;
		}
		return lastNormalIndex;
	}
}
