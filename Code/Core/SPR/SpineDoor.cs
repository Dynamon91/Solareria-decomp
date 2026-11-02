using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class SpineDoor : Door
{
	[SerializeField]
	private ParticleSystem particle;

	[Header("Animation")]
	[SerializeField]
	private string openAnimName = "open";

	[SerializeField]
	private string openIdleName = "open_idle";

	[SerializeField]
	private string closeAnimName = "close";

	[SerializeField]
	private string closeIdleName = "close_idle";

	[SerializeField]
	private float animationSpeed = 1f;

	private SkeletonAnimation sa;

	public bool IsOpened { get; private set; } = true;

	private SkeletonAnimation SkeletonAnimation => sa ?? (sa = GetComponentInChildren<SkeletonAnimation>());

	public float CloseAnimDuration => SkeletonAnimation.Skeleton.Data.FindAnimation(closeAnimName).Duration / animationSpeed;

	public void OpenLoop()
	{
		SkeletonAnimation.AnimationState.SetAnimation(0, openIdleName, loop: true);
	}

	protected override void PlayAnimation(bool open)
	{
		if (!open && !particle.IsNull())
		{
			particle.Play();
		}
		SkeletonAnimation.AnimationState.SetAnimation(0, open ? openAnimName : closeAnimName, loop: false).TimeScale = animationSpeed;
		SkeletonAnimation.AnimationState.AddAnimation(0, open ? openIdleName : closeIdleName, loop: true, 0f);
	}

	protected override void OnAwake()
	{
		SkeletonAnimation.AnimationState.SetAnimation(0, base.OpenOnAwake ? openIdleName : closeIdleName, loop: true);
	}
}
