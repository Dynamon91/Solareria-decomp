using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class OneWayPropRope : OneWayProp
{
	[SerializeField]
	private List<Sprite> knotSprites;

	[Header("References")]
	[SerializeField]
	private SpriteRenderer ropeRenderer;

	public override void Initialize(int slope)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, Vector2.up, 100f, LayerUtility.PlatformLayer);
		if (raycastHit2D.collider == null)
		{
			Debug.LogError("Hanging을 매달 플랫폼이 너무 멀거나 없습니다.");
			base.gameObject.SetActive(value: false);
			return;
		}
		if (knotSprites.Count > 1)
		{
			base.SpriteRenderer.sprite = knotSprites[Random.Range(0, knotSprites.Count)];
		}
		int num = raycastHit2D.distance.RoundToInt();
		ropeRenderer.size = new Vector2(ropeRenderer.size.x, num);
		base.gameObject.SetActive(value: true);
	}
}
