using DG.Tweening;
using UnityEngine;

namespace SPR.Field;

public class HiddenDoor : MonoBehaviour
{
	[SerializeField]
	private GameObject targetObject;

	[SerializeField]
	private float targetDistance;

	private BoxCollider2D coll;

	private bool isOpen;

	private const float Speed = 10f;

	private BoxCollider2D BoxCollider => coll ?? (coll = GetComponentInChildren<BoxCollider2D>());

	private void FixedUpdate()
	{
		float num = Mathf.Abs(BoxCollider.transform.position.x - targetObject.transform.position.x);
		if (!isOpen && num <= targetDistance)
		{
			isOpen = true;
			OpenDoor(open: true);
		}
		else if (isOpen && num > targetDistance)
		{
			isOpen = false;
			OpenDoor(open: false);
		}
	}

	private void OpenDoor(bool open)
	{
		BoxCollider.transform.DOKill();
		float num = (open ? BoxCollider.bounds.size.y : 0f);
		float duration = Mathf.Abs(num - BoxCollider.transform.localPosition.y) / 10f;
		BoxCollider.transform.DOLocalMoveY(num, duration).SetEase(Ease.Linear);
	}
}
