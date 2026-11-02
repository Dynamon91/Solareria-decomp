using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace SPR.Cutscene;

public abstract class HeatCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private List<GameObject> backgrounds;

	[SerializeField]
	private Image circleLightImage;

	[SerializeField]
	private GameObject cutsceneObjectsParent;

	protected override void SetProducerState(int state)
	{
		SetObjectsActive(value: false);
	}

	protected override UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		SetObjectsActive(value: true);
		UniTask.WaitUntil(() => base.PlayableDirector.state != PlayState.Playing, PlayerLoopTiming.Update, token).GetAwaiter().OnCompleted(delegate
		{
			if (!IsSkipping)
			{
				EndTimeline();
			}
		});
		return default(UniTask);
	}

	protected override void OnEndTimeline(int state)
	{
		SetObjectsActive(value: false);
		circleLightImage.color = GraphicHelper.TransparentColor;
	}

	protected override void OnSkipTimeline(int state)
	{
	}

	protected async UniTask PlayHeatOpenSequenceAsync(int targetLevel)
	{
		await CutsceneProducer.UIManager.FieldCanvas.PlayerStatusBoard.HeatBoard.PlayLevelOpenSequenceAsync(targetLevel);
	}

	private void SetObjectsActive(bool value)
	{
		cutsceneObjectsParent.SetActive(value);
		backgrounds.ForEach(delegate(GameObject go)
		{
			go.SetActive(value);
		});
	}

	public void TurnOnBackgrounds()
	{
		backgrounds.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: true);
		});
	}
}
