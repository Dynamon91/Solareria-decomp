using Spine.Unity;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class SpineSkinBehaviour : PlayableBehaviour
{
	public string SkinName;

	private SkeletonAnimation spine;

	private bool isInitialized;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		spine = playerData as SkeletonAnimation;
		if (spine == null)
		{
			return;
		}
		if (!isInitialized)
		{
			if (spine.Skeleton.Skin == null)
			{
				spine.Skeleton.SetSkin(SkinName);
			}
			isInitialized = true;
		}
		if (spine.Skeleton.Skin.Name != SkinName)
		{
			spine.Skeleton.SetSkin(SkinName);
			spine.Skeleton.SetSlotsToSetupPose();
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		_ = spine == null;
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		_ = spine == null;
	}
}
