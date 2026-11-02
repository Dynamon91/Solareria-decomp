using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.UI.Store;
using UnityEngine;

namespace SPR;

public static class Util
{
	public const string DateTimeFormat = "yyyy.MM.dd. HH:mm";

	public const float SlopeAngle = 26.56505f;

	public static Vector2 DisablePosition = -999f * Vector2.one;

	public static readonly List<Direction> FourDirections = new List<Direction>
	{
		Direction.Right,
		Direction.Left,
		Direction.Up,
		Direction.Down
	};

	private static readonly Dictionary<Type, Array> CachedEnumValueDict = new Dictionary<Type, Array>();

	public static float GetParticlesMaxLifeTime(GameObject go)
	{
		List<ParticleSystem> list = go.GetComponentsInChildren<ParticleSystem>().ToList();
		if (list.Count <= 0)
		{
			return 0f;
		}
		if (list.Count > 1)
		{
			list.RemoveAt(0);
		}
		return list.Aggregate(0f, (float current, ParticleSystem particle) => Mathf.Max(current, particle.main.startLifetime.constant));
	}

	public static float GetParticlesMaxLifeTime(ParticleSystem particleSystem)
	{
		ParticleSystem[] componentsInChildren = particleSystem.GetComponentsInChildren<ParticleSystem>();
		if (componentsInChildren.Length <= 1)
		{
			return componentsInChildren[0].main.startLifetime.constant / componentsInChildren[0].main.simulationSpeed;
		}
		return componentsInChildren.Where((ParticleSystem p) => (object)p != particleSystem).Aggregate(0f, (float current, ParticleSystem particle) => Mathf.Max(current, particle.main.startLifetime.constant / particle.main.simulationSpeed));
	}

	public static float GetParticlesMaxRemainTime(ParticleSystem particleSystem)
	{
		ParticleSystem[] componentsInChildren = particleSystem.GetComponentsInChildren<ParticleSystem>();
		if (componentsInChildren.Length <= 1)
		{
			return (componentsInChildren[0].main.startLifetime.constant + componentsInChildren[0].main.duration) / componentsInChildren[0].main.simulationSpeed;
		}
		return componentsInChildren.Where((ParticleSystem p) => (object)p != particleSystem).Aggregate(0f, (float current, ParticleSystem particle) => Mathf.Max(current, (particle.main.startLifetime.constant + particle.main.duration) / particle.main.simulationSpeed));
	}

	public static float GetParticlesMaxLifeTime(PoolableTimeLimitObject pooled)
	{
		return GetParticlesMaxLifeTime(pooled.gameObject);
	}

	public static float FrameToSeconds(float frame)
	{
		return frame * Time.fixedDeltaTime;
	}

	public static int SecondsToFrame(float seconds)
	{
		return Mathf.RoundToInt(seconds / Time.fixedDeltaTime);
	}

	public static int SecondsToFrame(double seconds)
	{
		return Mathf.RoundToInt((float)seconds / Time.fixedDeltaTime);
	}

	public static float GetCurrentAnimDuration(GameObject go)
	{
		if (!go.TryGetComponent<Animator>(out var component))
		{
			return 0f;
		}
		return component.GetCurrentAnimatorStateInfo(0).length;
	}

	public static float GetCurrentAnimDuration(PoolableTimeLimitObject pooled)
	{
		return GetCurrentAnimDuration(pooled.gameObject);
	}

	public static string GetLowerString(string origin)
	{
		char[] array = origin.ToCharArray();
		array[0] = char.ToLower(array[0]);
		return new string(array) ?? "";
	}

	public static void SetLoop(this ParticleSystem p, bool enabled)
	{
		ParticleSystem.MainModule main = p.main;
		main.loop = enabled;
	}

	public static void SetVcamFollow(Transform follow)
	{
		Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>().Follow = follow;
	}

	public static Transform GetVcamFollow()
	{
		return Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>().Follow;
	}

	public static Tween GetCameraOrthoSizeTween(float endValue, float duration)
	{
		CinemachineBrain component = Camera.main.GetComponent<CinemachineBrain>();
		CinemachineVirtualCamera vcam = component.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
		return DOTween.To(() => vcam.m_Lens.OrthographicSize, delegate(float x)
		{
			vcam.m_Lens.OrthographicSize = x;
		}, endValue, duration);
	}

