using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace SPR.Cutscene;

public class SceneMontageCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private FieldID targetFieldID;

	[SerializeField]
	private string targetCutscene;

	[SerializeField]
	private CinemachineVirtualCamera vCam;

	[SerializeField]
	private List<GameObject> activeObjects;

	[SerializeField]
	private List<GameObject> disableObjects;

	[SerializeField]
	private float sustainDuration = 1f;

	[Header("Fade")]
	[SerializeField]
	protected float fadeInDuration = 1f;

	protected virtual (FieldID fieldID, string cutsceneName) Target => (fieldID: targetFieldID, cutsceneName: targetCutscene);

	protected virtual SceneDataModel SceneDataModel => SceneDataModel.MoveCutsceneModel(Target.fieldID, Target.cutsceneName);

	protected float SustainDuration => sustainDuration;

	protected override void SetProducerState(int state)
	{
		if (state == 0)
		{
			activeObjects.ForEach(delegate(GameObject obj)
			{
				obj.SetActive(value: true);
			});
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		SustainVcamForFade(vCam);
		CutsceneProducer.UIManager.FadeCanvas.FadeIn(fadeInDuration, 0f, Ease.Linear, token).Forget();
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		CutsceneProducer.InputManager.BlockControlForTask(EndTimelineAsync(), "Move Scene").Forget();
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		DisableFadeInAfterSkip();
	}

	private async UniTask EndTimelineAsync()
	{
		disableObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: false);
		});
		await UniTask.WaitForSeconds(sustainDuration);
		await CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f);
		Game.Instance.LoadFieldAsync(SceneDataModel).Forget();
	}
}
