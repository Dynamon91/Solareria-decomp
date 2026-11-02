using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class FocusClip : PlayableAsset
{
	public Color color = Color.white;

	public float alpha;

	[Tooltip("집중의 중심점. target이 비어있는 경우에만 사용됩니다.")]
	public Vector2 centralPoint;

	[Tooltip("집중의 대상. 비어있으면 Central Point를 사용합니다.")]
	public ExposedReference<Transform> target;

	[Range(0f, 1f)]
	public float maskSize;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<FocusBehaviour> scriptPlayable = ScriptPlayable<FocusBehaviour>.Create(graph);
		FocusBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Color = color;
		behaviour.Alpha = alpha;
		behaviour.CentralPoint = centralPoint;
		behaviour.Target = target.Resolve(graph.GetResolver());
		behaviour.MaskSize = maskSize;
		return scriptPlayable;
	}
}
