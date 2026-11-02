using UnityEngine;

namespace SPR.UI.Map;

public class MapLegendObject : MonoBehaviour
{
	[SerializeField]
	private MapObjectType type;

	[SerializeField]
	private GameObject temp_highlight;

	public MapObjectType Type => type;

	public void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public void ShowNew(bool value)
	{
		temp_highlight.SetActive(value);
	}
}
