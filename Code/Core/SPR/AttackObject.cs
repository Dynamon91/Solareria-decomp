using UnityEngine;

namespace SPR;

public class AttackObject : MonoBehaviour, IAttackSource
{
	[SerializeField]
	private string objectName;

	[SerializeField]
	private Direction direction;

	public string Name => objectName;

	public Direction FacingDirection => direction;
}
