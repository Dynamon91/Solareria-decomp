using Library;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR;

[TrackColor(0f, 0f, 0f)]
[TrackClipType(typeof(EnemyMoveToClipBase))]
[TrackBindingType(typeof(Enemy))]
public class EnemyMoveToTrack : PlayableTrack
{
	private Direction direction;

	public Vector3 AnchorPosition { get; set; }

	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		Enemy component = gameObject.transform.parent.parent.GetComponent<Enemy>();
		EnemyMoveToClipBase enemyMoveToClipBase = clip.asset.CastObject<EnemyMoveToClipBase>();
		if (enemyMoveToClipBase.useAnchorPosition)
		{
			AnchorPosition = ((enemyMoveToClipBase.pivot == Pivot.Self) ? component.VisualBottom : Game.Instance.Player.mover.VisualBottom);
			direction = component.FacingDirection;
			enemyMoveToClipBase.anchorPosition = AnchorPosition;
			enemyMoveToClipBase.direction = direction;
		}
		return base.CreatePlayable(graph, gameObject, clip);
	}
}
