using System;
using System.Collections.Generic;
using Library;
using SPR.Save;

namespace SPR;

public abstract class Trigger
{
	private enum Operator
	{
		And,
		Or
	}

	protected abstract class Node
	{
		private readonly Box box;

		protected Box CurrentBox;

		public bool Result => box.Result;

		public Node()
		{
			box = new Box(null);
			CurrentBox = box;
		}

		public abstract void AddID(string id);

		public void OpenInnerTalkNode()
		{
			Box box = new Box(CurrentBox);
			CurrentBox.Add(box);
			CurrentBox = box;
		}

		public void CloseInnerTalkNode()
		{
			CurrentBox = CurrentBox.ParentBox;
		}

		public void AddOr()
		{
			if (CurrentBox.CheckLastElementClassIsSameAs(typeof(OperatorElement)))
			{
				throw new FormatException("논리 연산자 다음에 바로 논리 연산자가 올 수 없습니다.");
			}
			CurrentBox.Add(new OperatorElement(Operator.Or));
		}

		public void AddAnd()
		{
			if (CurrentBox.CheckLastElementClassIsSameAs(typeof(OperatorElement)))
			{
				throw new FormatException("논리 연산자 다음에 바로 논리 연산자가 올 수 없습니다.");
			}
			CurrentBox.Add(new OperatorElement(Operator.And));
		}
	}

	protected class TalkNode : Node
	{
		public override void AddID(string scenarioID)
		{
			if (CurrentBox.CheckLastElementClassIsSameAs(typeof(ScenarioElement)))
			{
				throw new FormatException("시나리오ID 다음에 바로 시나리오ID가 올 수 없습니다. 논리 연산자나 괄호를 빠뜨리지 않았나요?");
			}
			CurrentBox.Add(new ScenarioElement(scenarioID));
		}
	}

	protected class VisitAreaNode : Node
	{
		public override void AddID(string fieldID)
		{
			if (CurrentBox.CheckLastElementClassIsSameAs(typeof(VisitAreaElement)))
			{
				throw new FormatException("시나리오ID 다음에 바로 시나리오ID가 올 수 없습니다. 논리 연산자나 괄호를 빠뜨리지 않았나요?");
			}
			CurrentBox.Add(new VisitAreaElement(fieldID));
		}
	}

	protected class TriggerNode : Node
	{
		public override void AddID(string fieldID)
		{
			if (CurrentBox.CheckLastElementClassIsSameAs(typeof(TriggerElement)))
			{
				throw new FormatException("시나리오ID 다음에 바로 시나리오ID가 올 수 없습니다. 논리 연산자나 괄호를 빠뜨리지 않았나요?");
			}
			CurrentBox.Add(new TriggerElement(fieldID));
		}
	}

	protected class WatchedCutsceneNode : Node
	{
		public override void AddID(string scenarioID)
		{
			if (CurrentBox.CheckLastElementClassIsSameAs(typeof(CutsceneElement)))
			{
				throw new FormatException("시나리오ID 다음에 바로 시나리오ID가 올 수 없습니다. 논리 연산자나 괄호를 빠뜨리지 않았나요?");
			}
			CurrentBox.Add(new CutsceneElement(scenarioID));
		}
	}

	protected class Box : Element
	{
		private readonly Box parentBox;

		private readonly List<Element> elements;

		public Box ParentBox => parentBox;

		public override bool Result => CheckTrigger();

		public Box(Box parent)
		{
			parentBox = parent;
			elements = new List<Element>();
		}

		public bool CheckLastElementClassIsSameAs(Type targetType)
		{
			int count = elements.Count;
			if (count == 0)
			{
				return false;
			}
			return elements[count - 1].GetType() == targetType;
		}

		public void Add(Element element)
		{
			elements.Add(element);
		}

		private bool CheckTrigger()
		{
			bool flag = elements[0].Result;
			for (int i = 1; i < elements.Count; i++)
			{
				if (elements[i] is OperatorElement operatorElement)
				{
					flag = operatorElement.Evaluate(flag, elements[i + 1].Result);
				}
			}
			return flag;
		}
	}

	protected abstract class Element
	{
		public abstract bool Result { get; }
	}

	private class OperatorElement : Element
	{
		private readonly Operator _operator;

