using System;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class FireArbalist : Creature
{
	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<FireArbalist> shot;

	[SerializeField]
	private GenericAttackSequence<FireArbalist> enhancedShot;

	[SerializeField]
	private GenericAttackSequence<FireArbalist> explosiveShot;

	[SerializeField]
	private GenericAttackSequence<FireArbalist> melee;

	[Header("Sfx")]
	[SerializeField]
	private EventReference delayEvent;

	[SerializeField]
	private EventReference explodeEvent;

	[SerializeField]
	private Transform backflipPosition;

	private Vector2 targetDirection;

	public override string Name => "FireArbalist";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(shot, () => new List<AttackSequence> { shot });
		AddAttack(enhancedShot, () => new List<AttackSequence> { enhancedShot });
		AddAttack(explosiveShot, () => new List<AttackSequence> { explosiveShot });
		AddAttack(melee, () => new List<AttackSequence> { melee });
		AddAction(ActionTiming.Begin, "NormalShot", delegate(Parameter[] p)
		{
			targetDirection = FacingDirection.ToVector2();
			Shoot(FireArbalistBolt.BoltType.Normal, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "EnhancedShot", delegate(Parameter[] p)
		{
			targetDirection = FacingDirection.ToVector2();
			Shoot(FireArbalistBolt.BoltType.Enhanced, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "ExplosiveShot", delegate(Parameter[] p)
		{
			Shoot(FireArbalistBolt.BoltType.Explosive, p[0].Float, p[1].Int, p[2].Float);
		});
		AddAction(ActionTiming.Begin, "MeleeShot", delegate(Parameter[] p)
		{
			targetDirection = FacingDirection.ToVector2();
			Shoot(FireArbalistBolt.BoltType.Normal, p[0].Float);
		});
		AddAction(ActionTiming.OnFrame, "Aim", Aim);
		AddAction(ActionTiming.Begin, "Backflip", Backflip);
		AddAction(ActionTiming.Begin, "Landing", Landing);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { shot, enhancedShot, explosiveShot, melee };
	}

	private void Shoot(FireArbalistBolt.BoltType type, float speed, int explosiveTime = 0, float knockBack = 0f)
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("fire_bolt");
		FireArbalistBolt bolt;
		switch (type)
		{
		case FireArbalistBolt.BoltType.Normal:
			bolt = Enemy.PoolManager.GetObject<FireArbalistBolt>("Fire Arbalist Bolt_Normal.prefab", boneWorldPosition);
			bolt.Launch(targetDirection, speed);
			break;
		case FireArbalistBolt.BoltType.Enhanced:
			bolt = Enemy.PoolManager.GetObject<FireArbalistBolt>("Fire Arbalist Bolt_Enhanced.prefab", boneWorldPosition);
			bolt.Launch(targetDirection, speed);
			break;
		case FireArbalistBolt.BoltType.Explosive:
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(GetBoneWorldPosition("fire_bolt"), targetDirection, float.PositiveInfinity, LayerUtility.PlatformLayer);
			bolt = Enemy.PoolManager.GetObject<FireArbalistBolt>("Fire Arbalist Bolt_Explosive.prefab", boneWorldPosition);
			bolt.Rotate(targetDirection);
			bolt.Launch(targetDirection, 0f);
			bolt.SetExplodeEvent(explodeEvent);
			bolt.transform.DOMove(raycastHit2D.point, speed).SetSpeedBased(isSpeedBased: true).SetUpdate(UpdateType.Fixed)
				.SetEase(Ease.Linear)
				.OnUpdate(delegate
				{
					ExplosiveBoltOverlap(bolt, speed);
				})
				.OnComplete(delegate
				{
					bolt.SetExplosiveTimer(explosiveTime, delayEvent);
				});
			base.transform.DOMoveX(base.Position.x + (float)(-FacingDirection.GetSignX()) * knockBack, 0.25f).SetEase(Ease.Linear);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	private void Aim()
	{
		Bone bone = base.SkeletonAnimation.Skeleton.FindBone("player_follower");
		Bone bone2 = base.SkeletonAnimation.Skeleton.FindBone("fire_bolt");
		Vector3 vector;
		if (base.StateMachine.CurrentState is AttackState attackState && attackState.CurrentSequence == explosiveShot)
		{
			vector = Enemy.Player.GetBoneWorldPosition("Neck");
			if (vector.y >= VisualBottom.y - HitBox.size.y / 2f)
			{
				vector.y = VisualBottom.y - HitBox.size.y / 2f;
			}
		}
		else
		{
			vector = Enemy.Player.VisualCenter;
		}
		Vector3 worldPosition = bone2.GetWorldPosition(base.transform);
		Vector3 vector2 = vector - worldPosition;
		if (vector2.x * (float)FacingDirection.GetSignX() < 0f)
		{
			vector2.x = 0f;
		}
		targetDirection = vector2.normalized;
		Vector2 vector3 = targetDirection * 5f;
		vector3 = new Vector2(Mathf.Abs(vector3.x), vector3.y);
		Vector3 vector4 = bone2.GetWorldPosition(base.transform) - base.transform.position;
		bone.SetLocalPosition(vector4 + (Vector3)vector3);
	}

	private void Backflip(int duration)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2(base.transform.position.x, backflipPosition.position.y), distance: Mathf.Abs(backflipPosition.localPosition.x), direction: FacingDirection.Opposite().ToVector2(), layerMask: LayerUtility.PlatformLayer);
		Vector2 vector = (raycastHit2D ? (raycastHit2D.point + new Vector2((float)FacingDirection.GetSignX() * Width, 0f)) : ((Vector2)backflipPosition.position));
		base.transform.DOMove(vector, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void Landing(int duration)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualBottom, Vector2.down, float.PositiveInfinity, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		base.transform.DOMove(raycastHit2D.point, Util.FrameToSeconds(duration)).SetEase(Ease.InSine);
	}

	private void ExplosiveBoltOverlap(FireArbalistBolt bolt, float speed)
	{
		bolt.HandleOverlap(bolt.transform.position, bolt.Box2D.size * ((speed <= 60f) ? 1f : (speed * Time.fixedDeltaTime)));
	}
}
