using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace VinTools.BetterRuleTiles.Sample;

public class CustomPropertyTest : MonoBehaviour
{
	public Tilemap tilemap;

	public Camera cam;

	public event Action<string> onReadValue;

	private void Awake()
	{
		if (tilemap == null && base.gameObject.TryGetComponent<Tilemap>(out var component))
		{
			tilemap = component;
		}
		if (cam == null)
		{
			cam = Camera.main;
		}
	}

	private void Update()
	{
		if (tilemap == null)
		{
			Debug.LogError("Tilemap was not assigned!", this);
			return;
		}
		if (cam == null)
		{
			Debug.LogError("Camera was not assigned!", this);
			return;
		}
		Vector3Int position = tilemap.WorldToCell((Vector2)cam.ScreenToWorldPoint(Input.mousePosition));
		TileBase tile = tilemap.GetTile(position);
		if (tile != null && tile is BetterRuleTile)
		{
			BetterRuleTile obj = tile as BetterRuleTile;
			bool flag = obj.GetBool("Walkable");
			float num = obj.GetFloat("WalkingSpeed");
			this.onReadValue?.Invoke($"Tile: {tile.name}\nWalkable: {flag}\nWalking speed: {num}");
		}
		else if (tile != null)
		{
			this.onReadValue?.Invoke("Tile: " + tile.name);
		}
		else
		{
			this.onReadValue?.Invoke("Tile: Empty");
		}
	}
}