	public static Tween GetCameraRotationTween(float angle, float duration)
	{
		CinemachineBrain component = Camera.main.GetComponent<CinemachineBrain>();
		CinemachineVirtualCamera vcam = component.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
		return DOTween.To(() => vcam.m_Lens.Dutch, delegate(float x)
		{
			vcam.m_Lens.Dutch = x;
		}, angle, duration);
	}

	public static Tween DoShakeForSecond(RectTransform rectTransform)
	{
		DOTween.Kill(rectTransform, complete: true);
		return rectTransform.DOShakeAnchorPos(0.5f, 3f, 30, 90f, snapping: false, fadeOut: true, ShakeRandomnessMode.Harmonic);
	}

	public static T GetRandomEnumValue<T>() where T : Enum
	{
		T[] enumValues = GetEnumValues<T>();
		return (T)enumValues.GetValue(UnityEngine.Random.Range(0, enumValues.Length));
	}

	public static T[] GetEnumValues<T>() where T : Enum
	{
		Type typeFromHandle = typeof(T);
		if (!CachedEnumValueDict.TryGetValue(typeFromHandle, out var value))
		{
			value = Enum.GetValues(typeFromHandle);
			CachedEnumValueDict[typeFromHandle] = value;
		}
		return (T[])value;
	}

	public static T AddToEnumWithClamp<T>(T enumValue, int delta) where T : Enum
	{
		T[] enumValues = GetEnumValues<T>();
		int index = Mathf.Clamp(min: 0, max: enumValues.Length - 1, value: Convert.ToInt32(enumValue) + delta);
		return (T)enumValues.GetValue(index);
	}

	public static T AddToEnumCircular<T>(T enumValue, int delta) where T : Enum
	{
		T[] enumValues = GetEnumValues<T>();
		int num = enumValues.Length;
		int num2 = (Convert.ToInt32(enumValue) + delta) % num;
		if (num2 < 0)
		{
			num2 += num;
		}
		return (T)enumValues.GetValue(num2);
	}

	public static float CalculateSlopeAngle(Vector2 normalVector2, bool isFacingRight)
	{
		float num;
		if (Mathf.Abs(normalVector2.x) < 0.01f)
		{
			num = 0f;
		}
		else
		{
			int num2 = Mathf.RoundToInt(normalVector2.y / normalVector2.x);
			num = Mathf.Atan(1f / (float)(-num2)) * 57.29578f;
		}
		return num + (float)((!isFacingRight) ? 180 : 0);
	}

	public static StoreType GetStoreType(this ActorName npcName)
	{
		return npcName switch
		{
			ActorName.Jima => StoreType.Peddler, 
			ActorName.Pwoong => StoreType.Vendor, 
			ActorName.Cardamine => StoreType.Artifacts, 
			ActorName.Leafa => StoreType.Restaurant, 
			ActorName.Flutis => StoreType.Blacksmith, 
			ActorName.Lunya => StoreType.Blacksmith, 
			ActorName.Sia => StoreType.Chemist, 
			_ => StoreType.None, 
		};
	}

	public static IReadOnlyList<StoreItemData> GetStoreItemDataList(ActorName npcName)
	{
		return npcName switch
		{
			ActorName.Jima => DataManager.Instance.GetJimaItemDataList(), 
			ActorName.Pwoong => DataManager.Instance.GetPwoongItemDataList(), 
			ActorName.Cardamine => DataManager.Instance.GetCardamineItemDataList(), 
			ActorName.Leafa => DataManager.Instance.GetLeafaItemDataList(), 
			ActorName.TouristPot => DataManager.Instance.GetTouristItemDataList(), 
			ActorName.Muha => DataManager.Instance.GetMuhaItemDataList(), 
			_ => null, 
		};
	}

