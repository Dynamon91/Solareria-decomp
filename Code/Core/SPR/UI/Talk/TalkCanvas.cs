using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI.Choice;
using SPR.UI.Field;
using SPR.UI.Map;
using SPR.UI.Upgrade;
using UnityEngine;

namespace SPR.UI.Talk;

public class TalkCanvas : AutoScaledCanvas, IControllable
{
	[SerializeField]
	private TalkBoard talkBoard;

	[SerializeField]
	private ChoiceBoard choiceBoard;

	[SerializeField]
	private NarrationBoard specialTalkBoard;

	private Stack<Scenario> scenarioStack = new Stack<Scenario>();

	private readonly Dictionary<string, Dictionary<string, bool>> watchedChatDictionary = new Dictionary<string, Dictionary<string, bool>>();

	private Action onFinished;

	private ScenarioData narrationData;

	private ChoiceData lastChoice;

	public bool IsNarrating
	{
		get
		{
			if (narrationData == null)
			{
				return specialTalkBoard.IsActive;
			}
			return true;
		}
	}

	private static InputManager InputManager => Game.Instance.InputManager;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static TalkData TalkData => Game.Instance.SaveData.TalkData;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsTalking { get; set; }

	public bool IsValid => true;

	public void Initialize()
	{
		base.gameObject.SetActive(value: true);
		talkBoard.Initialize();
		choiceBoard.Initialize();
		specialTalkBoard.Initialize();
		scenarioStack = new Stack<Scenario>();
	}

	public void OnSceneLoaded()
	{
		ResetWatchedChat();
	}

	public void StartTalk(Scenario scenario, Action finishCallback = null)
	{
		InputManager.StackControlTarget(this).Forget();
		onFinished = (Action)Delegate.Combine(onFinished, finishCallback);
		StackScenario(scenario);
		RunNextScript();
	}

	public void StartTalkWithoutControl(Scenario scenario, Action finishCallback = null)
	{
		onFinished = null;
		onFinished = (Action)Delegate.Combine(onFinished, finishCallback);
		StackScenario(scenario);
		RunNextScript();
		talkBoard.Initialize();
	}

	public async UniTask ActivateTalk(string speakerName, string speakerText, float autoDuration, string voiceKey)
	{
		base.gameObject.SetActive(value: true);
		IsTalking = true;
		await talkBoard.ActivateTalkAsync(speakerName, speakerText, autoDuration, voiceKey);
		if (autoDuration > 0f)
		{
			await UniTaskExtension.WaitForSeconds(autoDuration);
			RunNextScript();
		}
	}

	public async UniTask DeactivateTalkBoardAsync()
	{
		await talkBoard.DeactivateTalkAsync();
	}

	public void RunNextScript()
	{
		Scenario scenario;
		while (true)
		{
			if (scenarioStack.Count == 0)
			{
				if (!InputManager.ContainsInControlStack(this))
				{
					Deactivate().Forget();
					InvokeFinishCallback();
				}
				else
				{
					InputManager.RemoveControlTarget(this).GetAwaiter().OnCompleted(InvokeFinishCallback);
				}
				return;
			}
			scenario = scenarioStack.Peek();
			if (!scenario.IsLastLine)
			{
				break;
			}
			scenarioStack.Pop();
		}
		scenario.Run(this);
	}

	public void InvokeFinishCallback()
	{
		onFinished?.Invoke();
		onFinished = null;
	}

	public void StackScenario(Scenario scenario)
	{
		if (scenarioStack == null)
		{
			scenarioStack = new Stack<Scenario>();
		}
		scenario.Reset();
		scenarioStack.Push(scenario);
		TalkData.AddTalk(scenario.GroupID);
	}

	public void AddChoiceOption(string chosenScenarioID, string text, NotiCheckType checkType, bool removeInstantly)
	{
		Scenario chosenScenario = ScenarioHelper.GetScenario(chosenScenarioID);
		bool showNotiIcon = checkType == NotiCheckType.NotiForced;
		if (checkType == NotiCheckType.NotiNewChat)
		{
			showNotiIcon = GetValidChats(chosenScenario).Count((ChatScript cs) => cs.IsHasTrigger && !cs.IsAlreadyWatched) > 0;
		}
		ChoiceData choiceData = new ChoiceData(text, showNotiIcon, delegate
		{
			if (chosenScenario != null && chosenScenario.HasValue)
			{
				StackScenario(chosenScenario);
			}
			if (removeInstantly)
			{
				InputManager.RemoveControlTarget(choiceBoard).GetAwaiter().OnCompleted(RunNextScript);
			}
			else
			{
				RunNextScript();
			}
		}, chosenScenarioID.IsNullOrEmpty());
		choiceBoard.AddChoice(choiceData);
	}

	public void HandleNextChoice()
	{
		if (scenarioStack.Peek().IsNextScriptClassSameAs<ChoiceScript>())
		{
			RunNextScript();
		}
		else
		{
			InputManager.StackControlTarget(choiceBoard).Forget();
		}
	}

