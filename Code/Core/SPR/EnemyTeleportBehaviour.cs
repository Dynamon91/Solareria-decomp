using System.Linq;
using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyTeleportBehaviour : PlayableBehaviour
{
	public Enemy Enemy;

	public Pivot Pivot;

	public Vector3 AnchorPosition;

	public int Direction;

	public bool UsePositionToAnchor;

	public bool UseSharedAnchorPosition;

	public bool LandingGround;

	public Transform Position;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (UsePositionToAnchor)
		{
			playable.GetGraph().GetResolver().CastObject<PlayableDirector>()
				.playableAsset.outputs.FirstOrDefault((PlayableBinding x) => x.sourceObject is EnemyMoveToTrack).sourceObject.CastObject<EnemyMoveToTrack>().AnchorPosition = ((Pivot == Pivot.Self) ? Enemy.VisualBottom : Game.Instance.Player.mover.VisualBottom);
		}
		if (!UseSharedAnchorPosition)
		{
			AnchorPosition = ((Pivot == Pivot.Self) ? Enemy.VisualBottom : Game.Instance.Player.mover.VisualBottom);
			Direction = Enemy.FacingDirection.GetSignX();
		}
		Enemy.SetVisible(visible: false);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		double duration = playable.GetDuration();
		double time = playable.GetTime();
		double num = time + (double)info.deltaTime;
		if ((info.effectivePlayState == PlayState.Paused && num > duration) || Mathf.Approximately((float)time, (float)duration))
		{
			Vector3 position = new Vector3(AnchorPosition.x + Position.localPosition.x * (float)Direction, AnchorPosition.y + Position.localPosition.y);
			if (LandingGround)
			{
				position.y = Enemy.GroundPosition.y;
			}
			Enemy.transform.position = position;
			Enemy.SetVisible(visible: true);
		}
	}
}
