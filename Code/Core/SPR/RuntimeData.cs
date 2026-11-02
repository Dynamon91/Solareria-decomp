using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR;

public class RuntimeData
{
	private class TipConditionChecker
	{
		public bool CanAction;

		public bool HadAction;

		public bool ShowTip
		{
			get
			{
				if (CanAction)
				{
					return !HadAction;
				}
				return false;
			}
		}

		public void Clear()
		{
			CanAction = false;
			HadAction = false;
		}
	}

	private readonly Dictionary<FieldID, List<Vector2>> dieCreatureDict = new Dictionary<FieldID, List<Vector2>>();

	private readonly Dictionary<FieldID, List<Vector2>> brokenEnvironmentDict = new Dictionary<FieldID, List<Vector2>>();

	private readonly List<ActionName> affectedStatusEffect = new List<ActionName>();

	private readonly TipConditionChecker hyperPyronActionChecker = new TipConditionChecker();

	private readonly TipConditionChecker hyperBurstChecker = new TipConditionChecker();

	private bool isBGMLessEvent;

	public bool IsBGMLessEvent => isBGMLessEvent;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	public void ClearRuntimeData()
	{
		ClearRuntimeDictionaries();
		affectedStatusEffect.Clear();
		hyperPyronActionChecker.Clear();
		hyperBurstChecker.Clear();
		isBGMLessEvent = false;
	}

	public void ClearRuntimeDictionaries()
	{
		ClearDieCreatureDict();
		ClearBrokenEnvironmentDict();
		hyperPyronActionChecker.Clear();
		hyperBurstChecker.Clear();
	}

	public void AddDieCreature(FieldID fieldID, Vector2 spawnPosition)
	{
		if (!dieCreatureDict.ContainsKey(fieldID))
		{
			dieCreatureDict.Add(fieldID, new List<Vector2>());
		}
		if (!dieCreatureDict[fieldID].Contains(spawnPosition))
		{
			dieCreatureDict[fieldID].Add(spawnPosition);
		}
	}

	public bool IsAlreadyDieCreature(FieldID fieldID, Vector2 spawnPosition)
	{
		if (dieCreatureDict.ContainsKey(fieldID))
		{
			return dieCreatureDict[fieldID].Contains(spawnPosition);
		}
		return false;
	}

	public void ClearDieCreatureDict()
	{
		dieCreatureDict.Clear();
	}

	public void AddBrokenEnvironment(FieldID fieldID, Vector2 spawnPosition)
	{
		if (!brokenEnvironmentDict.ContainsKey(fieldID))
		{
			brokenEnvironmentDict.Add(fieldID, new List<Vector2>());
		}
		if (!brokenEnvironmentDict[fieldID].Contains(spawnPosition))
		{
			brokenEnvironmentDict[fieldID].Add(spawnPosition);
			DebugLog.GeneralLog($"AddBroken({fieldID}),({spawnPosition})");
		}
	}

	public bool IsAlreadyBrokenEnvironment(FieldID fieldID, Vector2 spawnPosition)
	{
		bool flag = brokenEnvironmentDict.ContainsKey(fieldID) && brokenEnvironmentDict[fieldID].Contains(spawnPosition);
		DebugLog.GeneralLog($"IsAlreadyBroken({fieldID}),({spawnPosition}) : {flag}");
		return flag;
	}

	public void ClearBrokenEnvironmentDict()
	{
		brokenEnvironmentDict.Clear();
	}

	public void AddHitStatusEffect(StatusEffect se)
	{
		ActionName actionName = ((se is ElectricStatusEffect) ? ActionName.ShockStatusEffect : ((se is WetStatusEffect) ? ActionName.WetStatusEffect : ((se is SporeStatusEffect) ? ActionName.SporeStatusEffect : ActionName.None)));
		ActionName actionName2 = actionName;
		if (actionName2 == ActionName.None)
		{
			DebugLog.GeneralLog("튜토리얼이 정의되지 않은 원소 효과(se)가 튜토리얼을 호출하려 합니다!");
		}
		else
		{
			AddStatusEffect(actionName2);
		}
	}

	public void AddDarknessStatusEffect()
	{
		AddStatusEffect(ActionName.DarknessEffect);
	}

	private void AddStatusEffect(ActionName actionName)
	{
		if (WanderingData.ShouldShowTutorial(actionName) && !affectedStatusEffect.Contains(actionName))
		{
			affectedStatusEffect.Add(actionName);
		}
	}

	public async UniTask ShowStatusEffectTutorial()
	{
		foreach (ActionName item in affectedStatusEffect.Where((ActionName actionName) => WanderingData.ShouldShowTutorial(actionName)))
		{
			TutorialCanvas.SetData(item);
			InputManager.StackControlTarget(TutorialCanvas).Forget();
			await UniTask.WaitUntil(() => InputManager.IsCurrentControlTarget(Game.Instance.Player));
		}
		affectedStatusEffect.Clear();
	}

	public void SetBGMLessEvent(bool value)
	{
		isBGMLessEvent = value;
	}

	public int GetAvailableStatusEffectTutorialCount()
	{
		return affectedStatusEffect.Count((ActionName actionName) => WanderingData.ShouldShowTutorial(actionName));
	}

	public void AddCanHyperPyronAction()
	{
		hyperPyronActionChecker.CanAction = PlayerHeat.CanUseSkill(HeatSkillType.HyperPyronAction);
	}

	public void AddCanHyperBurst()
	{
		hyperBurstChecker.CanAction = PlayerHeat.CanUseSkill(HeatSkillType.HyperBurst);
	}

	public void AddHadHyperPyronAction()
	{
		hyperPyronActionChecker.HadAction = true;
	}

	public void AddHadHyperBurst()
	{
		hyperBurstChecker.HadAction = true;
	}

	public bool GetTipCondition(HeatSkillType type)
	{
		return type switch
		{
			HeatSkillType.HyperPyronAction => hyperPyronActionChecker.ShowTip, 
			HeatSkillType.HyperBurst => hyperBurstChecker.ShowTip, 
			_ => false, 
		};
	}
}
