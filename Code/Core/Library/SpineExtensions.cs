using SPR;
using Spine;
using Spine.Unity;

namespace Library;

public static class SpineExtensions
{
	public static TrackEntry SetAnimation(this SkeletonAnimation spine, string animationName, int trackIndex = 0, bool loop = true, float startTime = 0f, float timeScale = 1f, bool isImmediate = true, float mixDuration = 0.2f, float deltaTime = 0f)
	{
		TrackEntry trackEntry = spine.AnimationState.SetAnimation(trackIndex, animationName ?? "", loop);
		trackEntry.TimeScale = timeScale;
		trackEntry.TrackTime = startTime;
		trackEntry.MixDuration = (isImmediate ? 0f : mixDuration);
		spine.Update(deltaTime);
		return trackEntry;
	}

	public static void AddAnimation(this SkeletonAnimation spine, string animationName, int trackIndex = 0, bool loop = true, float startTime = 0f, float timeScale = 1f, bool isImmediate = true, float mixDuration = 0.2f, float deltaTime = 0f, float delay = 0f)
	{
		TrackEntry trackEntry = spine.AnimationState.AddAnimation(trackIndex, animationName ?? "", loop, delay);
		trackEntry.TimeScale = timeScale;
		trackEntry.TrackTime = startTime;
		trackEntry.MixDuration = (isImmediate ? 0f : mixDuration);
		spine.Update(deltaTime);
	}

	public static void SetAnimationInstantly(this SkeletonAnimation spine, string animationName, bool loop = true, float startTime = 0f, float timeScale = 1f, float deltaTime = 0f)
	{
		TrackEntry trackEntry = spine.AnimationState.SetAnimation(0, animationName ?? "", loop);
		trackEntry.TimeScale = timeScale;
		trackEntry.TrackTime = startTime;
		trackEntry.MixDuration = 0f;
		spine.Update(deltaTime);
	}

	public static void SynchronizeAnimation(this SkeletonAnimation toSpine, SkeletonAnimation fromSpine, bool isImmediate = true, string animationName = null, int trackIndex = 0)
	{
		TrackEntry trackEntry = fromSpine.AnimationState.GetCurrent(trackIndex);
		if (trackEntry == null)
		{
			DebugLog.GeneralLogError($"{fromSpine.transform.parent.gameObject.name}의 {trackIndex}번 트랙에 현재 애니메이션이 존재하지 않습니다! 일단 idle로 적용합니다.");
			SetIdle();
			return;
		}
		if (animationName == null)
		{
			animationName = trackEntry.Animation.Name;
		}
		if (animationName == "<empty>")
		{
			DebugLog.GeneralLogError($"{fromSpine.transform.parent.gameObject.name}의 {trackIndex}번 트랙에 현재 애니메이션이 비어 있습니다! 일단 idle로 적용합니다.");
			SetIdle();
		}
		else
		{
			float startTime = ((trackEntry.Animation.Name != animationName) ? 0f : (trackEntry.Loop ? (trackEntry.TrackTime % (trackEntry.AnimationEnd - trackEntry.AnimationStart)) : trackEntry.TrackTime));
			toSpine.SetAnimation(animationName, trackIndex, trackEntry.Loop, startTime, trackEntry.TimeScale, isImmediate);
		}
		void SetIdle()
		{
			toSpine.SetAnimation("idle", 0, loop: true, 0f, trackEntry.TimeScale, isImmediate);
		}
	}

	public static void SynchronizeAnimation(this SpineObject toSpine, SkeletonAnimation fromSpine, bool isImmediate = true, string animationName = null, int trackIndex = 0)
	{
		if ((object)toSpine == null)
		{
			DebugLog.GeneralLogError("toSpine is null!");
		}
		else if ((object)toSpine.SkeletonAnimation == null)
		{
			DebugLog.GeneralLogError("toSpine.SkeletonAnimation is null!");
		}
		else if ((object)fromSpine == null)
		{
			DebugLog.GeneralLogError("fromSpine is null!");
		}
		else
		{
			toSpine.SkeletonAnimation.SynchronizeAnimation(fromSpine, isImmediate, animationName, trackIndex);
		}
	}

	public static void SynchronizeAnimation(this SkeletonAnimation toSpine, SpineObject fromSpine, bool isImmediate = true, string animationName = null)
	{
		toSpine.SynchronizeAnimation(fromSpine.SkeletonAnimation, isImmediate, animationName);
	}

	public static void SetOrSyncAnimation(this SkeletonAnimation toSpine, SkeletonAnimation fromSpine, string animationName)
	{
		if (toSpine.AnimationName == animationName)
		{
			toSpine.SynchronizeAnimation(fromSpine);
		}
		else
		{
			toSpine.SetAnimation(animationName);
		}
	}

	public static void SetOrSyncAnimation(this SpineObject toSpineObject, SkeletonAnimation fromSpine, string animationName)
	{
		if (toSpineObject.SkeletonAnimation.AnimationName == animationName)
		{
			toSpineObject.SynchronizeAnimation(fromSpine);
		}
		else
		{
			toSpineObject.SetAnimation(animationName);
		}
	}

	public static bool IsAnimationComplete(this SkeletonAnimation spine, int trackIndex = 0)
	{
		return spine.AnimationState.GetCurrent(trackIndex).IsComplete;
	}

	public static void SynchronizeSelf(this SpineObject spineObject, string animationName)
	{
		spineObject.SynchronizeAnimation(spineObject.SkeletonAnimation, isImmediate: true, animationName);
	}
}
