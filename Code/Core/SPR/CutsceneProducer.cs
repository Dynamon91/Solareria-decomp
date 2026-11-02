using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using Library;
using Rewired;
using SPR.Field;
using SPR.Save;
using SPR.Timeline;
using SPR.UI;
using SPR.UI.Field;
using SPR.UI.Memory;
using SPR.UI.Talk;
using Spine.Unity;
using Spine.Unity.Playables;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR;

[RequireComponent(typeof(PlayableDirector))]
public abstract class CutsceneProducer : MonoBehaviour, IControllable, IStateSavable
{
	private PlayableDirector playableDirector;

	private CinemachineBrain cinBrain;

	private Area myArea;

	private bool isProducing;

	protected bool IsSkipping;

	private bool fadeInAfterSkip = true;

	private int currentState;

	private CancellationTokenSource cutsceneTokenSource;

	private const float CutSceneSkipHoldTime = 0.5f;

	protected static bool IsTalking { get; private set; }

	public virtual string ID => GetType().Name;

	protected FieldController FieldController => Game.Instance.FieldController;

	protected PlayableDirector PlayableDirector => playableDirector ?? (playableDirector = GetComponent<PlayableDirector>());

	protected CinemachineBrain CineBrain => cinBrain ?? (cinBrain = Camera.main.GetComponent<CinemachineBrain>());

	protected Area MyArea => myArea ?? (myArea = GetComponentInParent<Area>());

	public int CurrentState => currentState;

	public virtual bool IsProducing => isProducing;

	public bool IsProducingOnlyPlayableDirector => isProducing;

	protected virtual List<int> UnskippableCutscenes => new List<int>();

	protected virtual bool SkipCondition => IsProducing;

	protected static PlayerController Player => Game.Instance.Player;

	protected static FieldController Field => Game.Instance.FieldController;

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	protected static UIManager UIManager => Game.Instance.UIManager;

	protected static TalkCanvas TalkCanvas => UIManager.TalkCanvas;

	protected static FadeCanvas FadeCanvas => UIManager.FadeCanvas;

	protected static SkipBoard SkipBoard => UIManager.FadeCanvas.SkipBoard;

	protected static MenuMemoryPanel MemoryPanel => Game.Instance.UIManager.MenuCanvas.MemoryPanel;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	protected static WorldData WorldData => Game.Instance.SaveData.WorldData;

	private static TalkData TalkData => Game.Instance.SaveData.TalkData;

	protected static EnemyFightData EnemyFightData => Game.Instance.SaveData.EnemyFightData;

	protected static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public bool IsValid => !BasicExtension.IsNull(this);

	public void LoadState(int state)
	{
		currentState = state;
		SetProducerState(state);
	}

