using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cinemachine;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Library;

public static class BasicExtension
{
	public static bool IsNull(this UnityEngine.Object obj)
	{
		return !obj || (object)obj == null;
	}

	public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
	{
		if (enumerable != null)
		{
			if (enumerable is ICollection<T> collection)
			{
				return collection.Count < 1;
			}
			return !enumerable.Any();
		}
		return true;
	}

	public static T GetComponentInParentWithDepth<T>(this Transform transform, int depth) where T : MonoBehaviour
	{
		Transform parent = transform.parent;
		for (int i = 0; i < depth; i++)
		{
			if (parent.IsNull())
			{
				break;
			}
			if (parent.TryGetComponent<T>(out var component))
			{
				return component;
			}
			parent = parent.parent;
		}
		DebugLog.GeneralLogError($"{transform.name}의 {depth}단계 부모들 중 {typeof(T)}를 찾을 수 없습니다.");
		return null;
	}

	public static string GetGameObjectPath(this GameObject obj)
	{
		string text = "/" + obj.name;
		while (obj.transform.parent != null)
		{
			obj = obj.transform.parent.gameObject;
			text = "/" + obj.name + text;
		}
		return text;
	}

	public static T GetOrAddComponent<T>(this GameObject go) where T : Component
	{
		if (!go.TryGetComponent<T>(out var component))
		{
			return go.AddComponent<T>();
		}
		return component;
	}

	public static Vector2 ParseToVector2(this string str, char separator = ',')
	{
		string[] array = str.Split(separator);
		if (array.Length != 2)
		{
			throw new FormatException(str + "을 Vector2로 파싱할 수 없습니다.");
		}
		if (!TryParseFloat(array[0], out var result))
		{
			throw new FormatException(str + "의 첫번째 원소인 " + array[0] + "을 float으로 파싱할 수 없습니다.");
		}
		if (!TryParseFloat(array[1], out var result2))
		{
			throw new FormatException(str + "의 두번째 원소인 " + array[1] + "을 float으로 파싱할 수 없습니다.");
		}
		return new Vector2(result, result2);
	}

	public static List<string> ParseToStringList(this string str, char separator = ',')
	{
		if (string.IsNullOrEmpty(str))
		{
			return new List<string>();
		}
		return str.Replace(" ", "").Split(separator).ToList();
	}

	public static bool TryParseToVector2Int(this string str, out Vector2Int result, char separator = ',')
	{
		try
		{
			result = str.ParseToVector2Int(separator);
			return true;
		}
		catch (FormatException)
		{
			result = Vector2Int.zero;
			return false;
		}
	}

	public static Vector2Int ParseToVector2Int(this string str, char separator = ',')
	{
		string[] array = str.Replace("(", "").Replace(")", "").Split(separator);
		if (array.Length != 2)
		{
			throw new FormatException(str + "을 Vector2Int로 파싱할 수 없습니다.");
		}
		if (!array[0].TryParseInt(out var result))
		{
			throw new FormatException(str + "의 첫번째 원소인 " + array[0] + "을 int로 파싱할 수 없습니다.");
		}
		if (!array[1].TryParseInt(out var result2))
		{
			throw new FormatException(str + "의 두번째 원소인 " + array[1] + "을 int로 파싱할 수 없습니다.");
		}
		return new Vector2Int(result, result2);
	}

