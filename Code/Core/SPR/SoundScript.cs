using System;
using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class SoundScript : Script
{
	private enum Order
	{
		None,
		PlayBgm,
		StopBgm
	}

	private readonly Order order;

	private readonly string soundKey;

	public override bool IsInstant => true;

	public SoundScript(IReadOnlyList<string> values)
	{
		order = Enum.Parse<Order>(values[1]);
		soundKey = values[Script.ScenarioKOIndex];
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		switch (order)
		{
		case Order.PlayBgm:
			Script.SoundManager.PlayBGM(soundKey);
			break;
		case Order.StopBgm:
			Script.SoundManager.StopBGM();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		talkCanvas.RunNextScript();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		switch (order)
		{
		case Order.PlayBgm:
			Script.SoundManager.PlayBGM(soundKey);
			break;
		case Order.StopBgm:
			Script.SoundManager.StopBGM();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		talkCanvas.RunNextScript_Skip();
	}
}