	public void ChangeState(int state)
	{
		currentState = state;
		if (WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	protected abstract void SetProducerState(int state);

	public async UniTask PlayTimelineAsync(int state, bool blockControl = false, bool hideCanvas = true, bool checkIsProducing = true)
	{
		isProducing = checkIsProducing;
		LoadState(state);
		if (hideCanvas)
		{
			FieldCanvas.HideAllAsync(instant: false).Forget();
		}
		PlayableDirector.time = 0.0;
		await StartTimelineAsync(blockControl, TaskUtil.RefreshToken(ref cutsceneTokenSource));
		if (!cutsceneTokenSource.IsCancellationRequested)
		{
			PlayableDirector.Play();
			await UniTask.WaitUntil(() => (checkIsProducing && !isProducing) || cutsceneTokenSource.IsCancellationRequested);
		}
	}

	private async UniTask StartTimelineAsync(bool blockControl, CancellationToken token)
	{
		if (blockControl)
		{
			InputManager.StackControlTarget(this).Forget();
		}
		await UniTask.WhenAny(OnStartTimelineAsync(currentState, token), UniTask.WaitUntil(() => token.IsCancellationRequested));
	}

	protected abstract UniTask OnStartTimelineAsync(int state, CancellationToken token);

	public void EndTimeline()
	{
		WanderingData.AddWatchedCutscene(ID, currentState);
		OnEndTimeline(currentState);
		isProducing = false;
		if (InputManager.ContainsInControlStack(this))
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
		if (InputManager.IsCurrentControlTarget(Player))
		{
			FieldCanvas.ShowAllAsync(instant: false).Forget();
		}
		ChangeState(currentState + 1);
	}

	protected abstract void OnEndTimeline(int state);

	private async UniTask SkipTimelineAsync()
	{
		IsSkipping = true;
		await UIManager.FadeCanvas.FadeOut(0.5f, 0f, default(CancellationToken), ignoreTimescale: true);
		SkipUnstartedScenarios();
		if (isProducing)
		{
			cutsceneTokenSource?.Cancel();
			SoundManager.StopVoice();
			OnSkipTimeline(currentState);
			PlayableDirector.time = PlayableDirector.duration;
			PlayableDirector.Stop();
			EndTimeline();
		}
		else
		{
			DebugLog.GeneralLog("스킵 도중 컷씬이 이미 종료되어 있습니다!");
		}
		if (fadeInAfterSkip)
		{
			await UIManager.FadeCanvas.FadeIn(0.5f);
		}
		else
		{
			fadeInAfterSkip = true;
		}
		IsSkipping = false;
	}

	protected abstract void OnSkipTimeline(int state);

	protected void BreakTimeline()
	{
		PlayableDirector.time = 0.0;
		PlayableDirector.Stop();
		isProducing = false;
		if (InputManager.ContainsInControlStack(this))
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
		if (InputManager.IsCurrentControlTarget(Player))
		{
			FieldCanvas.ShowAllAsync(instant: false).Forget();
		}
	}

	protected void DisableFadeInAfterSkip()
	{
		fadeInAfterSkip = false;
	}

	public virtual bool ShouldLoop(int index)
	{
		return false;
	}

	public virtual void EnterLoop(int index)
	{
	}

	public virtual void ExitLoop(int index)
	{
	}

	protected void SynchronizePlayer(Transform fakePlayer, Vector3 positionOffset = default(Vector3))
	{
		if (!BasicExtension.IsNull(fakePlayer))
		{
			Vector3 vector = fakePlayer.position + positionOffset;
			bool isFacingRight = fakePlayer.localScale.x > 0f;
			Player.SetPosition(vector, isFacingRight);
			fakePlayer.gameObject.SetActive(value: false);
		}
	}

	protected void SwitchToFakeSpine(SpineObject targetObject, Transform fakeFigure, SkeletonAnimation fakeSpine)
	{
		fakeSpine.SynchronizeAnimation(targetObject);
		fakeFigure.gameObject.SetActive(value: true);
		targetObject.gameObject.SetActive(value: false);
	}

	protected void BindPlayerTracks(bool value, bool tween = false, SkeletonAnimation falseAnimation = null, Transform falseTransform = null)
	{
		PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Player Animation Track", value ? Player.mover.SkeletonAnimation : falseAnimation);
		if (tween)
		{
			PlayableDirector.TrackBinding<TweenTrack>("Player Tween Track", value ? Player.transform : falseTransform);
		}
		if (!value)
		{
			Player.mover.SynchronizeSelf("idle");
			falseAnimation?.SetAnimation("idle");
		}
	}

	protected void MoveCutsceneStartCamToCurrentCam(CinemachineVirtualCamera startVcam)
	{
		startVcam.transform.position = CineBrain.transform.position;
		startVcam.Priority = CineBrain.ActiveVirtualCamera.Priority + 1;
	}

	protected async UniTask BlendFieldCamToCutsceneStartCamAsync(CinemachineVirtualCamera startVcam, float blendTime = 0.4f)
	{
		if (CineBrain.ActiveVirtualCamera == startVcam)
		{
			await UniTask.CompletedTask;
			return;
		}
		startVcam.Priority = CineBrain.ActiveVirtualCamera.Priority + 1;
		startVcam.gameObject.SetActive(value: true);
		float originBlendTime = CineBrain.m_DefaultBlend.BlendTime;
		CineBrain.m_DefaultBlend.m_Time = blendTime;
		await UniTask.WaitUntil(() => CineBrain.IsBlending && CineBrain.ActiveBlend.CamB == startVcam);
		await UniTask.WaitUntil(() => !CineBrain.IsBlending);
		CineBrain.m_DefaultBlend.m_Time = originBlendTime;
		startVcam.gameObject.SetActive(value: false);
	}

	protected async UniTask SynchronizeEndVcamAsync(CinemachineVirtualCamera endVcam)
	{
		if (CineBrain.ActiveVirtualCamera != null && CineBrain.ActiveVirtualCamera.IsValid)
		{
			endVcam.Priority = CineBrain.ActiveVirtualCamera.Priority + 1;
		}
		endVcam.gameObject.SetActive(value: true);
		CinemachineBlendDefinition.Style originStyle = CineBrain.m_DefaultBlend.m_Style;
		CineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
		await UniTask.Yield();
		CineBrain.m_DefaultBlend.m_Style = originStyle;
		endVcam.gameObject.SetActive(value: false);
	}

	protected async UniTask SwitchFieldCamToCutsceneStartCam(CinemachineVirtualCamera startVcam)
	{
		if (CineBrain.ActiveVirtualCamera != startVcam)
		{
			startVcam.Priority = CineBrain.ActiveVirtualCamera.Priority + 1;
			startVcam.gameObject.SetActive(value: true);
			CinemachineBlendDefinition.Style originStyle = CineBrain.m_DefaultBlend.m_Style;
			CineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
			await UniTask.Yield();
			CineBrain.m_DefaultBlend.m_Style = originStyle;
			startVcam.gameObject.SetActive(value: false);
		}
	}

	protected void SustainVcamForFade(CinemachineVirtualCamera vcam)
	{
		vcam.gameObject.SetActive(value: true);
		CinemachineBlendDefinition.Style originStyle = CineBrain.m_DefaultBlend.m_Style;
		CineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
		UniTask.Yield().GetAwaiter().OnCompleted(delegate
		{
			CineBrain.m_DefaultBlend.m_Style = originStyle;
		});
	}

	protected void StartScenarioInsideCutscene(int index)
	{
		if (!IsSkipping)
		{
			Scenario scenario = ScenarioHelper.GetScenario(GetCurrentScenarioId(index));
			TalkCanvas.StartTalkWithoutControl(scenario, EndTalking);
			IsTalking = true;
		}
		else
		{
			DebugLog.GeneralLog($"컷씬 스킵이 진행중이라 대화를 재생하지 않습니다! (state={CurrentState}, index={index})");
		}
	}

	private string GetCurrentScenarioId(int index)
	{
		return string.Format("{0}_{1}_{2}", ID.Replace("Producer", ""), currentState, index);
	}

	protected void SkipUnstartedScenarios()
	{
		if (BasicExtension.IsNull(PlayableDirector.playableAsset))
		{
			return;
		}
		TrackAsset trackAsset = ((TimelineAsset)PlayableDirector.playableAsset).GetOutputTracks().FirstOrDefault((TrackAsset asset) => asset.GetType() == typeof(LoopTrack));
		if ((object)trackAsset == null)
		{
			return;
		}
		foreach (LoopClip item in from c in trackAsset.GetClips()
			select c.asset as LoopClip)
		{
			string currentScenarioId = GetCurrentScenarioId(item.loopClipIndex);
			if (TalkData.GetTalkCount(currentScenarioId) <= 0)
			{
				Scenario scenario = ScenarioHelper.GetScenario(currentScenarioId);
				if (scenario.HasValue)
				{
					TalkCanvas.StartTalk_Skip(scenario);
				}
			}
		}
	}

	private static void EndTalking()
	{
		IsTalking = false;
	}

	protected void ShakeCamera(float amplitude = 0f, float frequency = 1f, CinemachineBasicMultiChannelPerlin noiseModule = null)
	{
		if ((object)noiseModule == null)
		{
			noiseModule = GetNoiseModule();
		}
		noiseModule.m_AmplitudeGain = amplitude;
		noiseModule.m_FrequencyGain = frequency;
	}

	protected void ShakeCameraForSeconds(float amplitude, float frequency, float duration)
	{
		CinemachineBasicMultiChannelPerlin noiseModule = GetNoiseModule();
		ShakeCamera(amplitude, frequency, noiseModule);
		UniTask.WaitForSeconds(duration).GetAwaiter().OnCompleted(delegate
		{
			ShakeCamera(0f, 0f, noiseModule);
		});
	}

	private CinemachineBasicMultiChannelPerlin GetNoiseModule()
	{
		return CineBrain.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
	}

	public virtual bool TryCutsceneOnPlayerDie()
	{
		return false;
	}

	protected static void NotShowFieldCanvasUntilSceneChange()
	{
		InputManager.BlockControlForTask(WaitUntilNewSceneAsync(), "Not To Show FieldCanvas").Forget();
	}

	protected static async UniTask WaitUntilNewSceneAsync()
	{
		await UniTask.WaitUntil(() => Game.Instance.CurrentState == GameState.SceneLoading);
		await UniTask.WaitUntil(() => Game.Instance.CurrentState == GameState.Initialized);
		await FieldCanvas.HideAllAsync(instant: true);
	}

	protected static void AddInteractCount(ActorName npcName)
	{
		if (TalkData.GetInteractCount(npcName) == 0)
		{
			MemoryPanel.UpdateMemory(npcName, isClear: false);
		}
		TalkData.AddInteractCount(npcName, Game.Instance.CurrentFieldID);
	}

	protected static void ExecuteWithoutChangeBGM(Action action)
	{
		Game.Instance.RuntimeData.SetBGMLessEvent(value: true);
		action();
		Game.Instance.RuntimeData.SetBGMLessEvent(value: false);
	}

	public void PlayerLookBack(bool value)
	{
		if (value)
		{
			Player.mover.SetAnimation("turn_back", loop: false);
			Player.mover.AddAnimation("idle_back");
		}
		else
		{
			Player.mover.SetAnimation("turn_front", loop: false);
			Player.mover.AddAnimation("idle");
		}
	}

	public void PlayerLookUp()
	{
		Player.mover.SetAnimation("cam_up");
	}

	public void PlayerLookIdle()
	{
		Player.mover.SetAnimation("idle");
	}

	public void StopBGM()
	{
		SoundManager.FadeOutBGM(2f);
	}

	public virtual void PlayBGM(string soundKey = null)
	{
		SoundManager.FadeInBGM(soundKey, 2f);
	}

	public string ControllerName()
	{
		return ID;
	}

	public UniTask Activate()
	{
		SkipBoard.Initialize(0.5f);
		return UniTask.CompletedTask;
	}

	public virtual void Control()
	{
		if (!SkipCondition)
		{
			return;
		}
		bool menu = InputManager.menu;
		bool flag = IsTalking && InputManager.confirmDown;
		bool flag2 = CanSkip();
		if (!menu && ReInput.controllers.GetAnyButtonDown() && !flag && flag2)
		{
			SkipBoard.Activate();
		}
		if (flag2 && SkipBoard.CheckInput(menu, ignoreTimescale: true))
		{
			UniTask task = SkipTimelineAsync();
			InputManager.BlockControlForTask(task, "SkipTimeline").Forget();
			if (IsTalking)
			{
				TalkCanvas.DeactivateOnSkipCutscene();
			}
		}
		else if (IsTalking)
		{
			TalkCanvas.Control();
		}
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		SkipBoard.Deactivate();
		return UniTask.CompletedTask;
	}

	private bool CanSkip()
	{
		if (UnskippableCutscenes.Contains(CurrentState))
		{
			return false;
		}
		return Game.IsWatchedCutsceneInGlobal(ID, currentState);
	}
}
