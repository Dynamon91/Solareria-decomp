using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Talk;

namespace SPR;

public class TalkScript : Script
{
	private readonly string speakerName;

	private readonly float autoDuration;

	private readonly string speakerSpeech;

	private readonly string voiceKey;

	public override bool IsInstant => false;

	public TalkScript(IReadOnlyList<string> values)
	{
		speakerName = values[0];
		if (!BasicExtension.TryParseFloat(values[1], out autoDuration))
		{
			autoDuration = -1f;
		}
		string text = values[Script.SpeechIndex];
		if (string.IsNullOrEmpty(text))
		{
			text = (string.IsNullOrEmpty(values[Script.ScenarioENIndex]) ? values[Script.ScenarioKOIndex] : values[Script.ScenarioENIndex]);
		}
		speakerSpeech = text.EnteredString();
		voiceKey = values[2];
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		talkCanvas.ActivateTalk(speakerName, speakerSpeech, autoDuration, voiceKey).Forget();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		talkCanvas.ActivateTalk_Skip();
	}
}
