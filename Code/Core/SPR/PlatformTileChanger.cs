using Library;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SPR;

public class PlatformTileChanger : MonoBehaviour
{
	[SerializeField]
	private TileBase platformTile;

	[SerializeField]
	private TileBase ignoreTile;

	public void ChangeTile()
	{
		Tilemap component = GetComponent<Tilemap>();
		Bounds localBounds = component.localBounds;
		Vector2Int vector2Int = localBounds.min.ToVector2Int();
		Vector2Int vector2Int2 = localBounds.max.ToVector2Int();
		for (int i = vector2Int.x; i < vector2Int2.x; i++)
		{
			for (int j = vector2Int.y; j < vector2Int2.y; j++)
			{
				Vector3Int position = new Vector3Int(i, j, 0);
				if (component.HasTile(position) && (ignoreTile.IsNull() || !(component.GetTile(position) == ignoreTile)))
				{
					component.SetTile(position, platformTile);
				}
			}
		}
	}
}
