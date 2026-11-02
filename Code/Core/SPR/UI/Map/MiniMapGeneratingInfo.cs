using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR.UI.Map;

[CreateAssetMenu]
public class MiniMapGeneratingInfo : ScriptableObject
{
	[SerializeField]
	public Vector2 totalOffset;

	[SerializeField]
	public List<FieldInformation> fields = new List<FieldInformation>();

	[SerializeField]
	public List<MapVariantInformation> variants = new List<MapVariantInformation>();

	private readonly List<FieldInformation> targetFields = new List<FieldInformation>();

	private readonly List<FieldInformation> fieldsBackUp = new List<FieldInformation>();

	public Dictionary<FieldSubtitle, Vector4> Subtitles { get; } = new Dictionary<FieldSubtitle, Vector4>();

	public List<UnconnectedInformation> UnconnectedConnection { get; } = new List<UnconnectedInformation>();

	public void CalculateInfo(List<SectorName> sectors)
	{
		BackUpFields();
		Subtitles.Clear();
		UnconnectedConnection.Clear();
		foreach (FieldInformation item in fields.ToList())
		{
			if (sectors.Contains(item.fieldID.SectorName))
			{
				item.SetFixed(value: false);
			}
			else
			{
				fields.Remove(item);
			}
		}
		FieldInformation fieldInformation = fields.FirstOrDefault();
		if (fieldInformation != null)
		{
			fieldInformation.SetFixed(value: true);
			AddSubtitleInfo(fieldInformation);
			targetFields.Add(fieldInformation);
			LoopFieldInformation(fields.Count);
		}
		fields.DebugGeneralLog("지도 정보 계산이 끝났습니다! [Field 고정 정보]", (FieldInformation field) => (!field.IsFixed) ? $"{field}는 아직 고정되지 않았습니다!" : $"{field} : {field.Position + totalOffset}");
		Subtitles.DebugGeneralLog("[Subtitles 정보]", delegate(KeyValuePair<FieldSubtitle, Vector4> kvp)
		{
			Vector4 value = kvp.Value;
			float num = value.z - value.x;
			float num2 = value.w - value.y;
			return $"{kvp.Key} 위치 : ({(value.x + value.w) / 2f}, {(value.y + value.z) / 2f}), 크기 : ({num}, {num2})";
		});
	}

	private FieldInformation GetField(FieldID id)
	{
		return fields.FirstOrDefault((FieldInformation field) => field.fieldID == id);
	}

	private void LoopFieldInformation(int totalNum)
	{
		int num = 0;
		int num2 = totalNum * 10;
		while (targetFields.Count > 0)
		{
			foreach (FieldInformation item in targetFields.ToList())
			{
				SetFieldInformation(item);
				targetFields.Remove(item);
			}
			num++;
			if (num > num2 || targetFields.Count > num2)
			{
				DebugLog.GeneralLogError($"지도 정보 계산 횟수가 씬 갯수의 10배인 {num2}개를 넘어갑니다! 무한루프가 발생한 것 같으므로 일단 중단합니다.");
				throw new Exception();
			}
		}
	}

	private void SetFieldInformation(FieldInformation field)
	{
		if (!field.IsFixed)
		{
			DebugLog.GeneralLogWarning($"기준 필드({field})가 고정되지 않았습니다! 무시하고 진행합니다.");
			return;
		}
		foreach (FieldConnectionInformation connection in field.connections)
		{
			if (!connection.connect)
			{
				UnconnectedConnection.Add(new UnconnectedInformation(field.fieldID, connection.targetField, connection.entranceRawPosition));
				continue;
			}
			FieldInformation field2 = GetField(connection.targetField);
			if (field2 == null)
			{
				DebugLog.GeneralLogWarning($"{connection.targetField}의 정보가 등록되지 않았습니다! 이번만 무시하고 계속 진행합니다. (현재 : {field.fieldID})");
			}
			else if (!field2.IsFixed)
			{
				if (field2.HasConnectionWith(field.fieldID))
				{
					FixPosition(field, field2);
					continue;
				}
				DebugLog.GeneralLogError($"{field}에서 {field2}로 향하는 입구는 있지만, {field2}에서 {field}로 향하는 입구가 없습니다!");
			}
		}
		FixSpecialField(field);
	}

