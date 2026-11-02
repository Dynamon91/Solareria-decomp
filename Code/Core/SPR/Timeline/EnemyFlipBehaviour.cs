using Library;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyFlipBehaviour : PlayableBehaviour
{
	public bool LookPlayer;

	private Enemy enemy;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if ((object)enemy == null)
		{
			enemy = playerData.CastObject<Enemy>();
		}
		if (LookPlayer)
		{
			enemy.LookPlayer();
		}
		else
		{
			enemy.Flip(enemy.FacingDirection.Opposite());
		}
	}
}
