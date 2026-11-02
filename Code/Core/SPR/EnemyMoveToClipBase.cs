using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace SPR;

public abstract class EnemyMoveToClipBase : PlayableAsset
{
	public Pivot pivot;

	[HideInInspector]
	public Vector3 anchorPosition;

	[HideInInspector]
	public Direction direction;

	public bool usePositionToAnchor;

	[FormerlySerializedAs("useInitialPosition")]
	[FormerlySerializedAs("useCurrentPosition")]
	public bool useAnchorPosition;

	public bool keepTrackAnchor;

	public bool useCurrentDirection;

	public bool landingGround;
}
