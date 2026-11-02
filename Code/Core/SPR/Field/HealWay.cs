using UnityEngine;

namespace SPR.Field;

public class HealWay : MonoBehaviour
{
	[SerializeField]
	private int cycleFrame;

	[SerializeField]
	private int healAmount;

	private BoxCollider2D boxColl;

	private int frameCount;

	private bool isOverlap;

	private BoxCollider2D BoxCollider => boxColl ?? (boxColl = GetComponent<BoxCollider2D>());

	private PlayerMover PlayerMover => Game.Instance.Player.mover;

	private void FixedUpdate()
	{
		bool flag = isOverlap;
		isOverlap = BoxCollider.bounds.Intersects(PlayerMover.ColliderBounds);
		if (isOverlap)
		{
			if (!flag)
			{
				frameCount = 0;
			}
			frameCount++;
			frameCount %= cycleFrame;
			if (frameCount == 0)
			{
				Game.Instance.Player.PlayerStat.GetStat<Health>().AddHealth(healAmount);
			}
		}
	}
}
