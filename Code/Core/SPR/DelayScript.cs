using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Talk;

namespace SPR;

public class DelayScript : Script
{
	private readonly float delayTime;

	public override bool IsInstant => false;

	public DelayScript(IReadOnlyList<string> values)
	{
		if (!BasicExtension.TryParseFloat(values[0], out delayTime))
		{
			DebugLog.ScenarioLogError("DelayScript의 시간 값이 올바르지 않습니다.");
			delayTime = 0f;
		}
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		WaitAndRunAsync(talkCanvas).Forget();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		talkCanvas.RunNextScript_Skip();
	}

	private async UniTask WaitAndRunAsync(TalkCanvas talkCanvas)
	{
		talkCanvas.IsTalking = false;
		if (delayTime > 0f)
		{
			await UniTaskExtension.WaitForSeconds(delayTime);
		}
		talkCanvas.RunNextScript();
	}
}
