using System;
using System.Linq;
using Library;
using SPR.Save;

namespace SPR;

public class InteractFieldTrigger : Trigger
{
	private enum Operator
	{
		Same,
		More,
		Less,
		FirstHere,
		WasFirstAt,
		ContainsOther,
		WasLastAtHere,
		FirstRest,
		Sector
	}

	private readonly ActorName npcName;

	private readonly Operator op;

	private readonly int count;

	private readonly FieldID targetField;

	public InteractFieldTrigger(string target, string value)
	{
		if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(value))
		{
			throw new FormatException("InteractCount Trigger에서 Target과 Value는 반드시 존재해야합니다. 현재 값 : " + target + ", " + value);
		}
		npcName = target.ToEnum<ActorName>();
		string[] array = value.Split(',');
		if (array.Length == 2 && !array[0].TryParseInt(out var result) && Enum.TryParse<Operator>(array[0], out op))
		{
			if (op == Operator.Sector)
			{
				array[1].TryParseInt(out count);
			}
			else
			{
				targetField = FieldID.ConvertToFieldID(array[1]);
			}
			return;
		}
		if (!array[0].TryParseInt(out result) && Enum.TryParse<Operator>(value, out op))
		{
			count = 0;
			return;
		}
		if (value.TryParseInt(out var result2))
		{
			op = Operator.Same;
		}
		else
		{
			if (!value.Substring(1, value.Length - 1).TryParseInt(out result2))
			{
				throw new FormatException("InteractCount Trigger에서 Value가 형식에 맞지 않습니다. 현재 값 : " + value);
			}
			op = value[0] switch
			{
				'>' => Operator.More, 
				'<' => Operator.Less, 
				_ => throw new FormatException("InteractCount Trigger에서 Value가 형식에 맞지 않습니다. 현재 값 : " + value), 
			};
		}
		count = result2;
	}

	public override bool CheckTrigger()
	{
		TalkData.InteractCountData interactData = Trigger.SaveData.TalkData.GetInteractData(npcName);
		int num = interactData?.UniqueCount ?? 0;
		return op switch
		{
			Operator.Same => num == count, 
			Operator.More => num > count, 
			Operator.Less => num < count, 
			Operator.FirstHere => interactData == null || !interactData.Fields.Contains(Game.Instance.CurrentFieldID), 
			Operator.WasFirstAt => (interactData != null && interactData.Fields[0].Equals(targetField)) || (interactData == null && Game.Instance.CurrentFieldID.Equals(targetField)), 
			Operator.ContainsOther => interactData?.Fields.Any((FieldID id) => id != Game.Instance.CurrentFieldID) ?? false, 
			Operator.WasLastAtHere => interactData == null || interactData.Count == 0 || interactData.Fields.Last().Equals(Game.Instance.CurrentFieldID), 
			Operator.FirstRest => Trigger.SaveData.WorldData.IsFirstRestNpc(npcName, targetField), 
			Operator.Sector => (interactData == null) ? (count == 0) : (interactData.Sectors.Count == count), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