		public override bool Result
		{
			get
			{
				throw new ArgumentException("Operator는 결과를 직접 뱉지 않습니다.");
			}
		}

		public OperatorElement(Operator type)
		{
			_operator = type;
		}

		public bool Evaluate(bool result1, bool result2)
		{
			return _operator switch
			{
				Operator.And => result1 && result2, 
				Operator.Or => result1 || result2, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
	}

	private class ScenarioElement : Element
	{
		private readonly string scenarioID;

		private readonly bool shouldWatch;

		public override bool Result => SaveData.TalkData.GetTalkCount(scenarioID) > 0 == shouldWatch;

		public ScenarioElement(string scenarioID)
		{
			if (scenarioID[0] == '!')
			{
				shouldWatch = false;
				this.scenarioID = scenarioID.Remove(0, 1);
			}
			else
			{
				shouldWatch = true;
				this.scenarioID = scenarioID;
			}
		}
	}

	private class VisitAreaElement : Element
	{
		private readonly FieldID fieldID;

		private readonly SectorName sectorName;

		private readonly bool shouldVisit;

		public override bool Result
		{
			get
			{
				if (sectorName != SectorName.None)
				{
					return SaveData.WorldData.GetVisitedFieldCount(sectorName) > 0 == shouldVisit;
				}
				return SaveData.WorldData.IsVisitedField(fieldID) == shouldVisit;
			}
		}

		public VisitAreaElement(string value)
		{
			if (value[0] == '!')
			{
				shouldVisit = false;
				string text = value.Substring(1, value.Length - 1);
				if (Enum.TryParse<SectorName>(text, out var result))
				{
					sectorName = result;
				}
				else
				{
					fieldID = FieldID.ConvertToFieldID(text);
				}
			}
			else
			{
				shouldVisit = true;
				if (Enum.TryParse<SectorName>(value, out var result2))
				{
					sectorName = result2;
				}
				else
				{
					fieldID = FieldID.ConvertToFieldID(value);
				}
			}
		}
	}

	private class TriggerElement : Element
	{
		private readonly string triggerID;

		private readonly bool shouldTrue;

		public override bool Result => CheckTrigger(triggerID) == shouldTrue;

		public TriggerElement(string triggerID)
		{
			if (triggerID[0] == '!')
			{
				shouldTrue = false;
				this.triggerID = triggerID.Substring(1, triggerID.Length - 1);
			}
			else
			{
				shouldTrue = true;
				this.triggerID = triggerID;
			}
		}
	}

	private class CutsceneElement : Element
	{
		private readonly string cutsceneID;

		private readonly bool shouldWatch;

		public override bool Result
		{
			get
			{
				string[] array = cutsceneID.Split('_');
				if (array.Length > 1 && array[1].TryParseInt(out var result))
				{
					return SaveData.WanderingData.IsWatchedCutscene(array[0], result) == shouldWatch;
				}
				return SaveData.WanderingData.IsWatchedCutscene(cutsceneID) == shouldWatch;
			}
		}

		public CutsceneElement(string cutsceneID)
		{
			if (cutsceneID[0] == '!')
			{
				shouldWatch = false;
				this.cutsceneID = cutsceneID.Remove(0, 1);
			}
			else
			{
				shouldWatch = true;
				this.cutsceneID = cutsceneID;
			}
		}
	}

	protected Node BaseNode;

	public const string None = "None";

	public const string ChasaRetry = "Trigger_ChasaRetry";

	public const string ShouldJimaDisappear = "Trigger_ShouldJimaDisappear";

	public const string VeritaInfected = "Trigger_VeritaInfected";

	public const string CardamineWholeSoldOut = "Trigger_CardamineWholeSoldOut";

	public const string VeritaQuestProgress = "Trigger_VeritaQuestProgress";

	public const string VeritaQuestComplete = "Trigger_VeritaQuestComplete";

	public const string BaranShouldGuideMyoso = "Trigger_BaranShouldGuideMyoso";

	public const string MyosoHelpFightBaran = "Trigger_MyosoHelpFightBaran";

	public const string MyosoReunionedWithBaran = "Trigger_MyosoReunionedWithBaran";

	public const string InformationMyoso = "Trigger_InformationMyoso";

	public const string IsTrueEnding = "Trigger_IsTrueEnding";

	public const string EnterLibraryWithPortal = "Trigger_EnterLibraryWithPortal";

	public const string DarkChasaShouldResonance = "Trigger_DarkChasaShouldResonance";

	public const string PwoongShouldFear = "Trigger_PwoongShouldFear";

	public const string InviteLunya = "Trigger_InviteLunya";

	public const string ShouldTeleportToLibraryFront = "Trigger_ShouldTeleportToLibraryFront";

	public const string LibraryDestroyed = "Trigger_LibraryDestroyed";

	public const string Library01Haechi = "Trigger_NPC_Library_01_Haechi";

	protected static SaveData SaveData => Game.Instance.SaveData;

	public abstract bool CheckTrigger();

	public static bool IsTriggerExist(string id)
	{
		return DataManager.Instance.GetTriggerData(id) != null;
	}

	public static bool CheckTrigger(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return true;
		}
		bool flag = id[0] == '!';
		if (flag)
		{
			string text = id;
			id = text.Substring(1, text.Length - 1);
		}
		TriggerData triggerData = DataManager.Instance.GetTriggerData(id);
		if (triggerData == null)
		{
			DebugLog.DataManagerLogError("ID가 " + id + "인 Trigger를 찾을 수 없습니다!");
			return false;
		}
		bool flag2 = CheckTrigger(triggerData);
		if (!flag)
		{
			return flag2;
		}
		return !flag2;
	}

