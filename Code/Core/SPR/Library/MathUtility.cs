using System;
using UnityEngine;

namespace SPR.Library;

public static class MathUtility
{
	public static float ClampTo360Angle(float angle)
	{
		float num = angle % 360f;
		if (num < 0f)
		{
			num += 360f;
		}
		return num;
	}

	public static Direction ToDirection(float x, float y, bool includeDiagonal = false)
	{
		float num = 0.01f;
		if (Mathf.Abs(x) < num)
		{
			if (Mathf.Abs(y) < num)
			{
				return Direction.None;
			}
			if (y > 0f)
			{
				return Direction.Up;
			}
			return Direction.Down;
		}
		if (Mathf.Abs(y) < num)
		{
			if (Mathf.Abs(x) < num)
			{
				return Direction.None;
			}
			if (x > 0f)
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
		float num2 = Mathf.Atan2(y, x) * 57.29578f;
		if (includeDiagonal)
		{
			if (337.5f <= num2 || num2 < 22.5f)
			{
				return Direction.Right;
			}
			if (22.5f <= num2 && num2 < 67.5f)
			{
				return Direction.UpRight;
			}
			if (67.5f <= num2 && num2 < 112.5f)
			{
				return Direction.Up;
			}
			if (112.5f <= num2 && num2 < 157.5f)
			{
				return Direction.UpLeft;
			}
			if (157.5f <= num2 && num2 < 202.5f)
			{
				return Direction.Left;
			}
			if (202.5f <= num2 && num2 < 247.5f)
			{
				return Direction.DownLeft;
			}
			if (247.5f <= num2 && num2 < 292.5f)
			{
				return Direction.Down;
			}
			if (292.5f <= num2 && num2 < 337.5f)
			{
				return Direction.DownRight;
			}
		}
		else
		{
			if (315f <= num2 || num2 < 45f)
			{
				return Direction.Right;
			}
			if (45f <= num2 && num2 < 135f)
			{
				return Direction.Up;
			}
			if (135f <= num2 && num2 < 225f)
			{
				return Direction.Left;
			}
			if (225f <= num2 && num2 < 315f)
			{
				return Direction.Down;
			}
		}
		DebugLog.GeneralLogWarning($"({x}, {y})를 방향으로 Convert 할 수 없습니다.");
		return Direction.None;
	}

	public static bool IsAlmostInteger(float value, float tolerance = 0.001f)
	{
		return Mathf.Abs(value - Mathf.Round(value)) < tolerance;
	}

	public static Vector2 RotateVector(this Vector2 vec, float angle)
	{
		float num = Mathf.Cos(angle * (MathF.PI / 180f));
		float num2 = Mathf.Sin(angle * (MathF.PI / 180f));
		return new Vector2(num * vec.x - num2 * vec.y, num2 * vec.x + num * vec.y);
	}

	public static bool CompareSign(float f1, float f2)
	{
		if (f1 != 0f && f2 != 0f)
		{
			return Mathf.Sign(f1) == Mathf.Sign(f2);
		}
		return false;
	}

	public static float Normalize(float value, float min, float max)
	{
		return (value - min) / (max - min);
	}
}