	public async UniTask ShakeChoiceOptionAsync(int optionIndex = -1)
	{
		await choiceBoard.PlayShakeTweenAsync(optionIndex);
	}

	public async UniTask RemoveChoiceBoardAsync()
	{
		await InputManager.RemoveControlTarget(choiceBoard);
	}

	public void Jump(Scenario scenario)
	{
		scenarioStack.Peek().SkipScriptWhileType<JumpScript>();
		StackScenario(scenario);
		RunNextScript();
	}

	public void Chat()
	{
		Scenario scenario = scenarioStack.Peek();
		scenario.SkipScriptWhileType<ChatScript>();
		List<ChatScript> validChats = GetValidChats(scenario);
		for (int i = 0; i < validChats.Count; i++)
		{
			ChatScript chatScript = validChats[i];
			if (i >= validChats.Count - 1 || !chatScript.IsAlreadyWatched)
			{
				StackScenario(chatScript.Scenario);
				RunNextScript();
				return;
			}
		}
		if (validChats.Count == 0)
		{
			DebugLog.ScenarioLogError("대화할 수 있는 잡담 스크립트가 없습니다. 이는 의도된 상황이 아닙니다! 게임을 멈출 수는 없으니 다음 스크립트로 넘어갑니다.");
		}
		else
		{
			DebugLog.ScenarioLogError("모든 잡담 스크립트를 이미 봤었습니다. 이는 의도된 상황이 아닙니다! 게임을 멈출 수는 없으니 다음 스크립트로 넘어갑니다.");
		}
		RunNextScript();
	}

	private List<ChatScript> GetValidChats(Scenario currentScenario)
	{
		List<ChatScript> list = (from ChatScript script in currentScenario.Where((Script script) => script is ChatScript)
			where script.IsTriggerSatisfy
			select script).ToList();
		List<ChatScript> list2 = TrimWatchedChat(currentScenario.GroupID, list);
		if (list2.Count != 0)
		{
			return list2;
		}
		return list;
	}

	private List<ChatScript> TrimWatchedChat(string id, List<ChatScript> chats)
	{
		List<ChatScript> list = chats.ToList();
		if (!watchedChatDictionary.TryGetValue(id, out var value))
		{
			value = new Dictionary<string, bool>();
			foreach (ChatScript chat in chats)
			{
				value.Add(chat.ScenarioID, chat.IsAlreadyWatched);
			}
			watchedChatDictionary.Add(id, value);
		}
		foreach (KeyValuePair<string, bool> kvp in value.Where((KeyValuePair<string, bool> keyValuePair) => keyValuePair.Value))
		{
			list.RemoveAll((ChatScript cs) => cs.ScenarioID == kvp.Key);
		}
		return list;
	}

	private void ResetWatchedChat()
	{
		watchedChatDictionary.Clear();
	}

	public void DeactivateOnSkipCutscene()
	{
		RunNextScript_Skip();
		Deactivate().GetAwaiter().OnCompleted(delegate
		{
			scenarioStack.Clear();
		});
	}

	public void ShowSpecialTalk(string groupId, int scriptNum, float totalDuration, float fadeDuration)
	{
		ScenarioData scenarioData = DataManager.Instance.GetScenarioData($"{groupId}_{scriptNum}");
		if (scenarioData == null)
		{
			DebugLog.ScenarioLogError($"{groupId}의 {scriptNum}번 스크립트를 찾을 수 없습니다!");
			return;
		}
		try
		{
			specialTalkBoard.SetScenario(scenarioData);
			specialTalkBoard.SetVoice(scenarioData.Actor, scenarioData.VoiceKey);
			specialTalkBoard.SetActiveDuration(totalDuration, fadeDuration);
			if (TalkData.GetTalkCount(groupId) == 0)
			{
				TalkData.AddTalk(groupId);
			}
		}
		catch (Exception)
		{
			DebugLog.ScenarioLogError($"{groupId}에서 {Game.CurrentLanguage} 언어의 데이터를 찾을 수 없습니다!");
			throw;
		}
	}

	public async UniTask ShowSpecialTalk(string groupId, float delay = 0f)
	{
		narrationData = null;
		if (delay > 0f)
		{
			await UniTask.WaitForSeconds(delay);
		}
		int index = 1;
		narrationData = DataManager.Instance.GetScenarioData($"{groupId}_{index}");
		TalkData.AddTalk(groupId);
		while (narrationData != null)
		{
			if (narrationData.ScriptType == ScriptType.Narration)
			{
				specialTalkBoard.SetScenario(narrationData);
				specialTalkBoard.SetVoice(narrationData.Actor, narrationData.VoiceKey);
				specialTalkBoard.SetActiveDuration();
				await UniTask.WaitForSeconds(specialTalkBoard.TotalSeconds);
			}
			else if (narrationData.ScriptType == ScriptType.ScenarioInfo)
			{
				string text = narrationData.Option;
				if (text.IsNullOrEmpty())
				{
					text = "None";
				}
				if (Trigger.CheckTrigger(text))
				{
					MenuMapPanel.AddInfoStoneInformation(narrationData.KO);
					break;
				}
			}
			else
			{
				DebugLog.ScenarioLogError($"{groupId}의 {index}번 스크립트는 특수 대화에 적합하지 않은 스크립트 타입입니다: {narrationData.ScriptType}");
			}
			index++;
			narrationData = DataManager.Instance.GetScenarioData($"{groupId}_{index}");
		}
	}

