using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ColorVignetteClip : PlayableAsset
{
	public Color color = Color.white;

	[Range(0f, 1f)]
	public float radius = 1f;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<ColorVignetteBehaviour> scriptPlayable = ScriptPlayable<ColorVignetteBehaviour>.Create(graph);
		ColorVignetteBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Color = color;
		behaviour.Radius = radius;
		return scriptPlayable;
	}
}
