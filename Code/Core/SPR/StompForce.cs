using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class StompForce
{
	private readonly Timer timer;

	private readonly float force;

	private readonly Vector3 forceDirection;

	public StompForce(float duration, float force, Vector3 forceDirection)
	{
		timer = new Timer(duration);
		this.force = force;
		this.forceDirection = forceDirection;
	}

	public async UniTaskVoid ApplyForce(GameObject obj)
	{
		if (!obj.TryGetComponent<Rigidbody2D>(out var rb2D))
		{
			await UniTask.Yield();
		}
		while (!timer.IsDone)
		{
			if (obj.TryGetComponent<PlayerMover>(out var component))
			{
				if (component.CanHit)
				{
					rb2D.AddForce(forceDirection * force, ForceMode2D.Force);
				}
				timer.Tick(Time.fixedDeltaTime);
			}
			await UniTask.Yield();
		}
	}

	public static void ApplyStompForce(GameObject obj, int frames, float force, Vector3 direction)
	{
		new StompForce(Util.FrameToSeconds(frames), force, direction).ApplyForce(obj).Forget();
	}
}
