using Library;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class LoopClip : PlayableAsset
{
	public int loopClipIndex;

	public ExitType exitType;

	[Header("루프 테스트용")]
	public bool loopTest;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<LoopBehaviour> scriptPlayable = ScriptPlayable<LoopBehaviour>.Create(graph);
		LoopBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Producer = owner.GetComponent<CutsceneProducer>();
		behaviour.Director = owner.GetComponent<PlayableDirector>();
		foreach (TrackAsset outputTrack in behaviour.Director.playableAsset.CastObject<TimelineAsset>().GetOutputTracks())
		{
			foreach (TimelineClip clip in outputTrack.GetClips())
			{
				if (!(clip.asset != this))
				{
					behaviour.Clip = clip;
					break;
				}
			}
		}
		behaviour.LoopClipIndex = loopClipIndex;
		return scriptPlayable;
	}
}
