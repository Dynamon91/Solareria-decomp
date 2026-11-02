using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(SpineGhostClip))]
[TrackBindingType(typeof(SkeletonGhost))]
public class SpineGhostTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<SpineGhostBehaviour> scriptPlayable = ScriptPlayable<SpineGhostBehaviour>.Create(graph);
		SpineGhostBehaviour behaviour = scriptPlayable.GetBehaviour();
		SpineGhostClip spineGhostClip = (SpineGhostClip)clip.asset;
		GameObject gameObject2 = spineGhostClip.targetMeshFilterObject.Resolve(graph.GetResolver());
		if (gameObject2 != null && gameObject2.TryGetComponent<MeshFilter>(out var component))
		{
			behaviour.TargetMeshFilter = component;
		}
		behaviour.SpawnInterval = spineGhostClip.spawnInterval;
		behaviour.FadeSpeed = spineGhostClip.fadeSpeed;
		behaviour.GhostColor = spineGhostClip.ghostColor;
		return scriptPlayable;
	}
}
