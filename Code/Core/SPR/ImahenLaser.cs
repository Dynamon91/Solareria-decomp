using System.Collections.Generic;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class ImahenLaser : PoolingParticle, IAttackSource
{
	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageList = new List<IDamageable>();

	public string Name => "Imahen_Turret2";

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public override void InitializeBeforeUse()
	{
		damageList.Clear();
		base.InitializeBeforeUse();
	}

	public void Attack()
	{
		Vector3 vector = base.transform.position + base.transform.rotation * Box2D.offset;
		AttackUtility.AttackBox(size: base.transform.localScale * Box2D.size, center: vector, angle: base.transform.rotation.eulerAngles.z, hitInfo: new HitInfo(this), attackLayer: LayerUtility.PlayerLayer, attackedList: damageList);
	}
}
