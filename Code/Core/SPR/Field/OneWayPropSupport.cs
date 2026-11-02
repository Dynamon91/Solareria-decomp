using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class OneWayPropSupport : OneWayProp
{
	public enum SupportType
	{
		Left,
		Middle,
		Right
	}

	[SerializeField]
	private List<Sprite> knotSprites;

	[SerializeField]
	private List<Sprite> supportSprites;

	[SerializeField]
	private List<Sprite> ySupportSprites;

	[SerializeField]
	private float ySupportDistance;

	[SerializeField]
	private bool randomizeRotation;

	[Tooltip("true라면 Y Support의 X Scale을 랜덤으로 뒤집음")]
	[SerializeField]
	private bool randomizeXScaleSignOfYSupport;

	[Header("References")]
	[SerializeField]
	private SpriteRenderer supportRenderer;

	[SerializeField]
	private SpriteRenderer additionalKnotRenderer;

	[SerializeField]
	private SpriteRenderer ySupportRenderer;

	private SupportType supportType;

	public void SetType(SupportType type)
	{
		supportType = type;
	}

	public sealed override void Initialize(int slope)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, Vector2.down, 100f, LayerUtility.PlatformLayer);
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
		if (supportSprites.Count > 1)
		{
			supportRenderer.sprite = supportSprites[Random.Range(0, supportSprites.Count)];
		}
		int distance = raycastHit2D.distance.RoundToInt();
		switch (supportType)
		{
		case SupportType.Left:
		case SupportType.Right:
			if (CryptoRandom.RandomBool())
			{
				CreateYSupport(distance, supportType);
			}
			else
			{
				CreateStraightSupport(distance);
			}
			break;
		case SupportType.Middle:
			CreateStraightSupport(distance);
			break;
		}
		OnInitializeSupport(distance);
		base.gameObject.SetActive(value: true);
	}

	protected virtual void OnInitializeSupport(int distance)
	{
	}

	private void CreateStraightSupport(int distance)
	{
		supportRenderer.size = new Vector2(supportRenderer.size.x, distance + 1);
		if (!additionalKnotRenderer.IsNull())
		{
			additionalKnotRenderer.enabled = false;
		}
		ySupportRenderer.gameObject.SetActive(value: false);
	}

	private void CreateYSupport(int distance, SupportType type)
	{
		float num = (randomizeRotation ? ((float)CryptoRandom.Range(-2, 5) / 2f) : 0f);
		float y = Mathf.Sqrt((float)(distance * distance) + num * num) + 1f;
		float num2 = Mathf.Atan(num / (float)distance) * 57.29578f;
		int num3 = ((type == SupportType.Left) ? 1 : (-1));
		supportRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, (float)num3 * num2);
		supportRenderer.size = new Vector2(supportRenderer.size.x, y);
		Transform transform = ySupportRenderer.transform;
		transform.localPosition = new Vector3((float)num3 * ySupportDistance, transform.localPosition.y, 0f);
		transform.localRotation = Quaternion.Euler(0f, 0f, (float)num3 * num2);
		transform.localScale = transform.localScale.FlipX(num3 > 0).FlipX(randomizeXScaleSignOfYSupport && CryptoRandom.RandomBool());
		transform.gameObject.SetActive(value: true);
		if (ySupportSprites.Count > 0)
		{
			ySupportRenderer.sprite = ySupportSprites[Random.Range(0, ySupportSprites.Count)];
		}
		if (!additionalKnotRenderer.IsNull())
		{
			additionalKnotRenderer.transform.localPosition = new Vector3((float)num3 * ySupportDistance, 0f, 0f);
			if (CryptoRandom.RandomBool())
			{
				additionalKnotRenderer.enabled = true;
				additionalKnotRenderer.sprite = knotSprites[Random.Range(0, knotSprites.Count)];
			}
			else
			{
				additionalKnotRenderer.enabled = false;
			}
		}
	}
}
