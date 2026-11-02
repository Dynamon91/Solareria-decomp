using System;
using Library;

namespace SPR;

public class InteractCountTrigger : Trigger
{
	private enum Operator
	{
		Same,
		More,
		Less
	}

	private readonly ActorName npcName;

	private readonly Operator op;

	private readonly int count;

	public InteractCountTrigger(string target, string value)
	{
		if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(value))
		{
			throw new FormatException("InteractCount Trigger에서 Target과 Value는 반드시 존재해야합니다. 현재 값 : " + target + ", " + value);
		}
		npcName = target.ToEnum<ActorName>();
		if (value.TryParseInt(out var result))
		{
			op = Operator.Same;
		}
		else
		{
			if (!value.Substring(1, value.Length - 1).TryParseInt(out result))
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
		count = result;
	}

	public override bool CheckTrigger()
	{
		int interactCount = Trigger.SaveData.TalkData.GetInteractCount(npcName);
		return op switch
		{
			Operator.Same => interactCount == count, 
			Operator.More => interactCount > count, 
			Operator.Less => interactCount < count, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
