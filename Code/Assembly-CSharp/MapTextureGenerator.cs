using System.Collections.Generic;
using SPR.Field;
using UnityEngine;

public class MapTextureGenerator : MonoBehaviour
{
	[SerializeField]
	private int texturePPU;

	[SerializeField]
	private float checkMargin;

	[SerializeField]
	private bool fillGround;

	[SerializeField]
	private bool reverseWhite;

	private Collider2D platform;

	private Collider2D areaCollider;

	private Area area;

	private int totalX;

	private int totalY;

	private Vector2Int lowerLeft;

	private Vector2Int upperRight;

	private List<Collider2D> overlapResults;

	private bool[,] pixelInfo;

	private Color[] whites;

	private Color[] transparents;

	private Color[] colorTrue;

	private Color[] colorFalse;
}
