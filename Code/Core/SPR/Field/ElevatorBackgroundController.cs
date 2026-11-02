using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Field;

public class ElevatorBackgroundController : MonoBehaviour
{
	[SerializeField]
	private List<ElevatorBackground> backgrounds;

	private float initialSpeed;

	private bool isChasingCamera;

	private bool brake;

	public void Move(float speed, AreaBox areaBox, Vector2 boxPos, bool chaseCamera = true)
	{
		isChasingCamera = chaseCamera;
		backgrounds.ForEach(delegate(ElevatorBackground bg)
		{
			bg.Move(speed, areaBox, boxPos, isChasingCamera);
		});
		initialSpeed = speed;
	}

	public async UniTask ChangeSpeed(float speed, float duration, float stopChasingTime)
	{
		float timer;
		for (timer = 0f; timer <= duration; timer += Time.deltaTime)
		{
			if (brake)
			{
				return;
			}
			backgrounds.ForEach(delegate(ElevatorBackground bg)
			{
				bg.ChangeSpeed(Mathf.Lerp(initialSpeed, speed, timer / duration));
			});
			if (isChasingCamera && timer >= stopChasingTime)
			{
				isChasingCamera = false;
				backgrounds.ForEach(delegate(ElevatorBackground bg)
				{
					bg.StopChasingCamera();
				});
			}
			await UniTask.Yield();
		}
		backgrounds.ForEach(delegate(ElevatorBackground bg)
		{
			bg.ChangeSpeed(speed);
		});
	}

	public void Stop()
	{
		backgrounds.ForEach(delegate(ElevatorBackground bg)
		{
			bg.Stop();
		});
		brake = true;
	}
}
