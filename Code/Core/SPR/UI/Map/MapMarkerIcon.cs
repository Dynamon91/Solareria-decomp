using System;
using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapMarkerIcon : MapIcon
{
	[SerializeField]
	private int markerType;

	private static readonly List<FieldSubtitle> SpecialMarkerList = new List<FieldSubtitle>
	{
		FieldSubtitle.SilentRuins,
		FieldSubtitle.Earthford,
		FieldSubtitle.TerradinGarden,
		FieldSubtitle.WatermirrorBasin,
		FieldSubtitle.UndelCaverns,
		FieldSubtitle.Library,
		FieldSubtitle.AshHills,
		FieldSubtitle.FadedCastle,
		FieldSubtitle.WindshadeSpire,
		FieldSubtitle.MucoraFortress
	};

	protected override bool ShouldBeActive => true;

	public int MarkerType
	{
		get
		{
			return markerType;
		}
		protected set
		{
			markerType = value;
		}
	}

	protected static MapData MapData => Game.Instance.SaveData.WorldData.MapData;

	public static string GetMarkerName(int markerType)
	{
		if (markerType >= 0)
		{
			return MarkerType2String(markerType) + "Marker";
		}
		return $"MapMarkerNormal{markerType * -1}";
	}

	public static int GetMarkerType(string itemID)
	{
		if (itemID.StartsWith("MapMarkerNormal") && itemID.Replace("MapMarkerNormal", string.Empty).TryParseInt(out var result))
		{
			return result * -1;
		}
		return String2MarkerType(itemID);
	}

	private static string MarkerType2String(int markerType)
	{
		try
		{
			return SpecialMarkerList[markerType - 1].ToString();
		}
		catch (IndexOutOfRangeException)
		{
			DebugLog.GeneralLogError($"주어진 MarkerType({markerType})을 특수 마커로 변환할 수 없습니다!");
			throw;
		}
	}

	private static int String2MarkerType(string itemID)
	{
		try
		{
			Enum.TryParse<FieldSubtitle>(itemID.Replace("Marker", string.Empty), out var result);
			return SpecialMarkerList.IndexOf(result) + 1;
		}
		catch (Exception)
		{
			DebugLog.GeneralLogError("주어진 ID(" + itemID + ")를 특수 마커로 변환할 수 없습니다!");
			throw;
		}
	}

	public void Activate(float duration)
	{
		iconImage.SetAlpha(0f);
		base.gameObject.SetActive(value: true);
		FadeAlpha(1f, duration, Ease.OutQuad);
	}

	public void Deactivate(float duration)
	{
		FadeAlpha(0f, duration, Ease.InQuad, AfterDeactivate);
	}

	protected void FadeAlpha(float end, float duration, Ease easing, Action onComplete = null)
	{
		iconImage.DOAlpha(end, duration).SetEase(easing).OnComplete(delegate
		{
			onComplete?.Invoke();
		});
	}

	protected virtual void AfterDeactivate()
	{
		base.gameObject.SetActive(value: false);
	}
}
