using SPR.Graphic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(ShaderActivationClip))]
public class ShaderActivationTrack : TrackAsset
{
	public GraphicManager.RenderFeatureName renderFeatureName;

	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<ShaderActivationBehaviour> scriptPlayable = ScriptPlayable<ShaderActivationBehaviour>.Create(graph);
		scriptPlayable.GetBehaviour().RenderFeatureName = renderFeatureName;
		return scriptPlayable;
	}
}
