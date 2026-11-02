using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class CinemachineImpulseClip : PlayableAsset
{
	public ExposedReference<CinemachineImpulseSource> impulseSource;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<CinemachineImpulseBehaviour> scriptPlayable = ScriptPlayable<CinemachineImpulseBehaviour>.Create(graph);
		scriptPlayable.GetBehaviour().ImpulseSource = impulseSource.Resolve(graph.GetResolver());
		return scriptPlayable;
	}
}
