using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using SPR.Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class EarthWarriorWeapon : PoolingObject, IAttackSource
{
	[SerializeField]
	private ParticleSystem rotateParticle;

	[SerializeField]
	private ParticleSystem stuckParticle;

	[Header("SFX")]
	[SerializeField]
	private EventReference rotateEvent;

	[SerializeField]
	private EventReference stuckEvent;

	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageList = new List<IDamageable>();

	private MeshRenderer meshRenderer;

	private SkeletonAnimation skeletonAnimation;

	private EventInstance rotateInstance;

	public string Name => "EarthWarrior_Boomerang";

	public Direction FacingDirection { get; set; }

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = GetComponentInChildren<MeshRenderer>());

	private SkeletonAnimation SkeletonAnimation => skeletonAnimation ?? (skeletonAnimation = GetComponentInChildren<SkeletonAnimation>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public int SortingOrder
	{
		get
		{
			return MeshRenderer.sortingOrder;
		}
		set
		{
			MeshRenderer.sortingOrder = value;
		}
	}

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		rotateParticle.Play(withChildren: true);
		rotateInstance = SoundManager.PlayOneShotAttached(rotateEvent, base.transform);
		SkeletonAnimation.AnimationState.SetAnimation(0, "weapon_throw_rotation", loop: false);
		damageList.Clear();
	}

	public override void ReturnToPool()
	{
		DOTween.Kill(base.transform);
		base.ReturnToPool();
	}

	public void HandleOverlap()
	{
		if (base.gameObject.activeSelf)
		{
			AttackUtility.AttackBox(base.transform.position, Box2D.size, new HitInfo(this), LayerUtility.PlayerLayer, damageList);
		}
	}

	public RaycastHit2D CheckPlatform(Vector2 direction)
	{
		return Physics2D.Raycast(base.transform.position, direction, Box2D.size.x / 4f, LayerUtility.PlatformLayer);
	}

	public void ResetDamageList()
	{
		damageList.Clear();
	}

	public void StopRotate(Vector3 wallPosition)
	{
		rotateParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		Vector3 localPosition = base.transform.InverseTransformPoint(wallPosition);
		stuckParticle.transform.localPosition = localPosition;
		stuckParticle.Play(withChildren: true);
		if (rotateInstance.isValid())
		{
			rotateInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			rotateInstance.release();
		}
		SoundManager.PlayOneShotAttached(stuckEvent, base.transform);
		SkeletonAnimation.AnimationState.SetAnimation(0, "weapon_throw_stuck", loop: false);
	}
}
