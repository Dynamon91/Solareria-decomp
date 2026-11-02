using FMODUnity;
using SPR.Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class SpineMovingEnvironment : SpineEnvironment, IDamageable
{
	[SerializeField]
	private string idleAnimName = "idle";

	[SerializeField]
	private string moveAnimName;

	[SerializeField]
	private string hitAnimName = "hit";

	[SerializeField]
	private EventReference hitEvent;

	private bool hasIdleAnim;

	private bool hasMoveAnim;

	private bool hasHitAnim;

	private int frameCount;

	private const int AnimDelayFrame = 20;

	private SkeletonAnimation sa;

	private Collider2D coll;

	private SkeletonAnimation SkeletonAnimation => sa ?? (sa = GetComponentInChildren<SkeletonAnimation>());

	private Collider2D Coll => coll ?? (coll = GetComponent<Collider2D>());

	public HitType HitType => HitType.Object | HitType.Environment;

	public Vector2 VisualCenter => base.transform.position;

	private void Start()
	{
		hasIdleAnim = !string.IsNullOrWhiteSpace(idleAnimName);
		hasMoveAnim = !string.IsNullOrWhiteSpace(moveAnimName);
		hasHitAnim = !string.IsNullOrWhiteSpace(hitAnimName);
		if (hasIdleAnim)
		{
			SkeletonAnimation.AnimationState.SetAnimation(0, idleAnimName, loop: true);
		}
	}

	private void FixedUpdate()
	{
		if (frameCount > 0)
		{
			frameCount--;
		}
	}

	private void OnTriggerEnter2D(Collider2D coll)
	{
		if (hasMoveAnim && frameCount <= 0 && (int)LayerUtility.PlayerLayer == ((1 << coll.gameObject.layer) | (int)LayerUtility.PlayerLayer) && coll.TryGetComponent<PlayerMover>(out var _))
		{
			frameCount = 20;
			SkeletonAnimation.AnimationState.SetAnimation(0, moveAnimName, loop: false);
			SkeletonAnimation.AnimationState.AddAnimation(0, idleAnimName, loop: false, 0f);
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!hasHitAnim)
		{
			return false;
		}
		if (frameCount > 0)
		{
			return false;
		}
		frameCount = 20;
		SkeletonAnimation.AnimationState.SetAnimation(0, hitAnimName, loop: false);
		SkeletonAnimation.AnimationState.AddAnimation(0, idleAnimName, loop: false, 0f);
		if (!hitEvent.IsNull)
		{
			Game.Instance.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
		return true;
	}
}