	public static List<T> ToEnumList<T>(this string str, char separator = ',') where T : struct
	{
		if (string.IsNullOrWhiteSpace(str))
		{
			return new List<T>();
		}
		List<T> list = new List<T>();
		string[] array = str.Split(separator);
		for (int i = 0; i < array.Length; i++)
		{
			if (Enum.TryParse<T>(array[i], out var result))
			{
				list.Add(result);
				continue;
			}
			throw new FormatException($"{str}을 {typeof(T)}으로 파싱할 수 없습니다.");
		}
		return list;
	}

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		List<T> list = source.ToList();
		foreach (T item in list)
		{
			action(item);
		}
		return list;
	}

	public static IEnumerable<T> ForEach<T, TR>(this IEnumerable<T> source, Func<T, TR> func)
	{
		List<T> list = source.ToList();
		foreach (T item in list)
		{
			func(item);
		}
		return list;
	}

	public static bool Contains<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T result)
	{
		foreach (T item in source)
		{
			if (predicate(item))
			{
				result = item;
				return true;
			}
		}
		result = default(T);
		return false;
	}

	public static void DebugGeneralLog<T>(this IEnumerable<T> source, string header, Func<T, string> body)
	{
		StringBuilder stringBuilder = new StringBuilder(header);
		foreach (T item in source)
		{
			stringBuilder.Append("\n" + body(item));
		}
		DebugLog.GeneralLog(stringBuilder.ToString());
	}

	public static void DebugGeneralLog<T>(this IEnumerable<T> source, Func<List<T>, string> header, Func<T, string> body)
	{
		List<T> list = source.ToList();
		StringBuilder stringBuilder = new StringBuilder(header(list));
		foreach (T item in list)
		{
			stringBuilder.Append("\n" + body(item));
		}
		DebugLog.GeneralLog(stringBuilder.ToString());
	}

	public static bool IsOneOf<T>(this T target, params T[] args)
	{
		return args.Contains(target);
	}

	public static IEnumerable<T> ToEnumerable<T>(this T[,] array)
	{
		for (int i = 0; i < array.GetLength(0); i++)
		{
			for (int j = 0; j < array.GetLength(1); j++)
			{
				yield return array[i, j];
			}
		}
	}

	public static IEnumerable<T> SafeReverse<T>(this IList<T> items)
	{
		for (int i = items.Count - 1; i >= 0; i--)
		{
			if (items[i] != null)
			{
				yield return items[i];
			}
		}
	}

	public static T GetElementSafely<T>(this IEnumerable<T> source, int index)
	{
		List<T> list = source.ToList();
		if (index < 0 || index >= list.Count)
		{
			return default(T);
		}
		return list[index];
	}

	public static Vector2 GetXY(this Vector3 v)
	{
		return new Vector2(v.x, v.y);
	}

	public static int RoundToInt(this float f)
	{
		return Mathf.RoundToInt(f);
	}

	public static float SnapToHalf(this float f)
	{
		return (float)Mathf.RoundToInt(f * 2f) / 2f;
	}

	public static float SnapToQuarter(this float f)
	{
		return (float)Mathf.RoundToInt(f * 4f) / 4f;
	}

	public static Vector2 ToVector2(this Vector3 v)
	{
		return new Vector2(v.x, v.y);
	}

	public static Vector2 ToVector2(this Vector2Int v)
	{
		return new Vector2(v.x, v.y);
	}

	public static Vector3 ToVector3(this Vector2Int v)
	{
		return new Vector3(v.x, v.y, 0f);
	}

	public static Vector2Int ToVector2Int(this Vector2 v)
	{
		return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
	}

	public static Vector2Int ToVector2Int(this Vector3 v)
	{
		return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
	}

	public static Vector3Int ToVector3Int(this Vector2Int v)
	{
		return new Vector3Int(v.x, v.y, 0);
	}

	public static Vector2 RoundToInt(this Vector3 v)
	{
		return new Vector2(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
	}

	public static Vector2 RoundToInt(this Vector2 v)
	{
		return new Vector2(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
	}

	public static Vector2 SnapToHalf(this Vector2 v)
	{
		return new Vector2((float)Mathf.RoundToInt(v.x * 2f) / 2f, (float)Mathf.RoundToInt(v.y * 2f) / 2f);
	}

	public static Vector3 SnapToHalf(this Vector3 v)
	{
		return new Vector3((float)Mathf.RoundToInt(v.x * 2f) / 2f, (float)Mathf.RoundToInt(v.y * 2f) / 2f, (float)Mathf.RoundToInt(v.z * 2f) / 2f);
	}

	public static Vector2 SnapToQuarter(this Vector2 v)
	{
		return new Vector2((float)Mathf.RoundToInt(v.x * 4f) / 4f, (float)Mathf.RoundToInt(v.y * 4f) / 4f);
	}

	public static Vector2 ToDirectionVector(this float angleInDegree)
	{
		return new Vector2(Mathf.Cos(angleInDegree * (MathF.PI / 180f)), Mathf.Sin(angleInDegree * (MathF.PI / 180f)));
	}

	public static Vector3 FlipX(this Vector3 v, bool flip = true)
	{
		if (!flip)
		{
			return v;
		}
		return new Vector3(0f - v.x, v.y, v.z);
	}

	public static Vector3 FlipY(this Vector3 v, bool flip = true)
	{
		if (!flip)
		{
			return v;
		}
		return new Vector3(v.x, 0f - v.y, v.z);
	}

	public static bool IsAlmostInteger(this float f)
	{
		return Mathf.Abs(f - Mathf.Round(f)) < 0.01f;
	}

	public static bool IsAlmostSame(this float f, float target, float epsilon = 0.01f)
	{
		return Mathf.Abs(f - target) < epsilon;
	}

	public static string EnteredString(this string originStr)
	{
		return originStr.Replace("\\n", "\n");
	}

	public static bool TryParseFloat(string s, out float result)
	{
		return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}

	public static float ParseFloat(string s)
	{
		return float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
	}

	public static bool TryParseInt(this string s, out int result)
	{
		return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
	}

	public static int ParseInt(string s)
	{
		return int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}

	public static int ParseIntOrDefault(this string s)
	{
		if (!s.TryParseInt(out var result))
		{
			return 0;
		}
		return result;
	}

	public static int ToInt(this string s)
	{
		return ParseInt(s);
	}

	public static float ToFloat(this string s)
	{
		return ParseFloat(s);
	}

	public static bool IsValidBracket(this string value)
	{
		int num = 0;
		foreach (char c in value)
		{
			if (num < 0)
			{
				return false;
			}
			int num2 = num;
			num = num2 + c switch
			{
				'(' => 1, 
				')' => -1, 
				_ => 0, 
			};
		}
		return num == 0;
	}

	public static int GetSign(this int i)
	{
		if (i <= 0)
		{
			if (i < 0)
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	public static int GetSign(this float f)
	{
		if (!(f > 0f))
		{
			if (f < 0f)
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	public static float GetAngle(this Vector2 v)
	{
		float num = Vector2.SignedAngle(Vector2.right, v);
		if (num < 0f)
		{
			num += 360f;
		}
		return num;
	}

	public static Vector2 GetUnitVector(this float angle)
	{
		angle = angle % 360f * (MathF.PI / 180f);
		return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
	}

	public static T ToEnum<T>(this string str) where T : struct
	{
		if (Enum.TryParse<T>(str, out var result))
		{
			return result;
		}
		throw new FormatException($"{str}을 {typeof(T)}으로 파싱할 수 없습니다.");
	}

	public static Direction ToDirection(this Vector3 vector, bool includeDiagonal = true)
	{
		return new Vector2(vector.x, vector.y).ToDirection(includeDiagonal);
	}

	public static Direction ToDirection(this Vector2 vector, bool includeDiagonal = true)
	{
		if (vector.magnitude < 0.01f)
		{
			DebugLog.GeneralLogWarning($"({vector.x}, {vector.y})는 너무 작아서 방향으로 인식할 수 없습니다.");
			return Direction.None;
		}
		vector = vector.normalized;
		float angle = vector.GetAngle();
		if (includeDiagonal)
		{
			if (337.5f <= angle || angle < 22.5f)
			{
				return Direction.Right;
			}
			if (22.5f <= angle && angle < 67.5f)
			{
				return Direction.UpRight;
			}
			if (67.5f <= angle && angle < 112.5f)
			{
				return Direction.Up;
			}
			if (112.5f <= angle && angle < 157.5f)
			{
				return Direction.UpLeft;
			}
			if (157.5f <= angle && angle < 202.5f)
			{
				return Direction.Left;
			}
			if (202.5f <= angle && angle < 247.5f)
			{
				return Direction.DownLeft;
			}
			if (247.5f <= angle && angle < 292.5f)
			{
				return Direction.Down;
			}
			if (292.5f <= angle && angle < 337.5f)
			{
				return Direction.DownRight;
			}
		}
		else
		{
			if (315f <= angle || angle < 45f)
			{
				return Direction.Right;
			}
			if (45f <= angle && angle < 135f)
			{
				return Direction.Up;
			}
			if (135f <= angle && angle < 225f)
			{
				return Direction.Left;
			}
			if (225f <= angle && angle < 315f)
			{
				return Direction.Down;
			}
		}
		DebugLog.GeneralLogWarning($"({vector.x}, {vector.y})를 방향으로 Convert 할 수 없습니다.");
		return Direction.None;
	}

	public static Vector2 ToVector2(this Direction direction)
	{
		return direction switch
		{
			Direction.Right => Vector2.right, 
			Direction.UpRight => new Vector2(1f, 1f).normalized, 
			Direction.Up => Vector2.up, 
			Direction.UpLeft => new Vector2(-1f, 1f).normalized, 
			Direction.Left => Vector2.left, 
			Direction.DownLeft => new Vector2(-1f, -1f).normalized, 
			Direction.Down => Vector2.down, 
			Direction.DownRight => new Vector2(1f, -1f).normalized, 
			_ => Vector2.zero, 
		};
	}

	public static Direction Opposite(this Direction direction)
	{
		return direction switch
		{
			Direction.Right => Direction.Left, 
			Direction.Left => Direction.Right, 
			Direction.Up => Direction.Down, 
			Direction.Down => Direction.Up, 
			Direction.UpRight => Direction.DownLeft, 
			Direction.UpLeft => Direction.DownRight, 
			Direction.DownRight => Direction.UpLeft, 
			Direction.DownLeft => Direction.UpRight, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}

	public static Direction Opposite(this Direction direction, bool onlyX)
	{
		Vector2 vector = direction.ToVector2();
		if (onlyX)
		{
			vector.x *= -1f;
		}
		else
		{
			vector.y *= -1f;
		}
		return vector.ToDirection();
	}

	public static Direction ExtractX(this Direction direction)
	{
		return direction switch
		{
			Direction.Right => Direction.Right, 
			Direction.Left => Direction.Left, 
			Direction.UpRight => Direction.Right, 
			Direction.UpLeft => Direction.Left, 
			Direction.DownRight => Direction.Right, 
			Direction.DownLeft => Direction.Left, 
			_ => Direction.None, 
		};
	}

	public static int GetSignX(this Direction direction)
	{
		return direction switch
		{
			Direction.Right => 1, 
			Direction.UpRight => 1, 
			Direction.DownRight => 1, 
			Direction.Left => -1, 
			Direction.UpLeft => -1, 
			Direction.DownLeft => -1, 
			_ => 0, 
		};
	}

	public static T GetChild<T>(this Transform transform, string childName) where T : Component
	{
		foreach (Transform item in transform)
		{
			T component;
			if (item.name == childName)
			{
				return item.TryGetComponent<T>(out component) ? component : null;
			}
		}
		return null;
	}

	public static float GetCurrentVirtualCameraSize(this CinemachineBrain cineBrain)
	{
		if (cineBrain.ActiveVirtualCamera != null)
		{
			CinemachineVirtualCamera cinemachineVirtualCamera = cineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
			if (!cinemachineVirtualCamera.IsNull())
			{
				return cinemachineVirtualCamera.m_Lens.OrthographicSize;
			}
			DebugLog.ScenarioLogError(cineBrain.name + "에서 Active Virtual Camera를 가져오는데 실패했습니다.");
		}
		return float.MaxValue;
	}

	public static T CastObject<T>(this object obj) where T : class
	{
		if (obj is T result)
		{
			return result;
		}
		return null;
	}

	public static bool TryGetComponentFromParents<T>(this Transform transform, out T component)
	{
		Transform transform2 = transform;
		while (!transform2.IsNull())
		{
			if (transform2.TryGetComponent<T>(out var component2))
			{
				component = component2;
				return true;
			}
			transform2 = transform2.parent;
		}
		component = default(T);
		return false;
	}

	public static Direction GetDirection(this AttackDirection attackDirection, Direction facingDirection)
	{
		return attackDirection switch
		{
			AttackDirection.Facing => facingDirection, 
			AttackDirection.OppositeFacing => facingDirection.Opposite(), 
			AttackDirection.Both => Direction.None, 
			_ => throw new ArgumentOutOfRangeException("attackDirection", attackDirection, null), 
		};
	}

	public static void StopLooping(this ParticleSystem ps, bool includeChildren = true)
	{
		ParticleSystem.MainModule main = ps.main;
		main.loop = false;
		if (includeChildren)
		{
			ParticleSystem[] componentsInChildren = ps.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ParticleSystem.MainModule main2 = componentsInChildren[i].main;
				main2.loop = false;
			}
		}
	}

	public static void SetAlpha(this SpriteRenderer sr, float alpha)
	{
		Color color = sr.color;
		color.a = Mathf.Clamp01(alpha);
		sr.color = color;
	}

	public static void SetAlpha(this Skeleton skeleton, float alpha)
	{
		Color color = skeleton.GetColor();
		color.a = Mathf.Clamp01(alpha);
		skeleton.SetColor(color);
	}

	public static void SetAlpha(this Graphic graphic, float alpha)
	{
		Color color = graphic.color;
		color.a = Mathf.Clamp01(alpha);
		graphic.color = color;
	}

	public static float GetIntensity(this Color color)
	{
		float maxColorComponent = color.maxColorComponent;
		float num = 191f / maxColorComponent;
		return Mathf.Log(255f / num) / Mathf.Log(2f);
	}

	public static Color ApplyIntensity(this Color color, float intensity)
	{
		float num = Mathf.Pow(2f, intensity);
		return new Color(color.r * num, color.g * num, color.b * num, color.a);
	}

	public static void SynchronizeValues(this Transform to, Transform from)
	{
		to.position = from.position;
		to.rotation = from.rotation;
		to.localScale = from.localScale;
	}

	public static void SynchronizePositionDirection(this Transform transform, Transform origin, Vector3 offset = default(Vector3))
	{
		if (transform.IsNull() || origin.IsNull())
		{
			DebugLog.GeneralLogWarning("두 Transform 중 null인 것이 있어서 위치와 방향을 동기화할 수 없습니다!");
			return;
		}
		transform.position = origin.position + offset;
		transform.localScale = origin.localScale;
	}

	public static void SynchronizeLocalPositionDirection(this Transform transform, Transform origin, Vector3 offset = default(Vector3))
	{
		if (transform.IsNull() || origin.IsNull())
		{
			DebugLog.GeneralLogWarning("두 Transform 중 null인 것이 있어서 위치와 방향을 동기화할 수 없습니다!");
			return;
		}
		transform.localPosition = origin.localPosition + offset;
		transform.localScale = origin.localScale;
	}

	public static void ReflectXWithFlip(this Transform transform, float origin, bool includeFlip = true)
	{
		transform.position = new Vector3(2f * origin - transform.position.x, transform.position.y, transform.position.z);
		if (includeFlip)
		{
			transform.localScale = new Vector3(0f - transform.localScale.x, transform.localScale.y, transform.localScale.z);
		}
	}

	public static void AlignPosX(this Transform target, Transform source, float offset = 0f)
	{
		target.position = new Vector3(source.position.x + offset, target.position.y, target.position.z);
	}

	public static void SetDirection(this Transform target, bool isRight)
	{
		Vector3 localScale = target.localScale;
		if (!(localScale.x * (float)(isRight ? 1 : (-1)) >= 0f))
		{
			target.localScale = new Vector3(0f - localScale.x, localScale.y, localScale.z);
		}
	}

	public static bool Contains<T>(this List<T> source, Type type)
	{
		return source.Find((T x) => x.GetType() == type) != null;
	}

	public static void Flip(this Transform transform, float direction)
	{
		transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
	}

	public static void SetScaleX(this Transform transform, float value)
	{
		transform.localScale = new Vector3(value, transform.localScale.y, transform.localScale.z);
	}

	public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
	{
		T val = gameObject.GetComponent<T>();
		if (val == null)
		{
			val = gameObject.AddComponent<T>();
		}
		return val;
	}
}
