using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class IswElementField : PoolingObject, IAttackSource
{
	[SerializeField]
	private string attackKey;

	[Header("공격 설정")]
	[SerializeField]
	private int duration;

	[SerializeField]
	private int cycleFrame;

	[Header("Particle")]
	[SerializeField]
	private ParticleSystem[] particles;

	private BoxCollider2D box2D;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string Name => attackKey;

	public Direction FacingDirection => Direction.None;

	public async UniTaskVoid EnableAttackCycle()
	{
		if (particles.Any((ParticleSystem p) => p.IsAlive(withChildren: true)))
		{
			return;
		}
		ParticleSystem[] array = particles;
		for (int num = 0; num < array.Length; num++)
		{
			array[num].Play();
		}
		for (int frame = 0; frame < duration; frame++)
		{
			if (frame % cycleFrame == 0)
			{
				Vector2 center = Box2D.offset + (Vector2)base.transform.position;
				Vector2 size = Box2D.size * base.transform.localScale;
				AttackUtility.AttackBox(center, size, new HitInfo(this), LayerUtility.PlayerLayer);
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		array = particles;
		for (int num = 0; num < array.Length; num++)
		{
			array[num].Stop();
		}
		await UniTask.WaitWhile(() => particles.All((ParticleSystem p) => p.IsAlive(withChildren: true)));
		ReturnToPool();
	}

	public void SetScaleX(float value)
	{
		Box2D.size = new Vector2(value, 1f);
		ParticleSystem[] array = particles;
		foreach (ParticleSystem obj in array)
		{
			ParticleSystem.EmissionModule emission = obj.emission;
			if (emission.burstCount > 0)
			{
				ParticleSystem.Burst burst = emission.GetBurst(0);
				ParticleSystem.MinMaxCurve count = burst.count;
				if (attackKey.Contains("Dark"))
				{
					count.constant = value;
				}
				else
				{
					count.constant = value / 2f;
				}
				burst.count = count;
				emission.SetBurst(0, burst);
			}
			ParticleSystem.ShapeModule shape = obj.shape;
			shape.radius = value / 2f;
		}
	}
}
