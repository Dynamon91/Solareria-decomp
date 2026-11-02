using FMODUnity;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class FastDownJumper : MonoBehaviour, IDamageable
{
	[Header("References")]
	[SerializeField]
	private ParticleSystem successParticle;

	[SerializeField]
	private ParticleSystem failParticle;

	[Header("SFX")]
	[SerializeField]
	private EventReference jumpEvent;

	[SerializeField]
	private EventReference hitEvent;

	private Collider2D col;

	private SkeletonAnimation sa;

	private bool isInteracting;

	protected virtual int JumpSpeed => PlayerControlParameter.Instance.GroggyJumpSpeed;

	private Collider2D Collider => col ?? (col = GetComponent<Collider2D>());

	private SkeletonAnimation SkeletonAnimation => sa ?? (sa = GetComponentInChildren<SkeletonAnimation>());

	private static PlayerController Player => Game.Instance.Player;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => SkeletonAnimation.transform.position;

	private void Awake()
	{
		if (!SkeletonAnimation.IsNull())
		{
			SkeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
		}
	}

	private void FixedUpdate()
	{
		if (Collider.bounds.Intersects(Player.mover.VisualBounds))
		{
			if (isInteracting)
			{
				return;
			}
			PlayerStateMachine pSM = Player.mover.PSM;
			if (pSM.CurrentState == pSM.FastDownState)
			{
				TakeHighJump();
				return;
			}
			if (pSM.CurrentState == pSM.FallState)
			{
				isInteracting = true;
				if (!SkeletonAnimation.IsNull())
				{
					SkeletonAnimation.AnimationState.SetAnimation(0, "hit", loop: false);
					SkeletonAnimation.AnimationState.AddAnimation(0, "idle", loop: true, 0f);
				}
				if (!failParticle.IsNull())
				{
					failParticle.Play();
				}
			}
		}
		isInteracting = false;
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (hitInfo.AttackerType != HitType.Player)
		{
			return false;
		}
		if (!SkeletonAnimation.IsNull())
		{
			string animationName = ((hitInfo.GetDirection(base.transform.position).GetSignX() < 0) ? "hit_right" : "hit_left");
			SkeletonAnimation.AnimationState.SetAnimation(0, animationName, loop: false);
			SkeletonAnimation.AnimationState.AddAnimation(0, "idle", loop: true, 0f);
			SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
		return true;
	}

	private void TakeHighJump()
	{
		isInteracting = true;
		Vector3 position = Player.transform.position;
		position.y = Collider.bounds.max.y;
		Player.transform.position = position;
		Player.mover.PSM.GroundEvent();
		Player.mover.TakeHighJump(JumpSpeed);
		if (!SkeletonAnimation.IsNull())
		{
			SkeletonAnimation.AnimationState.AddAnimation(0, "idle", loop: true, 0f);
			SkeletonAnimation.AnimationState.SetAnimation(0, "jump", loop: false);
		}
		if (!successParticle.IsNull())
		{
			successParticle.Play();
		}
		SoundManager.PlayOneShotAttached(jumpEvent, base.transform);
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}
}
