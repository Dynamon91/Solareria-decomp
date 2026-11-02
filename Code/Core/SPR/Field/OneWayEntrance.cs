using UnityEngine;

namespace SPR.Field;

public class OneWayEntrance : MonoBehaviour, IEntrance
{
	[SerializeField]
	private Area connectArea;

	[SerializeField]
	private Direction direction;

	public Area ConnectArea => connectArea;

	public bool IsVertical => false;

	public Direction Direction => direction;

	public Vector2 GetPlayerSpawnPosition()
	{
		return base.transform.position;
	}

	public Vector2 GetPlayerEnterEndPosition()
	{
		return base.transform.position;
	}

	GameObject IEntrance.gameObject => base.gameObject;
}
