using UnityEngine;

namespace SPR.Field;

public class OneWayPropBottom : OneWayProp
{
	public override void Initialize(int slope)
	{
	}

	public void InitializeWithLength(int length)
	{
		base.SpriteRenderer.size = new Vector2(length, base.SpriteRenderer.size.y);
	}
}
