using System.Collections.Generic;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class MoldinSaw : MoldinWeapon
{
	public const string Address = "Moldin_Saw.prefab";

	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageList = new List<IDamageable>();

	public override string Name => "MoldinSaw";

	public override string PrefabKey => "Moldin_Saw.prefab";

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public override void InitializeBeforeUse()
	{
		damageList.Clear();
		base.InitializeBeforeUse();
	}

	public void HandleOverlap(string phase)
	{
		AttackUtility.AttackBox((Vector2)base.transform.position + Box2D.offset, Box2D.size, new HitInfo(this, phase), LayerUtility.PlayerLayer, damageList);
	}
}