	private void FixPosition(FieldInformation origin, FieldInformation target)
	{
		if (!origin.IsFixed || target.IsFixed)
		{
			DebugLog.GeneralLogWarning($"({origin}, {target})기준 필드가 고정되지 않았거나, 타겟 필드가 고정되어 있습니다!");
			return;
		}
		FieldConnectionInformation connection = origin.GetConnection(target.fieldID);
		FieldConnectionInformation connection2 = target.GetConnection(origin.fieldID);
		try
		{
			if (connection.direction.Opposite() != connection2.direction)
			{
				DebugLog.GeneralLogError($"({origin}, {target})서로에게 향하는 입구가 반대 방향이 아닙니다!");
				return;
			}
		}
		catch (ArgumentOutOfRangeException ex)
		{
			DebugLog.GeneralLogError($"{origin}에서 {target}으로 가는 입구의 방향이 {ex.ActualValue}입니다!");
			throw;
		}
		Vector2 pos = default(Vector2);
		switch (connection.direction)
		{
		case Direction.Right:
		case Direction.Left:
		{
			float num4 = origin.Position.y - (float)origin.Size.y / 2f + (float)connection.entrancePosition;
			pos.y = num4 - (float)connection2.entrancePosition + (float)target.Size.y / 2f;
			float num5 = (float)(origin.Size.x + target.Size.x) / 2f;
			int num6 = ((connection.direction != Direction.Right) ? 1 : (-1));
			pos.x = origin.Position.x + num5 * (float)num6;
			break;
		}
		case Direction.Up:
		case Direction.Down:
		{
			float num = origin.Position.x - (float)origin.Size.x / 2f + (float)connection.entrancePosition;
			pos.x = num - (float)connection2.entrancePosition + (float)target.Size.x / 2f;
			float num2 = (float)(origin.Size.y + target.Size.y) / 2f;
			int num3 = ((connection.direction != Direction.Up) ? 1 : (-1));
			pos.y = origin.Position.y + num2 * (float)num3;
			break;
		}
		default:
			DebugLog.GeneralLogError($"{origin}의 {target}쪽 입구가 상하좌우 4방향이 아닙니다!({connection.direction})");
			pos = new Vector2(-999f, -999f);
			break;
		}
		FixTarget(target, pos);
	}

	private void FixTarget(FieldInformation target, Vector2 pos, bool shouldFix = true)
	{
		target.SetPosition(pos);
		target.SetFixed(shouldFix);
		AddSubtitleInfo(target);
		if (shouldFix)
		{
			targetFields.Add(target);
		}
	}

	private void FixSpecialField(FieldInformation origin)
	{
		FieldID fieldID = origin.fieldID;
		switch (fieldID.SectorName)
		{
		case SectorName.AshHills:
			if (fieldID.SceneIndex == 26)
			{
				Set(SectorName.Library, 3, (FieldInformation target) => origin.Position.x - (float)origin.Size.x / 2f - 1296f - (float)target.Size.x / 2f, (FieldInformation target) => origin.Position.y + (float)origin.Size.y / 2f + 162f + (float)target.Size.y / 2f);
				Set(SectorName.AshHills, 27, (FieldInformation target) => origin.Position.x - (float)origin.Size.x / 2f - (float)target.Size.x / 2f, (FieldInformation target) => origin.Position.y + (float)origin.Size.y / 2f - (float)target.Size.y / 2f);
			}
			break;
		case SectorName.Library:
			if (fieldID.SceneIndex == 4)
			{
				Set(SectorName.WorldsEnd, 1, (FieldInformation target) => origin.Position.x + (float)origin.Size.x / 2f + (float)target.Size.x / 2f - 50f, (FieldInformation target) => origin.Position.y + (float)origin.Size.y / 2f + 100f + (float)target.Size.y / 2f);
			}
			break;
		case SectorName.WorldsEnd:
			if (fieldID.SceneIndex == 2)
			{
				Set(SectorName.WorldsEnd, 3, (FieldInformation target) => origin.Position.x + (float)origin.Size.x / 2f + (float)target.Size.x / 2f - 50f, (FieldInformation target) => origin.Position.y + (float)origin.Size.y / 2f + 100f + (float)target.Size.y / 2f);
			}
			break;
		}
		void Set(SectorName sectorName, int sceneIndex, Func<FieldInformation, float> xFormula, Func<FieldInformation, float> yFormula)
		{
			FieldInformation fieldInformation = fields.FirstOrDefault((FieldInformation field) => field.fieldID.Equals(sectorName, sceneIndex));
			if (fieldInformation != null)
			{
				Vector2 pos = default(Vector2);
				pos.x = xFormula(fieldInformation);
				pos.y = yFormula(fieldInformation);
				bool shouldFix = !fieldInformation.fieldID.Equals(SectorName.WorldsEnd, 3);
				FixTarget(fieldInformation, pos, shouldFix);
			}
		}
	}

	private void AddSubtitleInfo(FieldInformation field)
	{
		FieldSubtitle subtitle = field.fieldID.GetSubtitle();
		Vector2 vector = field.Position - field.Size / 2;
		Vector2 vector2 = field.Position + field.Size / 2;
		float x = vector.x;
		float y = vector.y;
		float x2 = vector2.x;
		float y2 = vector2.y;
		if (Subtitles.TryGetValue(subtitle, out var value))
		{
			Vector4 value2 = new Vector4(Mathf.Min(x, value.x), Mathf.Min(y, value.y), Mathf.Max(x2, value.z), Mathf.Max(y2, value.w));
			Subtitles[subtitle] = value2;
		}
		else
		{
			Vector4 value3 = new Vector4(x, y, x2, y2);
			Subtitles.Add(subtitle, value3);
		}
	}

	private void BackUpFields()
	{
		fieldsBackUp.Clear();
		foreach (FieldInformation field in fields)
		{
			fieldsBackUp.Add(field);
		}
	}

	public void RestoreFields()
	{
		foreach (FieldInformation item in fieldsBackUp.Where((FieldInformation f) => !fields.Contains(f)))
		{
			fields.Add(item);
		}
	}

	public MapVariantInformation GetMapVariant(FieldInformation field)
	{
		return variants.FirstOrDefault((MapVariantInformation v) => v.targetField.Equals(field.fieldID));
	}

	private void OnValidate()
	{
		foreach (MapVariantInformation variant in variants)
		{
			variant.SetName();
		}
	}
}
