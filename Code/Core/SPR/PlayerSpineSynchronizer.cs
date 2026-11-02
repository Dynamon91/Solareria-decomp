using Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class PlayerSpineSynchronizer : MonoBehaviour
{
	private SkeletonAnimation skeletonAnimation;

	private void Awake()
	{
		skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
		Game.Instance.Player.mover.AddAnimationEvent(SpineObject.AnimState.Start, delegate(TrackEntry trackEntry)
		{
			skeletonAnimation.AnimationState.SetAnimation(0, trackEntry.Animation, trackEntry.Loop);
		});
	}

	private void OnEnable()
	{
		skeletonAnimation.SynchronizeAnimation(Game.Instance.Player.mover);
	}
}
