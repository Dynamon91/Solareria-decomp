using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace SPR.Cutscene;

public class PlayerBornCutsceneProducer : CutsceneProducer
{
	[Header("Timeline")]
	[SerializeField]
	private PlayableAsset prologueTimeline;

	[SerializeField]
	private PlayableAsset playerBornTimeline;

	[Header("Assets")]
	[SerializeField]
	private VideoClip prologueVideo;

	[SerializeField]
	private EventReference bgmEvent;

	[SerializeField]
	[Range(0f, 4.2f)]
	private float bgmDelay;

	[Header("Reference")]
	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private SkeletonAnimation fakePlayerSpine;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private SkeletonAnimation armorSkeleton;

	[SerializeField]
	private List<BoneFollower> armors;

	public const float FadeDuration = 6f;

	private EventInstance bgmInstance;

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		CutsceneProducer.SoundManager.StopBGM();
		Game.Instance.RuntimeData.SetBGMLessEvent(value: true);
		base.PlayableDirector.playableAsset = prologueTimeline;
		base.PlayableDirector.time = 0.0;
		base.PlayableDirector.Evaluate();
		CutsceneProducer.UIManager.VideoCanvas.SetVideo(prologueVideo);
		await CutsceneProducer.UIManager.VideoCanvas.PlayVideoAsync(token);
		base.PlayableDirector.Play();
		await UniTask.WaitForSeconds(1f, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		await CutsceneProducer.FadeCanvas.FadeIn(1f, 0f, Ease.Linear, token);
		await UniTask.WaitWhile(() => CutsceneProducer.UIManager.VideoCanvas.IsVideoPlaying, PlayerLoopTiming.Update, token);
		base.PlayableDirector.Stop();
		CutsceneProducer.UIManager.VideoCanvas.StopVideo();
		await CutsceneProducer.FadeCanvas.FadeOut(0.1f, 0f, token);
		CutsceneProducer.UIManager.VideoCanvas.ReleaseVideo();
		base.PlayableDirector.playableAsset = playerBornTimeline;
		base.PlayableDirector.time = 0.0;
		base.PlayableDirector.Evaluate();
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
		CutsceneProducer.FadeCanvas.FadeIn(6f, 0f, Ease.InQuad, token).Forget();
		await UniTask.WaitForSeconds(bgmDelay, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		bgmInstance = CutsceneProducer.SoundManager.PlayOneShot(bgmEvent, forcedOnSceneLoad: true);
		await UniTask.WaitForSeconds(4.2f - bgmDelay, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
	}

	protected override void OnEndTimeline(int state)
	{
		if (fakePlayer.gameObject.activeSelf)
		{
			CutsceneProducer.Player.mover.SynchronizeAnimation(fakePlayerSpine);
		}
		SynchronizePlayer(fakePlayer);
		SynchronizeEndVcamAsync(endVcam).Forget();
		Game.Instance.SoundManager.StopBGM();
		CutsceneProducer.Player.PlayerStat.UpdateFieldCanvas();
		CutsceneProducer.UIManager.FieldCanvas.ShowAllAsync(instant: false).Forget();
		armorSkeleton.SetAnimation("action_static", 0, loop: false);
		foreach (BoneFollower armor in armors)
		{
			armor.enabled = false;
			armor.transform.GetChild(0).gameObject.SetActive(value: true);
		}
		base.PlayableDirector.Stop();
		CutsceneProducer.UIManager.VideoCanvas.StopVideo();
		CutsceneProducer.UIManager.VideoCanvas.ReleaseVideo();
		Game.Instance.SaveBonfire();
	}

	protected override void OnSkipTimeline(int state)
	{
		CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
		CutsceneProducer.FieldCanvas.NarrationBoard.DisableNow(disableControlGuide: true);
		if (bgmInstance.isValid())
		{
			bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			bgmInstance.release();
		}
	}

	protected override void SetProducerState(int state)
	{
		if (state == 0)
		{
			CutsceneProducer.UIManager.FieldCanvas.HideAllAsync(instant: true).Forget();
			return;
		}
		foreach (BoneFollower armor in armors)
		{
			armor.enabled = false;
			armor.transform.GetChild(0).gameObject.SetActive(value: true);
		}
		armorSkeleton.SetAnimation("action_static", 0, loop: false);
	}
}
