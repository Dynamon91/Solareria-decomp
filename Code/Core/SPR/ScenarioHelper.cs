using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace SPR;

public static class ScenarioHelper
{
	public static bool IsScenarioExist(string id)
	{
		for (int i = 1; i < 100; i++)
		{
			if (DataManager.Instance.GetScenarioData($"{id}_{i}") != null)
			{
				return true;
			}
		}
		return false;
	}

	public static Scenario GetScenario(string id)
	{
		Scenario scenario = new Scenario(id);
		for (int i = 1; i < 100; i++)
		{
			ScenarioData scenarioData = DataManager.Instance.GetScenarioData($"{id}_{i}");
			if (scenarioData == null)
			{
				break;
			}
			scenario.AddScript(scenarioData.ConvertToScript());
		}
		return scenario;
	}

	public static List<Scenario> GetScenarios(string key)
	{
		List<ScenarioData> list = DataManager.Instance.SearchAllScenarioDataContains(key);
		Dictionary<string, List<ScenarioData>> dictionary = new Dictionary<string, List<ScenarioData>>();
		foreach (ScenarioData item in list)
		{
			if (!dictionary.ContainsKey(item.GroupID))
			{
				dictionary.Add(item.GroupID, new List<ScenarioData>());
			}
			dictionary[item.GroupID].Add(item);
		}
		List<Scenario> list2 = new List<Scenario>();
		foreach (KeyValuePair<string, List<ScenarioData>> item2 in dictionary)
		{
			Scenario scenario = new Scenario(item2.Key);
			foreach (ScenarioData item3 in item2.Value.OrderBy((ScenarioData d) => d.Index))
			{
				scenario.AddScript(item3.ConvertToScript());
			}
			list2.Add(scenario);
		}
		return list2;
	}

	public static List<string> GetScenarioKeys(string key)
	{
		int result;
		return (from id in (from d in DataManager.Instance.SearchAllScenarioDataContains(key)
				select d.GroupID).Distinct()
			where string.IsNullOrWhiteSpace(id.Replace(key, "")) || id.Replace(key + "_", "").TryParseInt(out result)
			orderby (!string.IsNullOrWhiteSpace(id.Replace(key, ""))) ? id.Replace(key + "_", "").ToInt() : 0
			select id).ToList();
	}

	private static Script ConvertToScript(this ScenarioData data)
	{
		string[] values = new string[12]
		{
			data.Actor, data.Option, data.VoiceKey, data.KO, data.EN, data.JA, data.ZH_HANT, data.ZH_HANS, data.DE, data.ES,
			data.PT, data.FR
		};
		return data.ScriptType switch
		{
			ScriptType.Talk => new TalkScript(values), 
			ScriptType.Choice => new ChoiceScript(values), 
			ScriptType.Choice_NotiForced => new ChoiceNotiForced(values), 
			ScriptType.Choice_NotiNewChat => new ChoiceNotiNewChat(values), 
			ScriptType.Choice_DoNotRemove => new ChoiceDoNotRemove(values), 
			ScriptType.Anim => new AnimationScript(values), 
			ScriptType.Move => new MoveScript(values), 
			ScriptType.Special => new SpecialScript(values), 
			ScriptType.Cutscene => new CutsceneScript(values), 
			ScriptType.Jump => new JumpScript(values), 
			ScriptType.Chat => new ChatScript(values), 
			ScriptType.Delay => new DelayScript(values), 
			ScriptType.Item => new ItemScript(values), 
			ScriptType.Sound => new SoundScript(values), 
			ScriptType.ScenarioInfo => new ScenarioInfoScript(values), 
			_ => throw new NotImplementedException(), 
		};
	}
}
