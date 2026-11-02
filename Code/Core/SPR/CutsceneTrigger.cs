using System;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class CutsceneTrigger : MonoBehaviour
{
	public bool EventSubscribed => this.OnTriggered != null;

	public bool IsTriggered { get; protected set; }

	public event Action OnTriggered;

	private void FixedUpdate()
	{
		if (PhysicsUtility.IsPlayerStay(base.transform.position, base.transform.localScale))
		{
			IsTriggered = true;
			base.gameObject.SetActive(value: false);
			this.OnTriggered?.Invoke();
		}
	}

	public void ClearAction()
	{
		this.OnTriggered = null;
	}

	private void OnDrawGizmosSelected()
	{
		Color green = Color.green;
		green.a = 0.5f;
		Gizmos.color = green;
		Gizmos.DrawCube(base.transform.position, base.transform.localScale);
	}
}
