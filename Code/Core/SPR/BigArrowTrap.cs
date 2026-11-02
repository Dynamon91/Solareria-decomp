using Library;
using UnityEngine;

namespace SPR;

public class BigArrowTrap : TriggerTrap
{
	[SerializeField]
	private float arrowSpeed;

	[SerializeField]
	private Direction direction;

	private SpriteRenderer spriteRenderer;

	private SpriteRenderer SpriteRenderer => spriteRenderer ?? (spriteRenderer = GetComponent<SpriteRenderer>());

	public override void ActivateTrap()
	{
		Vector3 pos = new Vector3(base.transform.position.x + (float)direction.GetSignX() * SpriteRenderer.bounds.size.x, base.transform.position.y);
		TriggerTrap.PoolManager.GetObject<BigArrow>("BigArrow.prefab", pos).Shoot(arrowSpeed, direction);
	}
}
