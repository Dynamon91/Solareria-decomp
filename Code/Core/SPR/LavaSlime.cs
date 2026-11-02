using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class LavaSlime : Creature
{
	[Header("폭발")]
	[SerializeField]
	private Zone explosionZone;

	[Header("지속 공격")]
	[SerializeField]
	private int preExplosionTime;

	[SerializeField]
	private int particlePreDelay;

	[Header("Reference")]
	[SerializeField]
	private ParticleSystem explosion;

	[SerializeField]
	private GenericAttackSequence<LavaSlime> chase;

	public override string Name => "LavaSlime";

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { chase };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return new List<AttackSequence> { chase };
	}

	protected override void OnDie()
	{
		base.OnDie();
		Explosion().Forget();
	}

	protected override void CorpseRotate()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualCenter, Vector2.down, float.PositiveInfinity, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		if (!(Vector2.up == raycastHit2D.normal))
		{
			float num = Vector2.Angle(Vector2.up, raycastHit2D.normal);
			num = ((raycastHit2D.normal.x.GetSign() == -1) ? num : (0f - num));
			base.transform.rotation = Quaternion.Euler(0f, 0f, num);
		}
	}

	private async UniTaskVoid Explosion()
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(preExplosionTime - particlePreDelay);
		explosion.Play();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(particlePreDelay);
		Zone zoneOf = explosionZone.GetZoneOf(this);
		Collider2D[] array = AttackUtility.OverlapBoxAll(zoneOf.Center, zoneOf.Size, 0f, (int)LayerUtility.PlayerLayer | (int)LayerUtility.MonsterLayer);
		foreach (Collider2D obj in array)
		{
			if (obj.TryGetComponent<PlayerMover>(out var _))
			{
				Enemy.Player.TryTakeDamage(new HitInfo(this, "Explosion"));
			}
			if (obj.TryGetComponent<Enemy>(out var component2))
			{
				component2.TryTakeDamage(new HitInfo(this, "Explosion_Creature"));
			}
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = explosionZone.GetZoneOf(this);
		Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
		Gizmos.DrawCube(zoneOf.Center, zoneOf.Size);
	}
}
