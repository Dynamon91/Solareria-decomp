using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR.Field;

public class FakeFloor : MonoBehaviour
{
	private BoxCollider2D coll;

	private List<Vector2> range;

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	public void SetActive(bool value)
	{
		Coll.enabled = value;
		base.gameObject.SetActive(value);
		if (value)
		{
			range = new List<Vector2>();
			Bounds bounds = Coll.bounds;
			for (float num = bounds.min.x; num < bounds.max.x; num += 1f)
			{
				range.Add(new Vector2(num, bounds.max.y));
			}
			Game.Instance.FieldController.ModifyPathGraphs(range, setGround: true);
		}
		else if (range != null)
		{
			Game.Instance.FieldController.ModifyPathGraphs(range, setGround: false);
		}
	}

	private void OnDisable()
	{
		if (!range.IsNullOrEmpty())
		{
			Game.Instance.FieldController.ModifyPathGraphs(range, setGround: false);
		}
	}
}
