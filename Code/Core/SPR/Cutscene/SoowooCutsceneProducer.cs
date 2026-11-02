using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class SoowooCutsceneProducer : BossCutsceneProducer
{
	private class SoowooCutscenePositionData
	{
		public readonly Vector2 PlayerPosition = CutsceneProducer.Player.transform.position;

		public readonly Direction PlayerDirection = CutsceneProducer.Player.mover.FacingDirection;

		public readonly Vector3 CameraPosition;

		public SoowooCutscenePositionData(CinemachineVirtualCamera endVcam)
		{
			CameraPosition = endVcam.transform.position;
		}
	}

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset phaseTransitionTimeline;

	[SerializeField]
	private PlayableAsset endingTimeline;

	[SerializeField]
	private PlayableAsset waterfallTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingCam;

	[Header("Phase Transition")]
	[SerializeField]
	private CinemachineVirtualCamera phaseStartCam;

	[SerializeField]
	private CinemachineVirtualCamera phaseEndCam;

	[SerializeField]
	private Transform phaseStartPosition;

	[SerializeField]
	private Transform phaseDivePosition;

	[SerializeField]
	private Transform phaseLandPosition;

	[SerializeField]
	private List<Transform> alignWithDivePosition;

	[SerializeField]
	private List<Transform> alignWithLandPosition;

	[SerializeField]
	private float roarShakeAmplitude;

	[SerializeField]
	private float roarShakeFrequency;

	[SerializeField]
	private float roarShakeDuration;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera endingEndCam;

	[SerializeField]
	private List<CinemachineVirtualCamera> endingCams;

	[SerializeField]
	private Transform endingPosition;

	[SerializeField]
	private Transform endingPlayerPosition;

	[SerializeField]
	private List<Transform> alignWithEndingPosition;

	[Header("After WaterFall")]
	[SerializeField]
	private CinemachineVirtualCamera waterfallStartCam;

	[SerializeField]
	private CinemachineVirtualCamera waterfallEndCam;

	[Header("Platform")]
	[SerializeField]
	private Transform entrancePlatform;

	[SerializeField]
	private Transform platformOn;

	[SerializeField]
	private Transform platformOff;

	[SerializeField]
	private AreaBox areaBox;

	[SerializeField]
	private GameObject transparentWall;

	[Header("References")]
	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private List<GameObject> itemBoxObject;

	[SerializeField]
	private List<PositionDirectionSaver> corpses;

	[Header("Variables")]
	[SerializeField]
	private float centerXPos;

	[SerializeField]
	private float directionFlipXBorder;

	private static SoowooCutscenePositionData positionData;

	private CancellationTokenSource playerLookBackCancel = new CancellationTokenSource();

	private BoxCollider2D leftPlatformCollider;

	private BoxCollider2D rightPlatformCollider;

	private bool isSetPath;

	private List<Vector2> pathRange;

	protected override bool SkipCondition
	{
		get
		{
			if (base.SkipCondition)
			{
				return CutsceneProducer.FadeCanvas.IsWhite;
			}
			return false;
		}
	}

	protected override void SetProducerState(int state)
	{
		bool flag = state == 0 || state == 1 || state == 4;
		entrancePlatform.SynchronizePositionDirection(flag ? platformOn : platformOff);
		ModifyEntrancePlatformPath(flag);
		switch (state)
		{
		case 0:
		case 1:
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
			itemBoxObject.ForEach(delegate(GameObject obj)
			{
				obj.SetActive(value: false);
			});
			break;
		case 4:
			transparentWall.SetActive(value: false);
			ActivateCorpse();
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			transparentWall.SetActive(value: true);
			Boss.CastObject<Soowoo>().SetArea(areaBox);
			base.PlayableDirector.playableAsset = openingTimeline;
			await BlendFieldCamToCutsceneStartCamAsync(openingCam);
			base.BossSpine.SetAnimation("bubble_idle", 1);
			PlayerLookBackAsync(TaskUtil.RefreshToken(ref playerLookBackCancel)).Forget();
			break;
		case 2:
			transparentWall.SetActive(value: false);
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
			endingCams.ForEach(base.MoveCutsceneStartCamToCurrentCam);
			break;
		case 3:
			transparentWall.SetActive(value: false);
			base.PlayableDirector.playableAsset = waterfallTimeline;
			AdjustPositionsWaterfall();
			SustainVcamForFade(waterfallStartCam);
			CutsceneProducer.SoundManager.PlayAMB(base.FightArea.SoundKey);
			itemBoxObject.ForEach(delegate(GameObject obj)
			{
				obj.SetActive(value: true);
			});
			await CutsceneProducer.FadeCanvas.FadeIn(1f, 0f, Ease.Linear, token);
			break;
		}
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			SwitchToRealBoss();
			Boss.SynchronizeAnimation(base.BossSpine, isImmediate: true, null, 1);
			SynchronizeEndVcamAsync(openingCam).Forget();
			entrancePlatform.SynchronizePositionDirection(platformOff);
			ModifyEntrancePlatformPath(setGround: false);
			StartFight();
			playerLookBackCancel.Cancel();
			PlayerLookBack(value: false);
			break;
		case 2:
			SavePositionData();
			SustainVcamForFade(endingEndCam);
			Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.WatermirrorBasin, 25), "Waterfall")).Forget();
			CutsceneProducer.SoundManager.StopAMB();
			break;
		case 3:
			waterfallStartCam.gameObject.SetActive(value: false);
			SynchronizeEndVcamAsync(waterfallEndCam).Forget();
			entrancePlatform.SynchronizePositionDirection(platformOn);
			ModifyEntrancePlatformPath(setGround: true);
			CutsceneProducer.Field.ChangeAreaTo(CutsceneProducer.Field.MainArea);
			GetReward().Forget();
			break;
		case 1:
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
			break;
		case 2:
			DisableFadeInAfterSkip();
			break;
		}
	}

	public override bool ShouldLoop(int index)
	{
		_ = base.CurrentState;
		_ = 2;
		return false;
	}

	public void FacePlayer()
	{
		FlipBossToPlayer();
	}

	public void SetImmortal(bool isImmortal)
	{
		Boss.IsImmortal = isImmortal;
	}

	public void ShakeCameraLand()
	{
		ShakeCameraForSeconds(5f, 2f, 0.25f);
	}

	public void ShakeCameraRoar()
	{
		ShakeCameraForSeconds(roarShakeAmplitude, roarShakeFrequency, roarShakeDuration);
	}

	public void MovePlayerEnding()
	{
		CutsceneProducer.InputManager.StackControlTarget(this).Forget();
		MovePlayerEndingAsync().Forget();
	}

	private async UniTask MovePlayerEndingAsync()
	{
		await CutsceneProducer.Player.MoveXPositionAsync(endingPlayerPosition.position.x, isRunning: true);
		CutsceneProducer.Player.mover.Flip(bossFigure.position.x - CutsceneProducer.Player.transform.position.x);
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.GetCurrentAnim(0).Name.Equals("idle"));
		PlayerLookUp();
	}

	private async UniTask PlayerLookBackAsync(CancellationToken token)
	{
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround && CutsceneProducer.Player.mover.GetCurrentAnim(0).Name.Equals("idle"), PlayerLoopTiming.Update, token);
		PlayerLookBack(value: true);
	}

	private void AdjustPositionsEnding()
	{
		float x = ((Mathf.Abs(Boss.transform.position.x - centerXPos) > directionFlipXBorder) ? centerXPos : CutsceneProducer.Player.transform.position.x);
		Direction direction = FlipBossToTarget(x);
		float diffX = Boss.transform.position.x - endingPosition.position.x;
		endingPosition.position = Boss.transform.position;
		BossCutsceneProducer.AlignTransformXs(alignWithEndingPosition, diffX, direction == Direction.Right, endingPosition.position.x);
	}

	private void SavePositionData()
	{
		PositionDirectionSaver[] componentsInChildren = bossCorpse.GetComponentsInChildren<PositionDirectionSaver>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Save();
		}
		positionData = new SoowooCutscenePositionData(endingEndCam);
	}

	private void AdjustPositionsWaterfall()
	{
		CutsceneProducer.Player.SetPosition(positionData.PlayerPosition, positionData.PlayerDirection == Direction.Right);
		ActivateCorpse();
		waterfallStartCam.transform.position = positionData.CameraPosition;
		positionData = null;
	}

	private void ModifyEntrancePlatformPath(bool setGround)
	{
		if (setGround == isSetPath)
		{
			return;
		}
		if (pathRange == null)
		{
			if ((object)leftPlatformCollider == null)
			{
				leftPlatformCollider = entrancePlatform.GetComponent<BoxCollider2D>();
			}
			if ((object)rightPlatformCollider == null)
			{
				rightPlatformCollider = entrancePlatform.GetChild(0).GetComponent<BoxCollider2D>();
			}
			Vector2 size = leftPlatformCollider.size;
			Vector2 offset = leftPlatformCollider.offset;
			Vector2 vector = (Vector2)platformOn.transform.position + offset + Vector2.up * size.y / 2f;
			Vector2 vector2 = vector + Vector2.left * size.x / 2f;
			Vector2 vector3 = vector + Vector2.right * size.x / 2f;
			pathRange = new List<Vector2>();
			for (float num = vector2.x; num <= vector3.x; num += 1f)
			{
				pathRange.Add(new Vector2(num, vector.y));
			}
			vector = (Vector2)platformOn.transform.position + (Vector2)rightPlatformCollider.transform.localPosition + offset + Vector2.up * size.y / 2f;
			Vector2 vector4 = vector + Vector2.left * size.x / 2f;
			vector3 = vector + Vector2.right * size.x / 2f;
			for (float num2 = vector4.x; num2 <= vector3.x; num2 += 1f)
			{
				pathRange.Add(new Vector2(num2, vector.y));
			}
		}
		Game.Instance.FieldController.ModifyPathGraphs(pathRange, setGround);
		isSetPath = setGround;
	}

	private async UniTaskVoid GetReward()
	{
		await UniTask.WaitForSeconds(1f);
		new PureItem("SoowooSpirit").AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanel);
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.InteractionPanel.IsDisplaying);
		CutsceneProducer.Player.PlayerStat.AddStatAppliedExperience(Boss.Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		EarnMoney();
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.LevelBoard.IsLevelUpProducing);
		new PureItem("RoyalSealPremium").AddToPlayerData(ItemAcquireDisplayOrder.ShowNotification);
		ShowResonance();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(areaBox.Center, areaBox.Size.ToVector3());
	}
}
