using UnityEngine;

namespace SPR.Field;

public interface IEntrance
{
	GameObject gameObject { get; }

	Area ConnectArea { get; }

	bool IsVertical { get; }

	Vector2 GetPlayerSpawnPosition();

	Vector2 GetPlayerEnterEndPosition();
}
