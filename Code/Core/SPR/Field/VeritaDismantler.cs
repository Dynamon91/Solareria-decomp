using System;
using UnityEngine;

namespace SPR.Field;

[Obsolete("베리타 분해기는 사용하지 않고 있습니다.")]
public class VeritaDismantler : TalkableObject
{
	private bool isActivated;

	private float rpm = 10f;

	private float amplitude = 0.05f;

	public Action Callback { get; set; }

	protected override void OnInteractOver()
	{
		if (Trigger.CheckTrigger("Trigger_VeritaQuestProgress"))
		{
			isActivated = true;
			base.Coll.enabled = false;
			Callback?.Invoke();
		}
	}

	private void Update()
	{
		if (isActivated)
		{
			float x = 1f + amplitude * Mathf.Cos(Time.time * rpm * 2f * MathF.PI);
			float y = 1f + amplitude * Mathf.Sin(Time.time * rpm * 2f * MathF.PI);
			base.transform.localScale = new Vector3(x, y, 1f);
		}
	}
}