	public static IReadOnlyList<SproutRewardData> GetSproutRewardDataList(RegionName regionName)
	{
		return regionName switch
		{
			RegionName.Terradin => Game.DataManager.GetEarthSproutDataList(), 
			RegionName.Ophilin => Game.DataManager.GetWaterSproutDataList(), 
			RegionName.Pyron => Game.DataManager.GetFireSproutDataList(), 
			RegionName.Zephiron => Game.DataManager.GetWindSproutDataList(), 
			RegionName.Mucora => Game.DataManager.GetSporeSproutDataList(), 
			RegionName.Neutral => Game.DataManager.GetShaftSproutDataList(), 
			_ => null, 
		};
	}

	public static T AddOrGetComponent<T>(this MonoBehaviour go) where T : Component
	{
		if (!go.TryGetComponent<T>(out var component))
		{
			return go.gameObject.AddComponent<T>();
		}
		return component;
	}

	public static string GetID(FieldID fieldID, string objectName)
	{
		return $"{fieldID}_{objectName}";
	}

	public static string GetID(FieldID fieldID, string objectName, Vector2Int position)
	{
		return $"{fieldID}_{objectName}_{position}";
	}

	public static (FieldID fieldID, string id, Vector2Int worldPos) ParseID(string id)
	{
		string[] array = id.Split('_');
		if (array.Length == 3)
		{
			SectorName sectorName = array[0].ToEnum<SectorName>();
			if (!int.TryParse(array[1], out var result))
			{
				return (fieldID: FieldID.Empty, id: string.Empty, worldPos: Vector2Int.zero);
			}
			if (array[2].IsNullOrEmpty())
			{
				return (fieldID: FieldID.Empty, id: string.Empty, worldPos: Vector2Int.zero);
			}
			return (fieldID: new FieldID(sectorName, result), id: array[2], worldPos: Vector2Int.zero);
		}
		if (array.Length == 4)
		{
			SectorName sectorName2 = array[0].ToEnum<SectorName>();
			if (!int.TryParse(array[1], out var result2))
			{
				return (fieldID: FieldID.Empty, id: string.Empty, worldPos: Vector2Int.zero);
			}
			if (array[2].IsNullOrEmpty())
			{
				return (fieldID: FieldID.Empty, id: string.Empty, worldPos: Vector2Int.zero);
			}
			if (!array[3].TryParseToVector2Int(out var result3))
			{
				return (fieldID: FieldID.Empty, id: string.Empty, worldPos: Vector2Int.zero);
			}
			return (fieldID: new FieldID(sectorName2, result2), id: array[2], worldPos: result3);
		}
		return (fieldID: FieldID.Empty, id: string.Empty, worldPos: Vector2Int.zero);
	}

	public static string GetRandomResult(EnemyDropData dropData)
	{
		(int, string)[] array = new(int, string)[6]
		{
			(dropData.CrackedSolinShard, "CrackedSolinShard"),
			(dropData.HardSolinShard, "HardSolinShard"),
			(dropData.SuperiorSolinShard, "SuperiorSolinShard"),
			(dropData.CrackedSolinMass, "CrackedSolinMass"),
			(dropData.HardSolinMass, "HardSolinMass"),
			(dropData.SuperiorSolinMass, "SuperiorSolinMass")
		};
		int num = array.Sum(((int value, string name) c) => c.value);
		if (num <= 0)
		{
			DebugLog.GeneralLogError(dropData.ID + "의 확률 구조가 유효하지 않습니다. 총합이 0 이하입니다.");
			return null;
		}
		int num2 = CryptoRandom.Range(0, num);
		(int, string)[] array2 = array;
		for (int num3 = 0; num3 < array2.Length; num3++)
		{
			(int, string) tuple = array2[num3];
			if (num2 < tuple.Item1)
			{
				return tuple.Item2;
			}
			num2 -= tuple.Item1;
		}
		return null;
	}

	public static bool IsSkillCoreStone(string itemID)
	{
		if (Game.DataManager.GetItemData(itemID).ItemType != ItemType.CoreStone)
		{
			return false;
		}
		return Game.DataManager.GetTraitData(itemID).MaxLevel == 0;
	}

	public static bool IsStatCoreStone(string itemID)
	{
		if (Game.DataManager.GetItemData(itemID).ItemType != ItemType.CoreStone)
		{
			return false;
		}
		return Game.DataManager.GetTraitData(itemID).MaxLevel > 0;
	}

