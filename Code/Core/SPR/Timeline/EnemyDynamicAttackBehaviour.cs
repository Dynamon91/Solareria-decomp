using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyDynamicAttackBehaviour : EnemyAttackBehaviour
{
	public Zone Zone;

	public Direction Direction;

	private int frameCount;

	private float speed;

	private float range;

	private float width;

	private float height;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		base.OnBehaviourPlay(playable, info);
		frameCount = 0;
		int num = Util.SecondsToFrame(playable.GetDuration());
		speed = Mathf.Abs(Zone.UpperRight.x - Zone.LowerLeft.x) / (float)num;
		range = Mathf.Abs(Zone.UpperRight.x - Zone.LowerLeft.x);
		width = range / (float)num;
		height = Mathf.Abs(Zone.UpperRight.y - Zone.LowerLeft.y);
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if ((object)Enemy == null)
		{
			Enemy = playerData.CastObject<Enemy>();
		}
		HitInfo = new HitInfo(Enemy, Key);
		int signX = Direction.GetSignX();
		Vector2 vector = new Vector2(Zone.LowerLeft.x + (float)signX * (width / 2f), Zone.LowerLeft.y + height / 2f);
		Zone zone = new Zone(size: new Vector2(width, Zone.Size.y), center: vector + speed * (float)frameCount * Direction.ToVector2());
		Attack(zone);
		frameCount++;
	}
}
