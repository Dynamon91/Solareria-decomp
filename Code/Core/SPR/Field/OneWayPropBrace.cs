using System.Collections.Generic;
using UnityEngine;

namespace SPR.Field;

public class OneWayPropBrace : OneWayPropSimple
{
	[SerializeField]
	private List<Sprite> knotSprites;

	[Header("References")]
	[SerializeField]
	private SpriteRenderer knotRenderer;

	public override void Initialize(int slope)
	{
		if (knotSprites.Count > 1)
		{
			knotRenderer.sprite = knotSprites[Random.Range(0, knotSprites.Count)];
		}
		base.Initialize(slope);
	}
}
