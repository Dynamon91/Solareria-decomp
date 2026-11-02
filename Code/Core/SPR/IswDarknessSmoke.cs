using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class IswDarknessSmoke : PoolingObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem particle;

	private BoxCollider2D box2D;

	public string Name => "InfectedSpiritWarrior_Darkness_Projectile_Smoke";

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public async UniTaskVoid HandleOverlap(int duration, int cycle)
	{
		particle.Play();
		for (int frame = 0; frame < duration; frame++)
		{
			if (frame % cycle == 0)
			{
				AttackUtility.AttackBox(base.transform.position, Box2D.size, new HitInfo(this), LayerUtility.PlayerLayer);
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		await UniTask.WaitWhile(() => particle.isPlaying);
		ReturnToPool();
	}
}
