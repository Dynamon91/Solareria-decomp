using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class MapSpecialMarkerIcon : MapMarkerIcon
{
	protected override bool ShouldBeActive => MapMarkerIcon.MapData.GetSpecialStatus(base.MarkerType) == MapMarkerStatus.Active;

	public void SetScale(float scale)
	{
		base.RectTransform.localScale = new Vector3(scale, scale, 1f);
	}
}
