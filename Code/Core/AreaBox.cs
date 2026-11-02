using System;
using Library;
using UnityEngine;

[Serializable]
public struct AreaBox
{
	[SerializeField]
	private Vector2Int lowerLeft;

	[SerializeField]
	private Vector2Int upperRight;

	public Vector2 Center => (lowerLeft.ToVector2() + upperRight.ToVector2()) / 2f;

	public Vector2Int Size => upperRight - lowerLeft;

	public Vector2Int LowerLeft => lowerLeft;

	public Vector2Int UpperRight => upperRight;

	public AreaBox(Vector2Int lowerLeft, Vector2Int upperRight)
	{
		this.lowerLeft = lowerLeft;
		this.upperRight = upperRight;
	}

	public bool IsInArea(Vector2 pos)
	{
		if ((float)lowerLeft.x <= pos.x && pos.x <= (float)upperRight.x && (float)lowerLeft.y <= pos.y && pos.y <= (float)upperRight.y)
		{
			return true;
		}
		return false;
	}

	public bool IsInArea(Vector2 pos, float multiplier)
	{
		float num = (float)Size.x * multiplier / 2f;
		float num2 = (float)Size.y * multiplier / 2f;
		if ((float)lowerLeft.x - num <= pos.x && pos.x <= (float)upperRight.x + num && (float)lowerLeft.y - num2 <= pos.y && pos.y <= (float)upperRight.y + num2)
		{
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		return "Area Center = " + Center.ToString() + ", Area Size = " + Size.ToString();
	}

	public Vector2[] ToPath()
	{
		return new Vector2[4]
		{
			new Vector2(lowerLeft.x, lowerLeft.y),
			new Vector2(upperRight.x, lowerLeft.y),
			new Vector2(upperRight.x, upperRight.y),
			new Vector2(lowerLeft.x, upperRight.y)
		};
	}

	public void MoveBox(Vector2Int delta)
	{
		lowerLeft += delta;
		upperRight += delta;
	}

	public Vector2 Clamp(Vector2 pos)
	{
		float x = Mathf.Clamp(pos.x, lowerLeft.x, upperRight.x);
		float y = Mathf.Clamp(pos.y, lowerLeft.y, upperRight.y);
		return new Vector2(x, y);
	}
}
