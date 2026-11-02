using PathCreation;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class PathMoveClip : PlayableAsset
{
	public ExposedReference<PathCreator> path;

	public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<PathMoveBehaviour> scriptPlayable = ScriptPlayable<PathMoveBehaviour>.Create(graph);
		PathMoveBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Path = path.Resolve(graph.GetResolver());
		behaviour.Curve = curve;
		return scriptPlayable;
	}
}