	public async UniTask ShowSpecialTalk(string groupId, float totalDuration, float fadeDuration, float delay = 0f)
	{
		if (delay > 0f)
		{
			await UniTask.WaitForSeconds(delay);
		}
		int index = 1;
		ScenarioData scenarioData = DataManager.Instance.GetScenarioData($"{groupId}_{index}");
		TalkData.AddTalk(groupId);
		while (scenarioData != null)
		{
			specialTalkBoard.SetScenario(scenarioData);
			specialTalkBoard.SetVoice(scenarioData.Actor, scenarioData.VoiceKey);
			specialTalkBoard.SetActiveDuration(totalDuration, fadeDuration);
			await UniTask.WaitForSeconds(totalDuration);
			index++;
			scenarioData = DataManager.Instance.GetScenarioData($"{groupId}_{index}");
		}
	}

	public void HideSpecialTalk()
	{
		narrationData = null;
		specialTalkBoard.Clear();
		specialTalkBoard.DisableNow();
	}

	public string ControllerName()
	{
		return "Talk Controller";
	}

	public UniTask Activate()
	{
		talkBoard.Initialize();
		FieldCanvas.HideAllAsync(instant: false).Forget();
		SoundManager.FadeSFXEmitters(1f, 0.5f, 1f);
		return default(UniTask);
	}

	public void Control()
	{
		if (InputManager.confirmDown && talkBoard.IsActive && IsTalking)
		{
			if (talkBoard.IsReading)
			{
				talkBoard.SkipToEnd();
			}
			else if (talkBoard.NextReady)
			{
				talkBoard.NextScriptAsync(RunNextScript).Forget();
			}
		}
	}

	public UniTask Pause()
	{
		return default(UniTask);
	}

	public UniTask Restart()
	{
		return default(UniTask);
	}

	public async UniTask Deactivate()
	{
		SoundManager.FadeSFXEmitters(0.5f, 1f, 1f);
		await talkBoard.DeactivateTalkAsync();
		if (!InputManager.ContainControlTarget(typeof(CutsceneProducer)) && !InputManager.ContainControlTarget(typeof(CardEngravingPanel)) && !InputManager.ContainControlTarget(typeof(PartUpgradePanel)))
		{
			FieldCanvas.ShowAllAsync(instant: false).Forget();
		}
	}

	public void StartTalk_Skip(Scenario scenario)
	{
		StackScenario(scenario);
		RunNextScript_Skip();
	}

	public void ActivateTalk_Skip()
	{
		RunNextScript_Skip();
	}

	public void RunNextScript_Skip()
	{
		Scenario scenario;
		while (true)
		{
			if (scenarioStack.Count == 0)
			{
				return;
			}
			scenario = scenarioStack.Peek();
			if (!scenario.IsLastLine)
			{
				break;
			}
			scenarioStack.Pop();
		}
		scenario.Run_Skip(this);
	}

	public void AddChoiceOption_Skip(string chosenScenarioID, string text)
	{
		Scenario chosenScenario = ScenarioHelper.GetScenario(chosenScenarioID);
		ChoiceData choiceData = new ChoiceData(text, showNotiIcon: false, delegate
		{
			if (chosenScenario != null && chosenScenario.HasValue)
			{
				StackScenario(chosenScenario);
			}
			RunNextScript_Skip();
		}, chosenScenarioID.IsNullOrEmpty());
		lastChoice = choiceData;
	}

	public void HandleNextChoice_Skip()
	{
		if (scenarioStack.Peek().IsNextScriptClassSameAs<ChoiceScript>())
		{
			RunNextScript_Skip();
			return;
		}
		lastChoice.OnSelectChoice();
		lastChoice = default(ChoiceData);
	}

	public void Jump_Skip(Scenario scenario)
	{
		scenarioStack.Peek().SkipScriptWhileType<JumpScript>();
		StackScenario(scenario);
		RunNextScript_Skip();
	}

	public void Chat_Skip()
	{
		Scenario scenario = scenarioStack.Peek();
		scenario.SkipScriptWhileType<ChatScript>();
		List<ChatScript> validChats = GetValidChats(scenario);
		for (int i = 0; i < validChats.Count; i++)
		{
			ChatScript chatScript = validChats[i];
			if (i >= validChats.Count - 1 || !chatScript.IsAlreadyWatched)
			{
				StackScenario(chatScript.Scenario);
				RunNextScript_Skip();
				break;
			}
		}
	}
}
