using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(1f, 0.2509804f, 0.003921569f)]
[TrackClipType(typeof(SpineSkinClip))]
[TrackBindingType(typeof(SkeletonAnimation))]
public class SpineSkinTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<SpineSkinBehaviour> scriptPlayable = ScriptPlayable<SpineSkinBehaviour>.Create(graph);
		SpineSkinBehaviour behaviour = scriptPlayable.GetBehaviour();
		SpineSkinClip spineSkinClip = (SpineSkinClip)clip.asset;
		behaviour.SkinName = spineSkinClip.skinName;
		return scriptPlayable;
	}
}
