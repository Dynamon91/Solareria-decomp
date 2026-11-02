using System.Collections.Generic;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class InfectedMucoraGuardianLaser : PoolingParticle, IAttackSource
{
	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageList = new List<IDamageable>();

	public string Name => "InfectedMucoraGuardian_Lightning";

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		damageList.Clear();
	}

	public void HandleOverlap()
	{
		Vector3 vector = base.transform.position + base.transform.rotation * Box2D.offset;
		AttackUtility.AttackBox(size: Box2D.size, center: vector, angle: base.transform.rotation.eulerAngles.z, hitInfo: new HitInfo(this), attackLayer: LayerUtility.PlayerLayer, attackedList: damageList);
	}
}
