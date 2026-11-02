using Library;
using UnityEngine;

namespace SPR.Field;

public readonly struct GroundInfo
{
	private readonly Vector2 position;

	private readonly Vector2 normal;

	private readonly GroundGroup myGroup;

	public float X => position.x;

	public float Y => position.y;

	public Vector2 Position => position;

	public GroundGroup MyGroup => myGroup;

	public bool IsDisable
	{
		get
		{
			if (!(position == Util.DisablePosition))
			{
				return normal == Vector2.zero;
			}
			return true;
		}
	}

	public static GroundInfo Disable => new GroundInfo(Util.DisablePosition, Vector2.zero, GroundGroup.Disable);

	public GroundInfo(Vector2 position, Vector2 normal, GroundGroup myGroup)
	{
		this.position = position;
		this.normal = normal;
		this.myGroup = myGroup;
	}

	public Vector2 GetDirectionVector(Direction direction)
	{
		if (direction != Direction.Left && direction != Direction.Right)
		{
			DebugLog.GeneralLogError($"방향 벡터를 구할 수 없는 방향이 들어왔습니다. 입력 방향:{direction}, 타일 위치:{position}");
			return Vector2.zero;
		}
		bool isFacingRight = direction == Direction.Right;
		return Util.CalculateSlopeAngle(normal, isFacingRight).ToDirectionVector();
	}
}
