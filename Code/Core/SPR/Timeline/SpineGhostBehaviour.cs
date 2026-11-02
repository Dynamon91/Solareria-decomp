using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class SpineGhostBehaviour : PlayableBehaviour
{
	public MeshFilter TargetMeshFilter;

	public float SpawnInterval;

	public float FadeSpeed;

	public Color GhostColor;

	private SkeletonGhost ghost;

	private bool isInitialized;

	private MeshFilter originMeshFilter;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		ghost = playerData as SkeletonGhost;
		if (ghost == null)
		{
			return;
		}
		if (!isInitialized)
		{
			if (TargetMeshFilter != null)
			{
				originMeshFilter = ghost.targetMeshFilter;
				ghost.targetMeshFilter = TargetMeshFilter;
			}
			isInitialized = true;
		}
		ghost.ghostingEnabled = true;
		ghost.spawnInterval = SpawnInterval;
		ghost.fadeSpeed = FadeSpeed;
		ghost.color = GhostColor;
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (!(ghost == null))
		{
			ghost.ghostingEnabled = false;
			if (TargetMeshFilter != null)
			{
				ghost.targetMeshFilter = originMeshFilter;
			}
		}
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		if (!(ghost == null))
		{
			ghost.ghostingEnabled = false;
			if (TargetMeshFilter != null)
			{
				ghost.targetMeshFilter = originMeshFilter;
			}
		}
	}
}
