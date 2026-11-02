using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR.Field;

public class OneWayPropSimple : OneWayProp
{
	[SerializeField]
	private bool parallelToSlope;

	[SerializeField]
	private bool randomizeRotation;

	[SerializeField]
	private float minScale;

	[SerializeField]
	private float maxScale;

	[SerializeField]
	private List<Sprite> propSprites;

	public override void Initialize(int slope)
	{
		if (propSprites.Count > 0)
		{
			base.SpriteRenderer.sprite = propSprites[Random.Range(0, propSprites.Count)];
		}
		float z = (parallelToSlope ? (26.56505f * (float)slope) : 0f) + (float)(randomizeRotation ? CryptoRandom.Range(-10, 10) : 0);
		base.transform.rotation = Quaternion.Euler(0f, 0f, z);
		float num = Random.Range(minScale, maxScale);
		base.transform.localScale = new Vector3(num, num, 1f);
		base.gameObject.SetActive(value: true);
	}
}
