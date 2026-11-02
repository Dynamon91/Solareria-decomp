using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(PathMoveClip))]
[TrackBindingType(typeof(Transform))]
public class PathMoveTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<PathMoveBehaviour> scriptPlayable = ScriptPlayable<PathMoveBehaviour>.Create(graph);
		PathMoveBehaviour behaviour = scriptPlayable.GetBehaviour();
		PathMoveClip pathMoveClip = (PathMoveClip)clip.asset;
		behaviour.Path = pathMoveClip.path.Resolve(graph.GetResolver());
		behaviour.Curve = pathMoveClip.curve;
		return scriptPlayable;
	}
}
