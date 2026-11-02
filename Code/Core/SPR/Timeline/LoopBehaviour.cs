using Library;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class LoopBehaviour : PlayableBehaviour
{
	public CutsceneProducer Producer;

	public PlayableDirector Director;

	public TimelineClip Clip;

	public int LoopClipIndex;

	private bool isInitialized;

	private bool shouldLoop;

	private bool isReallyFirstFrame;

	private bool isFirstLoop;

	private LoopClip ClipAsset => (LoopClip)Clip.asset;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		Initialize();
		if (!Application.isPlaying)
		{
			shouldLoop = ClipAsset.loopTest;
			return;
		}
		if (isReallyFirstFrame)
		{
			Producer.EnterLoop(LoopClipIndex);
			isReallyFirstFrame = false;
		}
		if (shouldLoop && !Producer.ShouldLoop(LoopClipIndex))
		{
			shouldLoop = false;
			Producer.ExitLoop(LoopClipIndex);
			if (ClipAsset.exitType == ExitType.Immediate || (ClipAsset.exitType == ExitType.FinishOnceImmediate && !isFirstLoop))
			{
				Director.time = Clip.end;
			}
		}
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		shouldLoop = true;
		base.OnBehaviourPlay(playable, info);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (!BasicExtension.IsNull(Director))
		{
			bool flag = false;
			if (shouldLoop)
			{
				Director.time = Clip.start;
				flag = true;
			}
			if (!Application.isPlaying && ClipAsset.loopTest)
			{
				Director.time = Clip.start;
				flag = true;
			}
			if (flag)
			{
				isFirstLoop = false;
			}
			base.OnBehaviourPause(playable, info);
		}
	}

	public override void OnGraphStart(Playable playable)
	{
		isFirstLoop = true;
	}

	private void Initialize()
	{
		if (!isInitialized)
		{
			isReallyFirstFrame = true;
			isInitialized = true;
		}
	}
}
