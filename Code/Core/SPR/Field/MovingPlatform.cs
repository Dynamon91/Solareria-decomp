using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR.Field;

public class MovingPlatform : MonoBehaviour
{
	[SerializeField]
	private Vector2 size;

	private BoxCollider2D coll;

	private SpriteRenderer sr;

	private List<Vector2> range;

	public Vector2 Size => size;

	private BoxCollider2D BoxColl => coll ?? (coll = GetComponent<BoxCollider2D>());

	private SpriteRenderer SpriteRenderer => sr ?? (sr = GetComponent<SpriteRenderer>());

	public void ModifyPath(bool setGround)
	{
		if (!setGround)
		{
			if (!range.IsNullOrEmpty())
			{
				Game.Instance.FieldController.ModifyPathGraphs(range, setGround: false);
			}
			return;
		}
		range = new List<Vector2>();
		Vector3 position = base.transform.position;
		for (float num = (0f - size.x) / 2f; num < size.x / 2f; num += 1f)
		{
			range.Add(new Vector2(num, position.y + size.y / 2f));
		}
		Game.Instance.FieldController.ModifyPathGraphs(range, setGround: true);
	}
}
