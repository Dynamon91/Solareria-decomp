using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class SpineGhostClip : PlayableAsset
{
	public ExposedReference<GameObject> targetMeshFilterObject;

	public float spawnInterval;

	public float fadeSpeed;

	public Color ghostColor;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<SpineGhostBehaviour> scriptPlayable = ScriptPlayable<SpineGhostBehaviour>.Create(graph);
		SpineGhostBehaviour behaviour = scriptPlayable.GetBehaviour();
		if (targetMeshFilterObject.defaultValue != null && targetMeshFilterObject.Resolve(graph.GetResolver()).TryGetComponent<MeshFilter>(out var component))
		{
			behaviour.TargetMeshFilter = component;
		}
		behaviour.SpawnInterval = spawnInterval;
		behaviour.FadeSpeed = fadeSpeed;
		behaviour.GhostColor = ghostColor;
		return scriptPlayable;
	}
}
