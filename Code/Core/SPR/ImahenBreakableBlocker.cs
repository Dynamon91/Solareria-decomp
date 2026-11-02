using System;
using Spine;
using UnityEngine;

namespace SPR;

public class ImahenBreakableBlocker : ImahenBlockerBase, IDamageable
{
	private ClampedInt health;

	private BoxCollider2D box2D;

	public override string Name => "";

	public HitType HitType => HitType.Object;

	public override Vector2 VisualCenter => base.transform.position;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private void Awake()
	{
		SetAnimation("idle");
		AddAnimationEvent(AnimState.Complete, delegate(TrackEntry track)
		{
			if (track.Animation.Name == "hit")
			{
				SetAnimation("idle");
			}
		});
	}

	public override void OnTriggered()
	{
		SetAnimation("break", loop: false);
		Box2D.enabled = false;
		ChangeState(1);
	}

	public override void LoadState(int state)
	{
		if (Game.Instance.CurrentFieldID.ToString() == "SolinLab_90")
		{
			state = 0;
		}
		switch ((State)state)
		{
		case State.Closed:
		{
			SetAnimation("idle");
			int hP = Game.DataManager.GetEnemyStatData("ImahenBreakableBlocker").HP;
			health = new ClampedInt(hP, 0, hP);
			break;
		}
		case State.Opened:
		{
			TrackEntry trackEntry = base.SkeletonAnimation.AnimationState.SetAnimation(0, "break", loop: false);
			trackEntry.AnimationStart = trackEntry.AnimationEnd;
			health = new ClampedInt(0, 0, 0);
			Box2D.enabled = false;
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if ((float)health.Value <= 0f)
		{
			return false;
		}
		health -= hitInfo.Damage;
		SetAnimationInstantly("hit", loop: false);
		EnableVFX("hit");
		if (health.Value <= 0)
		{
			OnTriggered();
		}
		return true;
	}
}
