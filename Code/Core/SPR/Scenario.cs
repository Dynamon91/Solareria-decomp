using System;
using System.Collections;
using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class Scenario : IEnumerable<Script>, IEnumerable
{
	private List<Script> scripts;

	private int currentLineIndex;

	private bool isPlaying;

	public const float ResonanceDelay = 2f;

	public const string HaechiRescueResonanceGroupId = "HaechiRescue_Resonance";

	public const string HaechiFlyResonanceGroupId = "HaechiFly_Resonance";

	public const string DarkChasaClearResonanceGroupId = "DarkChasaClear_Resonance";

	public const string BaranGuideMyosoResonanceGroupId = "BaranGuideMyoso_Resonance";

	public const string ChasaRiverCutsceneGroupId = "ChasaRiverCutscene_2_1";

	public const string ImahenDivisionCutsceneGroupId = "ImahenDivisionCutscene_4_1";

	public const string MoldinClearVeritaCalledGroupId = "MoldinClearVeritaCalled_Special";

	public const string TrueEndScenarioID = "EndingCutscene_Thank_Real";

	public const string EndScenarioID = "EndingCutscene_Thank_Normal";

	public const string MuhaWarmhornShellScenarioID = "Muha_Hello_WS23";

	public const string JimaMucoraFortressLastChatGroupId = "Jima_Talk_MF12_1";

	public const string JimaSoldOutPerfectlyGroupId = "Jima_SoldOutPerfectly";

	public const string LunyaWentMucoraFortressGroupId = "Lunya_Talk_WentMucoraFortress_1";

	public const string LunyaLibraryVeritaClearedGroupId = "Lunya_Library_Verita_Cleared";

	public const string CorEarthfordDeathGroupId = "Earthford_28_Cor";

	public const string FlutisLastChatGroupId = "Flutis_Talk_5";

	public const string FlutisBringLunyaChatGroupId = "Flutis_Talk_6";

	public const string FlutisSoldOutGroupId = "Flutis_SoldOut";

	public const string FlutisSoldOutNoLunyaGroupId = "Flutis_SoldOut_NoLunya";

	public const string OpusLastChatGroupId = "Opus_Talk_8";

	public const string OpusGiveJournalGroupId = "Opus_GiveJournal";

	public const string OpusGiveLetterGroupId = "Opus_GiveLetter";

	public const string PwoongLastChatGroupId = "Pwoong_Talk_7";

	public const string PwoongVeritaInfectedGroupId = "Pwoong_VeritaInfected";

	public const string MyosoBaranCutsceneGroupId = "BaranCutscene_9_5";

	public const string MyosoLastChatGroupId = "SilentRuins_09_Myoso";

	public const string MuhaByeGroupId = "Muha_Bye";

	public const string VeritaGateCutsceneGroupId = "VeritaGateCutscene_0_0";

	public const string LeafaLastChatVeritaClearedGroupId = "Leafa_Talk_VeritaCleared_3";

	public const string LeafaSoldOutGroupId = "Leafa_SoldOut";

	public string GroupID { get; }

	public bool IsLastLine
	{
		get
		{
			if (isPlaying)
			{
				return currentLineIndex == scripts.Count - 1;
			}
			return false;
		}
	}

	public bool HasValue
	{
		get
		{
			if (scripts != null)
			{
				return scripts.Count != 0;
			}
			return false;
		}
	}

	public Scenario(string groupID)
	{
		GroupID = groupID;
		scripts = new List<Script>();
		currentLineIndex = 0;
		isPlaying = false;
	}

	public Scenario AddScript(Script script)
	{
		if (scripts == null)
		{
			scripts = new List<Script>();
		}
		scripts.Add(script);
		return this;
	}

	public void Reset()
	{
		currentLineIndex = 0;
		isPlaying = false;
	}

	public void Run(TalkCanvas talkCanvas)
	{
		if (isPlaying)
		{
			currentLineIndex++;
		}
		else
		{
			isPlaying = true;
		}
		try
		{
			scripts[currentLineIndex].Run(talkCanvas);
		}
		catch (ArgumentOutOfRangeException)
		{
			DebugLog.GeneralLogError($"인덱스 값({currentLineIndex})이 {GroupID}의 스크립트 개수({scripts.Count}) 범위를 벗어납니다!");
		}
	}

	public void Run_Skip(TalkCanvas talkCanvas)
	{
		if (isPlaying)
		{
			currentLineIndex++;
		}
		else
		{
			isPlaying = true;
		}
		try
		{
			scripts[currentLineIndex].Skip(talkCanvas);
		}
		catch (ArgumentOutOfRangeException)
		{
			DebugLog.GeneralLogError($"인덱스 값({currentLineIndex})이 {GroupID}의 스크립트 개수({scripts.Count}) 범위를 벗어납니다!");
		}
	}

	public bool IsNextScriptClassSameAs<T>() where T : Script
	{
		if (currentLineIndex + 1 >= scripts.Count)
		{
			return false;
		}
		return scripts[currentLineIndex + 1] is T;
	}

	public void SkipScriptWhileType<T>() where T : Script
	{
		while (IsNextScriptClassSameAs<T>())
		{
			currentLineIndex++;
		}
	}

	public IEnumerator<Script> GetEnumerator()
	{
		return scripts.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
