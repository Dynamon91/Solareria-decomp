using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.Field;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.Cutscene;

public class FirstHeatCutsceneProducer : HeatCutsceneProducer
{
	[SerializeField]
	private HeatWall heatWall;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	protected override void SetProducerState(int state)
	{
		base.SetProducerState(state);
		if (state == 1)
		{
			heatWall.TurnOff();
		}
	}

	protected override void OnEndTimeline(int state)
	{
		base.OnEndTimeline(state);
		ShowTutorialAsync().Forget();
	}

	private async UniTask ShowTutorialAsync()
	{
		await UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(CutsceneProducer.Player));
		await UniTask.WaitForSeconds(1f);
		await PlayHeatOpenSequenceAsync(2);
		TutorialCanvas.SetData(ActionName.Heat);
		CutsceneProducer.InputManager.StackControlTarget(TutorialCanvas).Forget();
		heatWall.TurnOff();
	}

	public static FirstHeatCutsceneProducer GetProducer()
	{
		CutsceneProducer cutsceneProducer = Game.Instance.FieldController.GetCutsceneProducers().FirstOrDefault((CutsceneProducer p) => p is FirstHeatCutsceneProducer);
		if ((object)cutsceneProducer != null)
		{
			return (FirstHeatCutsceneProducer)cutsceneProducer;
		}
		DebugLog.GeneralLogError("열기 획득 컷씬을 씬에서 찾을 수 없습니다!");
		return null;
	}
}
