using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class BreakableBarrel : BreakableObject, IAttackSource
{
	[SerializeField]
	private float waitTime;

	[SerializeField]
	private float attackRange;

	[SerializeField]
	private LayerMask attackLayer;

	[SerializeField]
	private ParticleSystem chargeParticle;

	[SerializeField]
	private ParticleSystem explosionParticle;

	public string Name => "Barrel";

	public Direction FacingDirection => Direction.None;

	public override HitType HitType => HitType.Player | HitType.Object;

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		WaitAndBreakAsync().Forget();
		return true;
	}

	private async UniTask WaitAndBreakAsync()
	{
		base.SkeletonAnimation.SetAnimation("active", 0, loop: false);
		base.Collider2D.enabled = false;
		chargeParticle.Play();
		BreakableObject.SoundManager.PlayOneShotAttached("event:/SFX/Object/SFX_Object_Bomb_Ignite", base.transform);
		await UniTaskExtension.WaitForSeconds(waitTime, EnableToken);
		if (!base.gameObject.IsNull())
		{
			AttackUtility.AttackCircle(hitInfo: new HitInfo(this, "Creature"), center: base.VisualCenter, radius: attackRange, attackLayer: attackLayer);
			AttackUtility.AttackCircle(hitInfo: new HitInfo(this, "Player"), center: base.VisualCenter, radius: attackRange, attackLayer: attackLayer);
			chargeParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			BreakableObject.SoundManager.PlayOneShotAttached("event:/SFX/Object/SFX_Object_Bomb_Explosion", base.transform);
			await explosionParticle.PlayAndWaitAsync(EnableToken);
			base.gameObject.SetActive(value: false);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.VisualCenter, attackRange);
	}
}