	public static Vector3 GetConfinedPosition(this CinemachineVirtualCamera vcam)
	{
		Vector3 position = vcam.transform.position;
		CinemachineConfiner component = vcam.GetComponent<CinemachineConfiner>();
		PolygonCollider2D polygonCollider2D = component.m_BoundingShape2D as PolygonCollider2D;
		if (component.IsNull() || polygonCollider2D.IsNull())
		{
			return position;
		}
		float orthographicSize = vcam.m_Lens.OrthographicSize;
		float num = orthographicSize * 16f / 9f;
		Bounds bounds = polygonCollider2D.bounds;
		float num2 = bounds.min.x + num;
		float num3 = bounds.max.x - num;
		float num4 = bounds.min.y + orthographicSize;
		float num5 = bounds.max.y - orthographicSize;
		float x = position.x;
		float y = position.y;
		x = ((!(num2 > num3)) ? Mathf.Clamp(x, num2, num3) : ((bounds.min.x + bounds.max.x) * 0.5f));
		y = ((!(num4 > num5)) ? Mathf.Clamp(y, num4, num5) : ((bounds.min.y + bounds.max.y) * 0.5f));
		return new Vector3(x, y, position.z);
	}

	public static int CalculateTargetCellIndex<T>(this List<T> allCells, int currentIndex, int columnSize, int horizontal, int vertical, bool canExit = false)
	{
		if (vertical == 0 && horizontal == 0)
		{
			return currentIndex;
		}
		Direction direction = ((vertical > 0) ? Direction.Up : ((vertical < 0) ? Direction.Down : ((horizontal > 0) ? Direction.Right : Direction.Left)));
		int num = currentIndex;
		switch (direction)
		{
		case Direction.Right:
			num = allCells.FindFirstNonNullIndex(num + 1, canExit);
			break;
		case Direction.Left:
			num = allCells.FindLastNonNullIndex(num - 1, canExit);
			break;
		case Direction.Up:
			num = allCells.FindFirstNonNullIndex(num - columnSize, canExit);
			break;
		case Direction.Down:
			num = allCells.FindLastNonNullIndex((num != -1) ? (num + columnSize) : 0, canExit);
			break;
		}
		if ((num == -1 && !canExit) || num >= allCells.Count)
		{
			return currentIndex;
		}
		return num;
	}

	public static int FindFirstNonNullIndex<T>(this List<T> allCells, int startIndex, bool canExit = false)
	{
		if (canExit && (startIndex < 0 || startIndex >= allCells.Count))
		{
			return -1;
		}
		if (startIndex < 0)
		{
			startIndex = 0;
		}
		else if (startIndex >= allCells.Count)
		{
			startIndex = allCells.Count - 1;
		}
		for (int i = startIndex; i < allCells.Count; i++)
		{
			if (allCells[i] != null)
			{
				return i;
			}
		}
		return -1;
	}

	public static int FindLastNonNullIndex<T>(this List<T> allCells, int startIndex, bool canExit = false)
	{
		if (canExit && (startIndex < 0 || startIndex >= allCells.Count))
		{
			return -1;
		}
		if (startIndex < 0)
		{
			startIndex = 0;
		}
		else if (startIndex >= allCells.Count)
		{
			startIndex = allCells.Count - 1;
		}
		for (int num = startIndex; num >= 0; num--)
		{
			if (allCells[num] != null)
			{
				return num;
			}
		}
		return -1;
	}

	public static bool CheckShownInCamera(string id, params Vector2[] pos)
	{
		if (Game.Instance.CurrentState != GameState.Initialized)
		{
			return false;
		}
		if (!Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			return false;
		}
		if (data.TryGetObjectStateWatched(id, out var watched) && watched)
		{
			return true;
		}
		if (GraphicHelper.IsAnyInsideCamera(pos, out var result, 1f) && result)
		{
			data.SaveObjectStateWatched(id, watched: true);
		}
		return true;
	}

	public static int GetPrioritizedControl(params int[] values)
	{
		return values.FirstOrDefault((int v) => v != 0);
	}

	public static int GetNotOverlappedControl(int origin, int overlap)
	{
		if (overlap == 0)
		{
			return origin;
		}
		return 0;
	}
}
