using System.Collections.Generic;
using Library;
using SPR.Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class SeedArcher : Creature
{
	[Header("화살")]
	[SerializeField]
	private float arrowSpeed;

	[Header("Attack")]
	[SerializeField]
	private GenericAttackSequence<SeedArcher> attack;

	private Vector3 targetDirection;

	public override string Name => "SeedArcher";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(attack, () => new List<AttackSequence> { attack });
		AddAction(ActionTiming.OnFrame, "AimPlayer", AimPlayer);
		AddAction(ActionTiming.Begin, "ShootArrow", ShootArrow);
	}

	public override bool ShouldChangeToAttackState()
	{
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		if (Physics2D.Raycast(VisualCenter, normalized, float.PositiveInfinity, (int)LayerUtility.PlayerLayer | (int)LayerUtility.PlatformLayer).transform.TryGetComponent<PlayerMover>(out var _))
		{
			return base.ShouldChangeToAttackState();
		}
		return false;
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	private void AimPlayer()
	{
		Bone bone = base.SkeletonAnimation.Skeleton.FindBone("target");
		Bone bone2 = base.SkeletonAnimation.Skeleton.FindBone("mouth");
		Vector3 vector = Enemy.Player.VisualCenter;
		Vector3 worldPosition = bone2.GetWorldPosition(base.transform);
		Vector3 vector2 = vector - worldPosition;
		if (vector2.x * (float)FacingDirection.GetSignX() < 0f)
		{
			vector2 = FacingDirection.ToVector2();
		}
		Vector3 vector3 = (targetDirection = vector2.normalized * 5f);
		vector3 = new Vector3(Mathf.Abs(vector3.x), vector3.y);
		Vector3 vector4 = bone2.GetWorldPosition(base.transform) - base.transform.position;
		bone.SetLocalPosition(vector4 + vector3);
	}

	private void ShootArrow()
	{
		Vector3 worldPosition = base.SkeletonAnimation.Skeleton.FindBone("mouth").GetWorldPosition(base.transform);
		Game.Instance.ObjectPoolManager.GetObject<SeedArcherArrow>("Seed Archer Arrow.prefab", worldPosition).Shoot(targetDirection, arrowSpeed);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector3 worldPosition = base.SkeletonAnimation.Skeleton.FindBone("target").GetWorldPosition(base.transform);
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(worldPosition, 0.5f);
		Vector3 vector = base.SkeletonAnimation.Skeleton.FindBone("mouth").GetWorldPosition(base.transform) - base.transform.position;
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(base.transform.position + vector + targetDirection, 0.5f);
	}
}