	public static bool CheckTrigger(TriggerData data)
	{
		bool num = Parse(data.Type1, data.Target1, data.Value1).CheckTrigger();
		bool flag = Parse(data.Type2, data.Target2, data.Value2).CheckTrigger();
		bool flag2 = Parse(data.Type3, data.Target3, data.Value3).CheckTrigger();
		return num && flag && flag2;
	}

	private static Trigger Parse(TriggerType type, string target, string value)
	{
		return type switch
		{
			TriggerType.None => new NoneTrigger(), 
			TriggerType.Never => new NeverTrigger(), 
			TriggerType.Talk => new TalkTrigger(target, value), 
			TriggerType.InteractCount => new InteractCountTrigger(target, value), 
			TriggerType.InteractField => new InteractFieldTrigger(target, value), 
			TriggerType.VisitArea => new VisitAreaTrigger(target, value), 
			TriggerType.CurrentArea => new CurrentAreaTrigger(target, value), 
			TriggerType.Item => new ItemTrigger(target, value), 
			TriggerType.FightCount => new FightCountTrigger(target, value), 
			TriggerType.Trigger => new TriggerTrigger(target, value), 
			TriggerType.Trait => new TraitTrigger(target, value), 
			TriggerType.Store => new StoreTrigger(target, value), 
			TriggerType.Sales => new SalesTrigger(target, value), 
			TriggerType.Cutscene => new WatchedCutsceneTrigger(target, value), 
			TriggerType.SameAnimation => new SameAnimation(target, value), 
			TriggerType.IsRested => new IsRested(target, value), 
			TriggerType.IsBossCleared => new IsBossCleared(target, value), 
			TriggerType.Object => new InteractableObjectTrigger(target, value), 
			TriggerType.Donation => new DonationTrigger(target, value), 
			_ => throw new ArgumentException(), 
		};
	}

	protected void ParseValue(string value)
	{
		if (!value.IsValidBracket())
		{
			throw new FormatException(value + "의 괄호 배치가 올바르지 않습니다.");
		}
		value = value.Replace(" ", "");
		int num = 0;
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] == '(' || value[i] == ')' || value[i] == '|' || value[i] == '&')
			{
				if (num < i)
				{
					BaseNode.AddID(value.Substring(num, i - num));
				}
				switch (value[i])
				{
				case '(':
					BaseNode.OpenInnerTalkNode();
					break;
				case ')':
					BaseNode.CloseInnerTalkNode();
					break;
				case '|':
					BaseNode.AddOr();
					break;
				case '&':
					BaseNode.AddAnd();
					break;
				}
				num = i + 1;
			}
		}
		if (num < value.Length)
		{
			BaseNode.AddID(value.Substring(num, value.Length - num));
		}
	}
}
