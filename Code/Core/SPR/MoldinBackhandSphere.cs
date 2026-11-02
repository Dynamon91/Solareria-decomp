using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class MoldinBackhandSphere : MoldinWeapon
{
	public const string Address = "Moldin_BackhandSphere.prefab";

	[SerializeField]
	private ParticleSystem explosionParticle;

	[SerializeField]
	private Zone explosionZone;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private CircleCollider2D circle2D;

	public override string PrefabKey => "Moldin_BackhandSphere.prefab";

	public override string Name => "MoldinBackhandSphere";

	public override Direction FacingDirection => Direction.None;

	private CircleCollider2D Circle2D => circle2D ?? (circle2D = GetComponent<CircleCollider2D>());

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Vector2 vector = (Vector2)base.transform.position + explosionZone.Center;
		Gizmos.DrawWireCube(size: explosionZone.Size, center: vector);
	}

	public override void InitializeBeforeUse()
	{
		base.SkeletonRenderer.gameObject.SetActive(value: true);
		base.gameObject.SetActive(value: true);
		damageableList.Clear();
		ClearAnimation();
		SetAnimation("slash_backhand_sphere", loop: false);
		ApplyFade(appear: true, 0f);
	}

	public void Drop(float destY, float speed, bool isSecondPhase, int explosionWaitDuration)
	{
		SetAnimation("slash_backhand_sphere_loop");
		destY += Circle2D.radius;
		base.transform.DOMoveY(destY, speed).SetSpeedBased(isSpeedBased: true).SetEase(Ease.Linear)
			.OnUpdate(delegate
			{
				HandleOverlap(isSecondPhase);
			})
			.OnComplete(delegate
			{
				if (isSecondPhase)
				{
					Explosion(explosionWaitDuration).Forget();
				}
				else
				{
					ApplyFade(appear: false, 1f, "Phase1");
				}
			});
	}

	private void HandleOverlap(bool isSecondPhase)
	{
		if (!isSecondPhase)
		{
			AttackUtility.AttackCircle(base.transform.position, Circle2D.radius, new HitInfo(this), LayerUtility.PlayerLayer, damageableList);
		}
	}

	private async UniTaskVoid Explosion(int explosionWaitDuration)
	{
		DOTween.Kill(base.transform);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(explosionWaitDuration);
		base.SkeletonRenderer.gameObject.SetActive(value: false);
		explosionParticle.Play();
		Vector2 center = (Vector2)base.transform.position + explosionZone.Center;
		Vector2 size = explosionZone.Size;
		AttackUtility.AttackBox(center, size, new HitInfo(this, "Explosion"), LayerUtility.PlayerLayer);
		await UniTask.WaitUntil(() => base.VfxController.IsTimelineEnd("Phase2"));
		await UniTask.WaitWhile(() => explosionParticle.isPlaying);
		ReturnToPool();
	}
}
