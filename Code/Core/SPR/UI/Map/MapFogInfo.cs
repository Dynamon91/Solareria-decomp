using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class MapFogInfo
{
	[SerializeField]
	[ReadOnlyInspector]
	private string name;

	[SerializeField]
	public int index;

	[SerializeField]
	public Vector2Int lowerLeft;

	[SerializeField]
	public Vector2Int upperRight;

	public Vector2Int Size => upperRight - lowerLeft;

	public Vector2 Center => new Vector2(lowerLeft.x + upperRight.x, lowerLeft.y + upperRight.y) / 2f;

	public MapFogInfo(int index, Vector2Int lowerLeft, Vector2Int upperRight)
	{
		this.index = index;
		this.lowerLeft = lowerLeft;
		this.upperRight = upperRight;
		name = $"Map Fog [{index}]";
	}
}
