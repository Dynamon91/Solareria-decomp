using UnityEngine;

namespace SPR.Field;

public class ElevatorBackground : MonoBehaviour
{
	private float height;

	private float speed;

	private AreaBox areaBox;

	private Vector2 boxPos;

	private Vector3 initialPos;

	private Camera mainCamera;

	private bool chaseCamera;

	private void Awake()
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		height = component.size.y;
		mainCamera = Camera.main;
		initialPos = base.transform.position;
	}

	public void Move(float speed, AreaBox areaBox, Vector2 boxPos, bool chaseCamera)
	{
		this.speed = speed;
		this.areaBox = areaBox;
		this.boxPos = boxPos;
		this.chaseCamera = chaseCamera;
	}

	public void ChangeSpeed(float speed)
	{
		this.speed = speed;
	}

	public void Stop()
	{
		speed = 0f;
		base.transform.position = initialPos;
	}

	public void StopChasingCamera()
	{
		chaseCamera = false;
	}

	private void FixedUpdate()
	{
		if (speed != 0f)
		{
			Vector3 position = base.transform.position;
			position.y += speed * Time.fixedDeltaTime;
			float num = (chaseCamera ? mainCamera.transform.position.y : boxPos.y);
			if (position.y - height / 2f >= (float)areaBox.UpperRight.y + num)
			{
				position.y -= height * 2f;
			}
			base.transform.position = position;
		}
	}
}
