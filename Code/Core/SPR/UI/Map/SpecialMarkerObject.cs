using Library;
using UnityEngine;

namespace SPR.UI.Map;

public class SpecialMarkerObject : MonoBehaviour, IMappable
{
	[SerializeField]
	private int markerType;

	[SerializeField]
	private float offsetY;

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		SpineObject componentInChildren = base.gameObject.GetComponentInChildren<SpineObject>();
		Vector2 position = (componentInChildren.IsNull() ? base.transform.position.ToVector2() : componentInChildren.VisualBottom) - center + new Vector2(0f, offsetY);
		return new MapObjectInfo(MapObjectType.SpecialMarker, position, markerType);
	}
}
