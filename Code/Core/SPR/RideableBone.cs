using UnityEngine;

namespace SPR;

public class RideableBone : MonoBehaviour
{
	private Vector2 oldPosition;

	private Vector2 nowPosition;

	public void ManualFixedUpdate()
	{
		if (nowPosition == Vector2.zero)
		{
			nowPosition = base.transform.position;
		}
		oldPosition = nowPosition;
		nowPosition = base.transform.position;
	}

	public Vector2 GetVelocity()
	{
		return (nowPosition - oldPosition) / Time.fixedDeltaTime;
	}
}
