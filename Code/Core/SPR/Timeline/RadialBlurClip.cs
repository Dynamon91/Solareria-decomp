using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

public class RadialBlurClip : PlayableAsset
{
	public float intensity;

	[Tooltip("집중의 중심점. target이 비어있는 경우에만 사용됩니다.")]
	public Vector2 center;

	[Tooltip("집중의 대상. 비어있으면 Center를 사용합니다.")]
	public ExposedReference<Transform> target;

	public float delay = 0.1f;

	public int numberOfSamples;

	public ClipCaps clipCaps => ClipCaps.Blending;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<RadialBlurBehaviour> scriptPlayable = ScriptPlayable<RadialBlurBehaviour>.Create(graph);
		RadialBlurBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Intensity = intensity;
		behaviour.Center = center;
		behaviour.Target = target.Resolve(graph.GetResolver());
		behaviour.Delay = delay;
		behaviour.NumberOfSamples = numberOfSamples;
		return scriptPlayable;
	}
}
