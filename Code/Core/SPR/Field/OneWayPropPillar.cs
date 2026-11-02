using System.Collections.Generic;
using UnityEngine;

namespace SPR.Field;

public class OneWayPropPillar : OneWayPropSupport
{
	[Header("Pillar")]
	[SerializeField]
	private SpriteRenderer pillarBottomSprite;

	[SerializeField]
	private List<Sprite> pillarBottomSprites;

	protected override void OnInitializeSupport(int distance)
	{
		pillarBottomSprite.transform.localPosition = new Vector3(0f, -distance, 0f);
		if (pillarBottomSprites.Count > 0)
		{
			pillarBottomSprite.sprite = pillarBottomSprites[Random.Range(0, pillarBottomSprites.Count)];
		}
	}
}
