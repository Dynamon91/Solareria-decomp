using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.UI.Tutorial;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class HeatUpgradeCutsceneProducer : HeatCutsceneProducer
{
	[SerializeField]
	private List<TimelineAsset> timelineAssets;

	protected override void SetProducerState(int state)
	{
		base.SetProducerState(state);
		if (state >= 2)
		{
			int num = state - 2;
			if (num < timelineAssets.Count)
			{
				base.PlayableDirector.playableAsset = timelineAssets[num];
			}
		}
	}

	protected override void OnEndTimeline(int state)
	{
		base.OnEndTimeline(state);
		OpenNextHeatAsync().Forget();
	}

	private async UniTask OpenNextHeatAsync()
	{
		await PlayHeatOpenSequenceAsync(PlayerStat.HeatMaxLevel + 1);
		if (Enum.TryParse<ActionName>(PlayerHeat.GetCurrentUnlockSkillType(PlayerStat.HeatMaxLevel).ToString(), out var result))
		{
			CutsceneProducer.UIManager.TutorialCanvas.SetData(result);
			CutsceneProducer.InputManager.StackControlTarget(CutsceneProducer.UIManager.TutorialCanvas).Forget();
		}
	}

	public static HeatUpgradeCutsceneProducer GetProducer()
	{
		CutsceneProducer cutsceneProducer = Game.Instance.FieldController.GetCutsceneProducers().FirstOrDefault((CutsceneProducer p) => p is HeatCutsceneProducer);
		if ((object)cutsceneProducer != null)
		{
			return (HeatUpgradeCutsceneProducer)cutsceneProducer;
		}
		DebugLog.GeneralLogError("열기 강화 컷씬을 씬에서 찾을 수 없습니다!");
		return null;
	}
}
