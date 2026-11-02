using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyTeleportClip : EnemyMoveToClipBase
{
	public ExposedReference<Transform> position;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyTeleportBehaviour> scriptPlayable = ScriptPlayable<EnemyTeleportBehaviour>.Create(graph);
		EnemyTeleportBehaviour behaviour = scriptPlayable.GetBehaviour();
		Transform parent = owner.transform.parent.parent;
		behaviour.Enemy = parent.GetComponent<Enemy>();
		behaviour.Pivot = pivot;
		behaviour.Position = position.Resolve(graph.GetResolver());
		behaviour.UsePositionToAnchor = usePositionToAnchor;
		behaviour.UseSharedAnchorPosition = useAnchorPosition;
		behaviour.LandingGround = landingGround;
		if (useAnchorPosition)
		{
			behaviour.AnchorPosition = anchorPosition;
			behaviour.Direction = direction.GetSignX();
		}
		return scriptPlayable;
	}
}
