using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR.Field;

public struct GroundGroup
{
	private List<GroundInfo> groundTiles;

	public IReadOnlyList<GroundInfo> GroundTiles => groundTiles;

	public static GroundGroup Disable => default(GroundGroup);

	public void Add((Vector2 pos, Vector2 normal) info)
	{
		if (groundTiles == null)
		{
			groundTiles = new List<GroundInfo>();
		}
		if (!Contains(info.pos))
		{
			groundTiles.Add(new GroundInfo(info.pos, info.normal, this));
		}
	}

	public bool IsNear(Vector2 tilePosition)
	{
		bool result = false;
		foreach (GroundInfo groundTile in groundTiles)
		{
			if (Mathf.Abs(groundTile.X - tilePosition.x) < 0.01f)
			{
				return false;
			}
			if (Mathf.Abs(groundTile.Y - tilePosition.y) <= 0.55f && Mathf.Abs(groundTile.X - tilePosition.x) <= 1.05f)
			{
				result = true;
			}
		}
		return result;
	}

	public bool IsNearWith(Vector2 target, out int index)
	{
		index = -1;
		for (int i = 0; i < groundTiles.Count; i++)
		{
			if (Mathf.Abs(groundTiles[i].Y - target.y) <= 0.55f && Mathf.Abs(groundTiles[i].X - target.x) <= 1.05f)
			{
				index = i;
				return true;
			}
		}
		return false;
	}

	public void Sort()
	{
		groundTiles.Sort((GroundInfo a, GroundInfo b) => a.X.CompareTo(b.X));
	}

	public bool Contains(Vector2 target)
	{
		return groundTiles.Any((GroundInfo tile) => tile.Position == target);
	}

	public bool ContainsX(float x)
	{
		return groundTiles.Any((GroundInfo tile) => Mathf.Abs(tile.X - x) < 0.01f);
	}

	public bool IsInSameGroup(Vector2 a, Vector2 b)
	{
		Vector2 target = new Vector2((float)(a.x - 0.5f).RoundToInt() + 0.5f, a.y.SnapToQuarter());
		Vector2 target2 = new Vector2((float)(b.x - 0.5f).RoundToInt() + 0.5f, b.y.SnapToQuarter());
		if (Contains(target))
		{
			return Contains(target2);
		}
		return false;
	}

	public Vector2 GetRandomGroundPosition(int start, int end, int creatureWidth)
	{
		int num = Mathf.CeilToInt((float)creatureWidth / 2f);
		start = Mathf.Max(start, num);
		end = Mathf.Min(end, groundTiles.Count - 1 - num);
		int index = CryptoRandom.Range(start, end);
		return groundTiles[index].Position;
	}

	public GroundInfo GetProjectionGroundInfo(Vector3 position, float distanceThreshold)
	{
		float num = (float)(position.x + 0.5f).RoundToInt() - 0.5f;
		foreach (GroundInfo groundTile in groundTiles)
		{
			float num2 = position.y - groundTile.Y;
			if (Mathf.Abs(groundTile.X - num) <= 0.01f && 0f <= num2 && num2 <= distanceThreshold)
			{
				return groundTile;
			}
		}
		return GroundInfo.Disable;
	}

	public Vector2 GetFarthestGroundPosition(Vector2 start, float distance, bool isLeft, bool checkSlope = false)
	{
		Vector2 target = start + new Vector2(distance * (float)((!isLeft) ? 1 : (-1)), 0f);
		if (checkSlope)
		{
			IsNearWith(start, out var index);
			if (isLeft)
			{
				int b = index - Mathf.RoundToInt(distance);
				for (int i = Mathf.Max(0, b); i < groundTiles.Count; i++)
				{
					if (groundTiles[i].Y.IsAlmostInteger())
					{
						return groundTiles[i].Position;
					}
				}
			}
			else
			{
				int b2 = index + Mathf.RoundToInt(distance);
				for (int num = Mathf.Min(groundTiles.Count - 1, b2); num >= 0; num--)
				{
					if (groundTiles[num].Y.IsAlmostInteger())
					{
						return groundTiles[num].Position;
					}
				}
			}
		}
		if (!IsNearWith(target, out var index2))
		{
			List<GroundInfo> list = groundTiles;
			return list[(isLeft ? ((Index)0) : (^1)).GetOffset(list.Count)].Position;
		}
		return groundTiles[index2].Position;
	}

	public bool IsThereGroundFrom(Vector2 start, float delta)
	{
		if (Contains(start))
		{
			return ContainsX(start.x + delta);
		}
		return false;
	}

	public Vector2 GetTileFromBelow(Vector3 position)
	{
		float num = (float)(position.x + 0.5f).RoundToInt() - 0.5f;
		foreach (GroundInfo groundTile in groundTiles)
		{
			if (Mathf.Abs(groundTile.Position.x - num) <= Mathf.Epsilon && groundTile.Position.y < position.y)
			{
				return groundTile.Position;
			}
		}
		return Util.DisablePosition;
	}

	public Vector2 GetTile(int index)
	{
		return groundTiles[index].Position;
	}
}
