using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class PrimordialFlameSword : SpineObject, IPoolableObject
{
	[SerializeField]
	private Zone dropAttackZone;

	[SerializeField]
	private SpriteRenderer effect;

	private Color originColor;

	private static PlayerMover Player => Game.Instance.Player.mover;

	public string PrefabKey => "Primordial Flame_Sword.prefab";

	public GameObject GameObject => base.gameObject;

	public override string Name => "PrimordialFlameSword";

	public override Direction FacingDirection
	{
		get
		{
			if (!((Player.VisualBottom - (Vector2)base.transform.position).normalized.x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	public void InitializeBeforeUse()
	{
		base.SkeletonRenderer.skeleton.SetAlpha(1f);
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Drop()
	{
		Vector2 center = (Vector2)base.transform.position + dropAttackZone.Center;
		Vector2 size = dropAttackZone.Size;
		AttackUtility.AttackBox(center, size, new HitInfo(this, string.Empty), LayerUtility.EnemyDefaultAttackLayer, new List<IDamageable>());
	}

	public async UniTaskVoid ReturnSword(string vfxName)
	{
		base.SkeletonRenderer.skeleton.SetAlpha(0f);
		await UniTask.WaitUntil(() => base.VfxController.IsTimelineEnd(vfxName));
		ReturnToPool();
	}

	public void ImpactEffect()
	{
		EnableVFX("impact");
	}

	protected override void OnDrawGizmosSelected()
	{
		Vector2 vector = (Vector2)base.transform.position + dropAttackZone.Center;
		Vector2 size = dropAttackZone.Size;
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(vector, size);
	}
}
