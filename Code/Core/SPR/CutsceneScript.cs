using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Talk;

namespace SPR;

public class CutsceneScript : Script
{
	private readonly string targetName;

	private readonly int index;

	public override bool IsInstant => false;

	public CutsceneScript(IReadOnlyList<string> values)
	{
		targetName = values[0];
		if (!values[1].TryParseInt(out index))
		{
			index = -1;
		}
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		PlayCutsceneAsync(talkCanvas).Forget();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		PlayCutsceneAsync(talkCanvas).Forget();
	}

	private async UniTask PlayCutsceneAsync(TalkCanvas talkCanvas)
	{
		CutsceneProducer producer = Script.FieldController.FindCutsceneProducer(targetName);
		if (producer.IsNull())
		{
			DebugLog.ScenarioLogError(targetName + "의 CutsceneProducer가 존재하지 않습니다!");
			return;
		}
		await Game.Instance.InputManager.RemoveControlTarget(talkCanvas);
		talkCanvas.InvokeFinishCallback();
		if (index > -1)
		{
			await producer.PlayTimelineAsync(index, blockControl: true);
		}
	}
}
