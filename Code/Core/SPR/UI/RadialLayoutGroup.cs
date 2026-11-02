using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPR.UI;

public class RadialLayoutGroup : MonoBehaviour
{
	[SerializeField]
	private float radius = 100f;

	[SerializeField]
	private Vector2 centerOffset;

	[SerializeField]
	private float angleOffset;

	[SerializeField]
	private float elementAngleOffset;

	[SerializeField]
	private List<RectTransform> elements;

	private bool cannotArrange;

	public int Count => elements.Count;

	public float AngleStep => 360f / (float)Count;

	private void ArrangeElements()
	{
		if (elements == null || Count == 0)
		{
			cannotArrange = true;
			return;
		}
		for (int i = 0; i < elements.Count; i++)
		{
			float num = angleOffset - (float)i * AngleStep + 90f;
			float f = num * (MathF.PI / 180f);
			Vector2 vector = new Vector2(Mathf.Cos(f), Mathf.Sin(f)) * radius;
			if ((object)elements[i] == null)
			{
				cannotArrange = true;
				continue;
			}
			elements[i].anchoredPosition = centerOffset + vector;
			elements[i].localRotation = Quaternion.Euler(0f, 0f, num + elementAngleOffset - 90f);
		}
		cannotArrange = false;
	}

	private void OnDrawGizmosSelected()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			ArrangeElements();
			if (cannotArrange)
			{
				DebugLog.GeneralLogWarning("RadialLayoutGroup의 일부 또는 전체를 정렬할 수 없습니다! 일단 가능한 요소들만 정렬했습니다.");
			}
		}
	}
}
