using System;
using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

[Serializable]
public struct Zone
{
	[SerializeField]
	private Vector2 lowerLeft;

	[SerializeField]
	private Vector2 upperRight;

	public Vector2 Center => (lowerLeft + upperRight) / 2f;

	public Vector2 Size => upperRight - lowerLeft;

	public Vector2 LowerLeft => lowerLeft;

	public Vector2 UpperRight => upperRight;

	public Zone(Vector2 center, Vector2 size)
	{
		lowerLeft = center - size / 2f;
		upperRight = center + size / 2f;
	}

	public bool Contains(Vector2 pos)
	{
		if (lowerLeft.x <= pos.x && pos.x <= upperRight.x && lowerLeft.y <= pos.y)
		{
			return pos.y <= upperRight.y;
		}
		return false;
	}

	public bool Contains(Bounds bounds)
	{
		return new Bounds(Center, Size).Intersects(bounds);
	}

	public Zone GetZoneOf(Transform transform)
	{
		Vector2 center = (Vector2)transform.position + new Vector2(Center.x * (float)transform.localScale.x.GetSign(), Center.y);
		Vector2 size = Size;
		return new Zone(center, size);
	}

	public Zone GetZoneOf(SpineObject spineObject)
	{
		Vector2 center = spineObject.VisualCenter + new Vector2(Center.x * (float)spineObject.FacingDirection.GetSignX(), Center.y);
		Vector2 size = Size;
		return new Zone(center, size);
	}

	public Zone GetZoneOf(Vector2 position, int sign)
	{
		Vector2 center = position + new Vector2(Center.x * (float)sign, Center.y);
		Vector2 size = Size;
		return new Zone(center, size);
	}

	public List<Zone> SplitByX(float anchorX)
	{
		Vector2 size = new Vector2(Mathf.Abs(lowerLeft.x - anchorX), Size.y);
		Vector2 center = new Vector2(lowerLeft.x + size.x / 2f, Center.y);
		Vector2 size2 = new Vector2(Size.x - size.x, Size.y);
		Vector2 center2 = new Vector2(anchorX + size2.x / 2f, Center.y);
		Zone item = new Zone(center, size);
		Zone item2 = new Zone(center2, size2);
		return new List<Zone> { item, item2 };
	}
}
