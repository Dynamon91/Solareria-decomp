using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Spine;
using Spine.Unity;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class PrimordialFlameCutsceneProducer : BossCutsceneProducer
{
	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset phaseTwoTimeline;

	[SerializeField]
	private PlayableAsset phaseThreeTimeline;

	[SerializeField]
	private PlayableAsset dyingTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingCam;

	[SerializeField]
	private Transform openingTransform;

	[Header("Phase Two")]
	[SerializeField]
	private CinemachineVirtualCamera phaseTwoStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera phaseTwoEndVcam;

	[SerializeField]
	private Transform phaseTwoStartPosition;

	[SerializeField]
	private List<Transform> phaseTwoPositions;

	[SerializeField]
	private float phase2PositionOffsetX;

	[SerializeField]
	private Transform disappearEffect;

	[Header("Phase Three")]
	[SerializeField]
	private CinemachineVirtualCamera phaseThreeCam;

	[SerializeField]
	private Transform phaseThreeBossStartPosition;

	[SerializeField]
	private Transform phaseThreeBossEndPosition;

	[SerializeField]
	private Transform phaseThreePlayerEndPosition;

	[SerializeField]
	private List<Transform> phaseThreePositions;

	[SerializeField]
	private int knockBackDuration;

	[SerializeField]
	private float knockBackForce;

	[Header("Dying")]
	[SerializeField]
	private CinemachineVirtualCamera dyingCam;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingStartCam;

	[SerializeField]
	private Transform endingStartPlayerPosition;

	[SerializeField]
	private Transform endingStartBossPosition;

	[SerializeField]
	private List<Transform> endingStartPositions;

	[Header("References")]
	[SerializeField]
	private Transform playerFigure;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private Transform phase2BossFigure;

	[SerializeField]
	private GameObject solines;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private List<GameObject> solineSmokeList;

	[Header("Disk")]
	[SerializeField]
	private List<GameObject> disks;

	[SerializeField]
	private float rotateSpeed;

	private SkeletonAnimation playerSpine;

	private PrimordialFlame2 phase2Boss;

	private readonly List<Tween> diskTweenList = new List<Tween>();

	private int enteredState;

	protected override float KnockBackHoldTime => 3f;

	private SkeletonAnimation PlayerSpine => playerSpine ?? (playerSpine = playerFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	private SkeletonAnimation Phase2BossSpine => phase2BossFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true);

	protected override BossCharacter Boss
	{
		get
		{
			int num = base.CurrentState;
			object obj;
			if (num != 1 && num != 3 && num != 5 && num >= 2)
			{
				obj = phase2Boss;
				if (obj == null)
				{
					return phase2Boss = base.MyArea.GetComponentInChildren<PrimordialFlame2>(includeInactive: true);
				}
			}
			else
			{
				obj = base.Boss;
			}
			return (BossCharacter)obj;
		}
	}

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			foreach (GameObject transparentWall in transparentWalls)
			{
				transparentWall.transform.gameObject.SetActive(value: false);
			}
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
			SetBossFigureWait();
			RotateDisk();
			enteredState = state;
			break;
		case 1:
		case 3:
		case 5:
			foreach (GameObject transparentWall2 in transparentWalls)
			{
				transparentWall2.transform.gameObject.SetActive(value: false);
			}
			cutsceneTrigger.OnTriggered += delegate
			{
				foreach (GameObject transparentWall3 in transparentWalls)
				{
					transparentWall3.transform.gameObject.SetActive(value: true);
				}
				ChangeArea();
				Boss.ExitCutsceneState(bossFigure);
				bossFigure.gameObject.SetActive(value: false);
				EndTimeline();
				StartFight();
				ChangeState(state);
			};
			RotateDisk();
			enteredState = state;
			break;
		case 2:
		case 4:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			await CutsceneProducer.Player.MoveXPositionAsync(openingTransform.position.x, isRunning: true, token);
			await BlendFieldCamToCutsceneStartCamAsync(openingCam, 0.6f);
			break;
		case 2:
			SwitchToBossFigure(bossFigure, base.Boss);
			phase2BossFigure.transform.Flip(base.Boss.FacingDirection.GetSignX());
			base.PlayableDirector.playableAsset = phaseTwoTimeline;
			BlendFieldCamToCutsceneStartCamAsync(phaseTwoStartVcam).Forget();
			CutsceneProducer.Player.StatusEffectController.SetEffectLoop(active: false);
			break;
		case 4:
			Boss.LookAt(CutsceneProducer.Player.transform.position);
			AdjustPositionsPhaseThree();
			base.PlayableDirector.playableAsset = phaseThreeTimeline;
			Boss.IsImmortal = false;
			Boss.gameObject.SetActive(value: true);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", phase2Boss.SkeletonAnimation);
			base.PlayableDirector.TrackBinding<AnimationTrack>("Animation Track", phase2Boss.SkeletonAnimation.GetComponent<Animator>());
			solines.gameObject.SetActive(value: true);
			CutsceneProducer.Player.StatusEffectController.SetEffectLoop(active: false);
			break;
		case 6:
			DisableSolineEffect();
			AdjustPositionsEnding();
			base.PlayableDirector.playableAsset = dyingTimeline;
			ClearFight(updateMemory: false, stopBgm: true, getHaechiReward: false);
			FlipBossToPlayer();
			SwitchToBossFigure(phase2BossFigure, phase2Boss);
			MoveCutsceneStartCamToCurrentCam(dyingCam);
			if (!Mathf.Approximately(phase2BossFigure.position.y, phase2Boss.SpawnPosition.y))
			{
				phase2BossFigure.DOMoveY(phase2Boss.SpawnPosition.y, 0.5f).SetEase(Ease.Linear);
			}
			break;
		}
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			foreach (GameObject transparentWall in transparentWalls)
			{
				transparentWall.transform.gameObject.SetActive(value: true);
			}
			SynchronizeEndVcamAsync(openingCam).Forget();
			Boss.gameObject.SetActive(value: true);
			Boss.SynchronizeAnimation(base.BossSpine);
			base.BossSpine.Skeleton.UpdateWorldTransform(Skeleton.Physics.Reset);
			Boss.SkeletonAnimation.Skeleton.UpdateWorldTransform(Skeleton.Physics.Reset);
			bossFigure.gameObject.SetActive(value: false);
			Boss.ExitCutsceneState(bossFigure);
			CutsceneProducer.Player.transform.position = openingTransform.position;
			StartFight();
			PlayerLookIdle();
			break;
		case 2:
			SwitchToPhase2Boss();
			SynchronizeEndVcamAsync(phaseTwoEndVcam).Forget();
			CutsceneProducer.Player.StatusEffectController.SetEffectLoop(active: true);
			break;
		case 4:
			ExitBossCutsceneState();
			CutsceneProducer.Player.StatusEffectController.SetEffectLoop(active: true);
			break;
		case 6:
		{
			base.BossSpine.SynchronizeAnimation(base.BossSpine);
			UniTask task = PlayEndingNormalOrTrueAsync();
			CutsceneProducer.InputManager.BlockControlForTask(task, "Primordial Flame Timeline Dying to Ending").Forget();
			EarnMoney();
			ChangeState(enteredState - 1);
			break;
		}
		case 1:
		case 3:
		case 5:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		switch (state)
		{
		case 0:
			ChangeArea();
			CutsceneProducer.FieldCanvas.NarrationBoard.FadeOutNow(0f);
			break;
		case 2:
			CutsceneProducer.Player.mover.PSM.ChangeState(CutsceneProducer.Player.mover.PSM.IdleState);
			PlayBGMPhase2();
			CutsceneProducer.FieldCanvas.NarrationBoard.FadeOutNow(0f);
			break;
		case 4:
			UseWarmhorn();
			break;
		case 1:
		case 3:
			break;
		}
	}

	private async UniTask PlayEndingNormalOrTrueAsync()
	{
		Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.WorldsEnd, 90), "Ending")).Forget();
	}

	public void RunPlayerAtEnding()
	{
		SwitchToRealPlayer();
		MovePlayerAtEndingAsync().Forget();
	}

	public void SwitchToPlayerFigure()
	{
		playerFigure.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		PlayerSpine.SynchronizeAnimation(CutsceneProducer.Player.mover.SkeletonAnimation);
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		playerFigure.gameObject.SetActive(value: true);
	}

	public void SwitchToRealPlayer()
	{
		CutsceneProducer.Player.mover.SynchronizeAnimation(PlayerSpine);
		SynchronizePlayer(playerFigure);
	}

	public void FlipBothBossPlayer()
	{
		FlipFigureToPlayer();
		FlipPlayerToFigure();
	}

	public void GuardKnockBackPlayer()
	{
		Vector3 direction = (((CutsceneProducer.Player.mover.VisualBottom - Boss.VisualBottom).x < 0f) ? Vector3.left : Vector3.right);
		StompForce.ApplyStompForce(CutsceneProducer.Player.gameObject, knockBackDuration, knockBackForce, direction);
	}

	public void StuckDisk()
	{
		foreach (Tween diskTween in diskTweenList)
		{
			diskTween.Kill();
		}
		diskTweenList.Clear();
		for (int i = 0; i < disks.Count; i++)
		{
			GameObject gameObject = disks[i];
			Vector3 endValue = ((i % 2 == 0) ? new Vector3(0f, 0f, gameObject.transform.eulerAngles.z + 10f) : new Vector3(0f, 0f, gameObject.transform.eulerAngles.z - 10f));
			Vector3 endValue2 = new Vector3(0f, 0f, gameObject.transform.eulerAngles.z);
			DG.Tweening.Sequence sequence = DOTween.Sequence();
			sequence.Append(gameObject.transform.DORotate(endValue, 1f).SetEase(Ease.InOutBounce)).Append(gameObject.transform.DORotate(endValue2, 2f).SetEase(Ease.InOutBounce)).SetLoops(-1);
			diskTweenList.Add(sequence);
		}
	}

	public void PlayBGMPhase2()
	{
		CutsceneProducer.SoundManager.PlayBGM("BossPrimordialFlamePhase2");
	}

	public void Teleport()
	{
		float num = (float)base.FightArea.AreaBox.Size.x / 3f;
		float num2 = (float)base.FightArea.AreaBox.LowerLeft.x + num;
		float num3 = (float)base.FightArea.AreaBox.UpperRight.x - num;
		if (Boss.Position.x > num2 && bossFigure.position.x < num3)
		{
			if (Mathf.Abs((float)base.FightArea.AreaBox.LowerLeft.x - CutsceneProducer.Player.mover.Position.x) > Mathf.Abs((float)base.FightArea.AreaBox.UpperRight.x - CutsceneProducer.Player.mover.Position.x))
			{
				bossFigure.position = new Vector2(num2 - num / 2f, bossFigure.position.y);
			}
			else
			{
				bossFigure.position = new Vector2(num3 + num / 2f, bossFigure.position.y);
			}
		}
		else
		{
			bossFigure.position = new Vector2(base.FightArea.AreaBox.Center.x, Boss.SpawnPosition.y);
		}
		int sign = (CutsceneProducer.Player.mover.Position.x - bossFigure.transform.position.x).GetSign();
		bossFigure.Flip(sign);
		phase2BossFigure.Flip(sign);
		AdjustPositionsPhaseTwo();
		List<Transform> list = phaseTwoPositions;
		list[list.Count - 1].Flip(sign);
	}

	public void EnableSolineEffect()
	{
		foreach (GameObject solineSmoke in solineSmokeList)
		{
			solineSmoke.SetActive(value: true);
		}
	}

	public void UseWarmhorn()
	{
		phase2Boss.UseWarmhorn();
	}

	public void Disappear()
	{
		disappearEffect.transform.position = new Vector3(bossFigure.position.x, disappearEffect.transform.position.y);
	}

	private void SetBossFigureWait()
	{
		bossFigure.gameObject.SetActive(value: true);
		base.BossSpine.SetAnimation("scene/op_idle_back");
	}

	private void AdjustPositionsPhaseTwo()
	{
		float diffX = bossFigure.transform.position.x - phaseTwoStartPosition.position.x + (float)bossFigure.transform.localScale.x.GetSign() * phase2PositionOffsetX;
		BossCutsceneProducer.AlignTransformXs(phaseTwoPositions, diffX, flipCondition: false);
	}

	private void AdjustPositionsPhaseThree()
	{
		float diffX = phase2Boss.transform.position.x - phaseThreeBossStartPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseThreePositions, diffX, flipCondition: false);
	}

	private void AdjustPositionsEnding()
	{
		float diffX = phase2Boss.transform.position.x - endingStartBossPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingStartPositions, diffX, flipCondition: false);
	}

	private async UniTask MovePlayerAtEndingAsync()
	{
		await CutsceneProducer.Player.MoveXPositionAsync(CutsceneProducer.Player.mover.VisualBottom.x + 8.5f * (float)CutsceneProducer.Player.mover.FacingDirection.GetSignX(), isRunning: true);
		SwitchToPlayerFigure();
	}

	private void RotateDisk()
	{
		for (int i = 0; i < disks.Count; i++)
		{
			GameObject gameObject = disks[i];
			Vector3 endValue = ((i % 2 == 0) ? new Vector3(0f, 0f, 360f) : new Vector3(0f, 0f, -360f));
			diskTweenList.Add(gameObject.transform.DORotate(endValue, rotateSpeed, DG.Tweening.RotateMode.FastBeyond360).SetSpeedBased(isSpeedBased: true).SetLoops(-1)
				.SetEase(Ease.Linear));
		}
	}

	private void SwitchToPhase2Boss()
	{
		Boss.SynchronizeAnimation(Phase2BossSpine);
		Boss.ExitCutsceneState(phase2BossFigure);
		phase2BossFigure.gameObject.SetActive(value: false);
	}

	private void DisableSolineEffect()
	{
		foreach (GameObject solineSmoke in solineSmokeList)
		{
			solineSmoke.GetComponentInChildren<ParticleSystem>().Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	private void OnDestroy()
	{
		foreach (Tween diskTween in diskTweenList)
		{
			diskTween.Kill();
		}
	}
}
