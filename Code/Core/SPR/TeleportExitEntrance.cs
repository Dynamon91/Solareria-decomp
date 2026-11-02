using UnityEngine;

namespace SPR;

public class TeleportExitEntrance : Entrance
{
	[Header("Teleport Exit Entrance")]
	[SerializeField]
	private Vector2 positionExit;

	[SerializeField]
	private Direction direction;

	protected override void Start()
	{
	}

	protected override void FixedUpdate()
	{
	}

	protected override Direction GetEntranceDirection()
	{
		return direction;
	}

	public override Vector2 GetPlayerSpawnPosition()
	{
		return positionExit;
	}

	public override Vector2 GetPlayerEnterEndPosition()
	{
		return positionExit;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.DrawWireSphere(positionExit, 0.5f);
	}
}
