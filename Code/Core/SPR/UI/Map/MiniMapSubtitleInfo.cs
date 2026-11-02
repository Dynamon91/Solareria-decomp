using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.UI.Map;

[CreateAssetMenu]
public class MiniMapSubtitleInfo : ScriptableObject
{
	[SerializeField]
	private Color defaultColor;

	[Tooltip("지도 색상을 적용할 때, 불투명도를 Global Alpha 값으로 덮어 씌울지를 결정합니다.\ntrue면, Default Color의 Alpha 값을 각 색상에 적용하여 지도를 생성합니다.\nfalse면, 개별 색상의 불투명도를 그대로 사용합니다.")]
	[SerializeField]
	private bool useGlobalAlpha;

	[Tooltip("지도 색상을 적용할 때, 세부 지도와 전체 지도의 Alpha 값을 다르게 할지 결정합니다.\ntrue면, 세부 지도에 Map Part Alpha 값을 각 색상에 적용하여 지도를 생성합니다.\nfalse면, Use Global Alpha에 따라 개별 색상 Alpha 값 또는 Global Alpha를 적용합니다.")]
	[SerializeField]
	private bool separateMapPartAlpha;

	[SerializeField]
	[Range(0f, 1f)]
	private float mapPartAlpha;

	[SerializeField]
	private List<MapSubtitleInfo> mapColorInfos;

	public Sprite GetFillSprite(FieldSubtitle field)
	{
		return mapColorInfos.First((MapSubtitleInfo i) => i.field == field).fillSprite;
	}

	public Sprite GetBorderSprite(FieldSubtitle field)
	{
		return mapColorInfos.First((MapSubtitleInfo i) => i.field == field).borderSprite;
	}

	public Color GetFillColor(FieldSubtitle field, bool isFull = true)
	{
		MapSubtitleInfo mapSubtitleInfo = mapColorInfos.First((MapSubtitleInfo i) => i.field == field);
		Color color = (mapSubtitleInfo.Equals(null) ? defaultColor : mapSubtitleInfo.fillColor);
		float a = ((!isFull && separateMapPartAlpha) ? mapPartAlpha : (useGlobalAlpha ? defaultColor.a : color.a));
		return new Color(color.r, color.g, color.b, a);
	}

	public Color GetBorderColor(FieldSubtitle field)
	{
		MapSubtitleInfo mapSubtitleInfo = mapColorInfos.First((MapSubtitleInfo i) => i.field == field);
		if (!mapSubtitleInfo.Equals(null))
		{
			return mapSubtitleInfo.borderColor;
		}
		return defaultColor;
	}

	public void OnValidate()
	{
		for (int i = 0; i < mapColorInfos.Count; i++)
		{
			MapSubtitleInfo value = mapColorInfos[i];
			value.Name = value.field.ToString();
			mapColorInfos[i] = value;
		}
	}
}
