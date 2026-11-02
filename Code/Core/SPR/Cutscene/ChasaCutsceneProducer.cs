using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Field;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class ChasaCutsceneProducer : CutsceneProducer
{
	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset openingYesTimeline;

	[SerializeField]
	private PlayableAsset openingNoTimeline;

	[SerializeField]
	private PlayableAsset firstEventTimeline;

	[SerializeField]
	private PlayableAsset clearTimeline;

	[SerializeField]
	private PlayableAsset playerDefeatTimeline;

	[Header("Opening Yes")]
	[SerializeField]
	private CinemachineVirtualCamera openingYesStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera openingYesEndVcam;

	[SerializeField]
	private Transform chasaTalkPosition;

	[Header("Opening No")]
	[SerializeField]
	private CinemachineVirtualCamera openingNoStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera openingNoEndVcam;

	[Header("First Event")]
	[SerializeField]
	private CinemachineVirtualCamera firstEventStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera firstEventEndVcam;

	[SerializeField]
	private Transform playerBlockPosition;

	[SerializeField]
	private Transform chasaDashPosition;

	[SerializeField]
	private List<Transform> chasaDashParticles;

	[Header("Player Defeat")]
	[SerializeField]
	private CinemachineVirtualCamera playerDefeatStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera playerDefeatEndVcam;

	[SerializeField]
	private Transform defeatPlayerStartPosition;

	[SerializeField]
	private Transform defeatPlayerRecoveryPosition;

	[SerializeField]
	private Transform defeatChasaStartPosition;

	[SerializeField]
	private Transform defeatChasaRecoveryPosition;

	[SerializeField]
	private List<Transform> defeatChasaStartParticles;

	[SerializeField]
	private List<Transform> defeatChasaRecoveryParticles;

	[SerializeField]
	private EventReference maskDropEvent;

	[Header("Clear")]
	[SerializeField]
	private CinemachineVirtualCamera clearVcam;

	[SerializeField]
	private List<Transform> clearVcams;

	[SerializeField]
	private Transform clearPlayerStartPosition;

	[SerializeField]
	private Transform clearPlayerEndPosition;

	[SerializeField]
	private Transform clearChasaStartPosition;

	[SerializeField]
	private Transform clearChasaEndPosition;

	[SerializeField]
	private List<Transform> clearChasaStartParticles;

	[SerializeField]
	private List<Transform> clearChasaEndParticles;

	[Header("Start By Dummy")]
	[SerializeField]
	private Transform playerFightPosition;

	[SerializeField]
	private Transform chasaFightPosition;

	[SerializeField]
	private SpriteRenderer fadeImageMain;

	[SerializeField]
	private SpriteRenderer fadeImageSub;

	[SerializeField]
	private ParticleSystem playerTeleportToParticle;

	[SerializeField]
	private ParticleSystem chasaTeleportToParticle;

	[SerializeField]
	private ParticleSystem headVfx1;

	[SerializeField]
	private ParticleSystem headVfx2;

	[SerializeField]
	private EventReference disappearEvent;

	[SerializeField]
	private EventReference teleportEvent;

	[Header("References")]
	[SerializeField]
	private Transform fakeChasa;

	[SerializeField]
	private SkeletonAnimation fakeChasaSpine;

	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private SkeletonAnimation fakePlayerSpine;

	[SerializeField]
	private List<ChasaCutsceneTrigger> cutsceneTriggers;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private ParticleSystem ashFireParticle;

	[SerializeField]
	private List<GameObject> playerRightEffectParents;

	[SerializeField]
	private List<GameObject> playerLeftEffectParents;

	[Header("Variables")]
	[SerializeField]
	private int firstEventWaitingMilliSeconds;

	[SerializeField]
	private float maxVcamPositionX;

	[SerializeField]
	private float maxPlayerPositionsX;

	private const int OpeningYesState = 0;

	private const int OpeningNoState = 1;

	private const int AfterOpeningNoState = 2;

	private const int FirstEventState = 3;

	private const int PlayerDefeatState = 4;

	private const int AfterPlayerDefeatState = 5;

	private const int ClearState = 6;

	private const int AfterClearState = 7;

	private BossChasa chasaBoss;

	private Chasa chasaNpc;

	private Area originArea;

	private Area fightArea;

	private Dummy[] dummies;

	private MeshRenderer fakePlayerRenderer;

	private MeshRenderer fakeChasaRenderer;

	private bool isDefeatPositionsFacingRight = true;

	private float defeatEndVcamExcessX;

	private float ashFireInitialRate = -1f;

	private float ashFireInitialSpeed = -1f;

	private bool isAshFireInitialized;

	private bool retryRightAfterDefeat;

	private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

	private static readonly int AlphaID = Shader.PropertyToID("_Alpha");

	private static readonly int DistortionAmountID = Shader.PropertyToID("_DistortionAmount");

	private static readonly int GlitchAmountID = Shader.PropertyToID("_GlitchAmount");

	private static readonly int GlitchSizeID = Shader.PropertyToID("_GlitchSize");

	private static readonly int HeadParticle = int.MinValue;

	private ParticleSystemRenderer ashFireRenderer;

	private float fightTime;

	private static readonly Dictionary<int, (float active, float inactive)> MpbDictionary = new Dictionary<int, (float, float)>
	{
		{
			AlphaID,
			(1f, 0f)
		},
		{
			DistortionAmountID,
			(1.6f, -40f)
		},
		{
			GlitchAmountID,
			(0f, 20f)
		},
		{
			GlitchSizeID,
			(0.25f, 3f)
		},
		{
			HeadParticle,
			(1f, 0f)
		}
	};

	private BossChasa ChasaBoss => chasaBoss ?? (chasaBoss = FightArea.GetComponentInChildren<BossChasa>(includeInactive: true));

	private Chasa ChasaNpc => chasaNpc ?? (chasaNpc = FightArea.GetComponentInChildren<Chasa>(includeInactive: true));

	private Area FightArea => fightArea ?? (fightArea = GetComponentInParent<Area>());

	private Dummy[] Dummies => dummies ?? (dummies = FightArea.GetComponentsInChildren<Dummy>());

	private MeshRenderer FakePlayerRenderer => fakePlayerRenderer ?? (fakePlayerRenderer = fakePlayerSpine.GetComponent<MeshRenderer>());

	private MeshRenderer FakeChasaRenderer => fakeChasaRenderer ?? (fakeChasaRenderer = fakeChasaSpine.GetComponent<MeshRenderer>());

	private static BossName BossName => BossName.Chasa;

	private static bool IsChasaNotFirst => CutsceneProducer.EnemyFightData.GetBossTryCount(BossName) > 0;

	public bool IsFighting => fightTime > 0f;

	protected override void SetProducerState(int state)
	{
		InitializeAshFire();
		fakePlayerSpine.Skeleton.SetSkin(CutsceneProducer.Player.mover.SkeletonAnimation.Skeleton.Skin.Name);
		cutsceneTriggers.ForEach(delegate(ChasaCutsceneTrigger trigger)
		{
			trigger.gameObject.SetActive(value: false);
		});
		switch (state)
		{
		case 2:
		case 5:
			Dummies.ForEach(delegate(Dummy d)
			{
				d.ChangeState(1);
			});
			break;
		case 7:
			Dummies.ForEach(delegate(Dummy d)
			{
				d.ChangeState(0);
			});
			ChasaNpc.gameObject.SetActive(value: false);
			break;
		}
		if ((uint)(state - 3) > 1u)
		{
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingYesTimeline;
			await SetUpStartTimelineAsync(openingYesStartVcam, chasaTalkPosition);
			SwitchToFakeSpine(ChasaNpc, fakeChasa, fakeChasaSpine);
			break;
		case 1:
			base.PlayableDirector.playableAsset = openingNoTimeline;
			await SetUpStartTimelineAsync(openingNoStartVcam, chasaTalkPosition);
			SwitchToFakeSpine(ChasaNpc, fakeChasa, fakeChasaSpine);
			ChasaNpc.DisableTalkVcam();
			break;
		case 3:
			base.PlayableDirector.playableAsset = firstEventTimeline;
			SetChasaMask(value: true);
			SwitchChasaEffectDirection(CutsceneProducer.Player.mover.IsFacingRight);
			break;
		case 4:
			base.PlayableDirector.playableAsset = playerDefeatTimeline;
			AdjustPositionsOnPlayerDefeat();
			EnterRealCutscene();
			LandPlayerOnGround();
			await BlendFieldCamToCutsceneStartCamAsync(playerDefeatStartVcam);
			CutsceneProducer.Player.StatusEffectController.Clear();
			break;
		case 6:
		{
			ChasaBoss.ClearStatusEffect(force: true);
			base.PlayableDirector.playableAsset = clearTimeline;
			SetPlayerMaterial();
			ClearFight();
			CutsceneProducer.SoundManager.StopBGM();
			bool isFacingRight = ChasaBoss.transform.position.x >= CutsceneProducer.Player.transform.position.x;
			AdjustBossSpineOnClear(isFacingRight);
			await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround);
			AdjustPositionsOnClear(isFacingRight);
			await SetUpStartTimelineAsync(clearVcam, clearChasaStartPosition, clearPlayerStartPosition);
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			CutsceneProducer.Player.StatusEffectController.Clear();
			break;
		}
		}
		await UniTask.Yield();
	}

	private async UniTask SetUpStartTimelineAsync(CinemachineVirtualCamera startVcam, Transform spineTarget = null, Transform fakePlayerTarget = null)
	{
		await BlendFieldCamToCutsceneStartCamAsync(startVcam);
		if (BasicExtension.IsNull(fakePlayerTarget))
		{
			fakePlayer.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		}
		else
		{
			fakePlayer.SynchronizePositionDirection(fakePlayerTarget);
		}
		if (!BasicExtension.IsNull(spineTarget))
		{
			fakeChasa.SynchronizePositionDirection(spineTarget);
		}
		fakePlayerSpine.SynchronizeAnimation(CutsceneProducer.Player.mover.SkeletonAnimation);
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		fakePlayer.gameObject.SetActive(value: true);
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			FinishEndTimeline(openingYesEndVcam);
			WaitAndStartEventAsync().Forget();
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			break;
		case 1:
			fakeChasa.gameObject.SetActive(value: false);
			Dummies.ForEach(delegate(Dummy d)
			{
				d.ChangeState(0);
			});
			FinishEndTimeline(openingNoEndVcam);
			break;
		case 3:
			fakeChasa.gameObject.SetActive(value: false);
			SetChasaMask(value: true);
			ChasaBoss.SynchronizeAnimation(fakeChasaSpine);
			ChasaBoss.ExitCutsceneState(fakeChasa);
			FinishEndTimeline(firstEventEndVcam);
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			StartFight();
			break;
		case 4:
			if (retryRightAfterDefeat)
			{
				if (!IsChasaNotFirst)
				{
					CutsceneProducer.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(BossName, isClear: false);
				}
				AddFightNum();
				CutsceneProducer.FieldCanvas.BossBoard.HideAsync(0.6f, deactivate: true).Forget();
				FightArea.Vcam.gameObject.SetActive(value: false);
				SustainVcamForFade(playerDefeatEndVcam);
				retryRightAfterDefeat = false;
				break;
			}
			if (!IsChasaNotFirst)
			{
				CutsceneProducer.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(BossName, isClear: false);
			}
			AddFightNum();
			CutsceneProducer.Field.ChangeAreaTo(originArea);
			fakeChasa.gameObject.SetActive(value: false);
			ChasaNpc.gameObject.SetActive(value: true);
			ChasaNpc.SetEventSfx(value: true);
			CutsceneProducer.FieldCanvas.BossBoard.HideAsync(0.6f, deactivate: true).Forget();
			FinishEndTimeline(playerDefeatEndVcam);
			FlipFakePlayer(isRight: true);
			Game.Instance.RuntimeData.ShowStatusEffectTutorial().Forget();
			Dummies.ForEach(delegate(Dummy d)
			{
				d.ChangeState(1);
			});
			break;
		case 6:
			AddFightNum();
			CutsceneProducer.Field.ChangeAreaTo(originArea);
			fakeChasa.gameObject.SetActive(value: false);
			Dummies.ForEach(delegate(Dummy d)
			{
				d.ChangeState(0);
			});
			FinishEndTimeline(clearVcam);
			GiveReward();
			break;
		case 2:
		case 5:
			break;
		}
	}

	private void FinishEndTimeline(CinemachineVirtualCamera endVcam)
	{
		CutsceneProducer.Player.mover.SynchronizeAnimation(fakePlayerSpine);
		SynchronizePlayer(fakePlayer);
		SynchronizeEndVcamAsync(endVcam).Forget();
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		fakePlayerSpine.SetAnimationInstantly("idle");
		switch (state)
		{
		case 0:
			ChangeArea();
			fakePlayer.SynchronizePositionDirection(playerFightPosition);
			fakeChasa.SynchronizePositionDirection(chasaFightPosition);
			fakeChasaSpine.SetAnimationInstantly("scene/battle_ready_b_loop");
			break;
		case 3:
			fakeChasa.SynchronizePositionDirection(chasaFightPosition);
			ActivateAshFire();
			PlayBGM(FightArea.SoundKey);
			break;
		case 6:
			fakePlayer.SynchronizePositionDirection(clearPlayerEndPosition);
			DeactivateAshFire();
			break;
		case 4:
			HealPlayer();
			LandPlayerOnGround();
			fakePlayer.SynchronizePositionDirection(defeatPlayerRecoveryPosition);
			DeactivateAshFire();
			break;
		case 1:
		case 2:
		case 5:
			break;
		}
	}

	public override bool TryCutsceneOnPlayerDie()
	{
		if (!IsProducing)
		{
			CutsceneProducer.Player.PlayerStat.GetStat<Health>().AddHealth(1);
			CutsceneProducer.Player.mover.Flip(-CutsceneProducer.Player.mover.LastHitDirection);
			PlayerStateMachine pSM = CutsceneProducer.Player.mover.PSM;
			pSM.ChangeState(pSM.DownState);
			pSM.SetStateOptionActive(StateOption.Immortal, active: true);
			ChasaBoss.ClearStatusEffect(force: true);
			ChasaBoss.IsImmortal = true;
			AdjustChasaPositionOnPlayerDefeat();
			fakeChasa.SynchronizePositionDirection(defeatChasaStartPosition);
			SwitchToFakeSpine(ChasaBoss, fakeChasa, fakeChasaSpine);
			NullControllable nullControl = new NullControllable("Chasa Defeat");
			CutsceneProducer.InputManager.StackControlTarget(nullControl).Forget();
			UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround && CutsceneProducer.Player.mover.RelativeVelocity.x == 0f).GetAwaiter().OnCompleted(delegate
			{
				PlayTimelineAsync(4, blockControl: true).Forget();
				CutsceneProducer.InputManager.RemoveControlTarget(nullControl).Forget();
			});
		}
		return true;
	}

	public override bool ShouldLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 3:
			switch (index)
			{
			case 0:
				return IsNotOnGround();
			case 1:
				return CutsceneProducer.IsTalking;
			case 2:
				return CutsceneProducer.IsTalking;
			}
			break;
		case 4:
			if (index != 0)
			{
				break;
			}
			return CutsceneProducer.IsTalking;
		case 6:
			if (index != 0)
			{
				break;
			}
			return CutsceneProducer.IsTalking;
		}
		return false;
		bool IsNotOnGround()
		{
			bool num = !CutsceneProducer.Player.mover.IsOnGround;
			if (num && CutsceneProducer.InputManager.IsCurrentControlTarget(CutsceneProducer.Player))
			{
				CutsceneProducer.InputManager.StackControlTarget(this).Forget();
			}
			return num;
		}
	}

	public override void EnterLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 3:
			if (index == 0 || (uint)(index - 1) > 1u)
			{
				break;
			}
			goto IL_0032;
		case 6:
			if (index != 0)
			{
				break;
			}
			goto IL_0032;
		case 4:
			if (index == 0)
			{
				StartScenarioInsideCutscene(index);
			}
			break;
		case 5:
			break;
			IL_0032:
			StartScenarioInsideCutscene(index);
			break;
		}
	}

	public override void ExitLoop(int index)
	{
		if (base.CurrentState != 3 || index != 0)
		{
			return;
		}
		CutsceneProducer.FieldCanvas.HideAllAsync(instant: false).Forget();
		CutsceneProducer.InputManager.StackControlTarget(this).Forget();
		CutsceneProducer.Player.mover.Flip(Direction.Right);
		SetUpStartTimelineAsync(firstEventStartVcam).GetAwaiter().OnCompleted(delegate
		{
			Vector3 diff = chasaDashPosition.position - playerBlockPosition.position;
			playerBlockPosition.SynchronizePositionDirection(CutsceneProducer.Player.transform);
			chasaDashParticles.ForEach(delegate(Transform p)
			{
				p.SynchronizePositionDirection(playerBlockPosition, diff + p.position - chasaDashPosition.position);
			});
			chasaDashPosition.SynchronizePositionDirection(playerBlockPosition, diff);
		});
	}

	public void ChangeArea()
	{
		if ((object)CutsceneProducer.Field.CurrentArea != FightArea)
		{
			originArea = CutsceneProducer.Field.CurrentArea;
			CutsceneProducer.Field.ChangeAreaTo(FightArea);
		}
	}

	public void HealPlayer()
	{
		CutsceneProducer.Player.PlayerStat.RecoverStat(isHeatFulled: true);
		CutsceneProducer.Player.mover.PSM.SetStateOptionActive(StateOption.Immortal, active: false);
	}

	public void SustainSpineAnimation()
	{
		fakeChasaSpine.SynchronizeAnimation(fakeChasaSpine);
	}

	public void EnterRealCutscene()
	{
		defeatPlayerStartPosition.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		fakePlayer.SynchronizePositionDirection(defeatPlayerStartPosition);
		FlipFakePlayer(isDefeatPositionsFacingRight);
		fakePlayerSpine.SynchronizeAnimation(CutsceneProducer.Player.mover.SkeletonAnimation);
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		fakePlayer.gameObject.SetActive(value: true);
		transparentWalls.ForEach(delegate(GameObject wall)
		{
			wall.SetActive(value: false);
		});
	}

	public void LandPlayerOnGround()
	{
		CutsceneProducer.Player.mover.PSM.ChangeState(CutsceneProducer.Player.mover.PSM.IdleState);
	}

	public void ActivateAshFire()
	{
		InitializeAshFire();
		ParticleSystem.EmissionModule emission = ashFireParticle.emission;
		ParticleSystem.MainModule main = ashFireParticle.main;
		if ((object)ashFireRenderer == null)
		{
			ashFireRenderer = ashFireParticle.GetComponent<ParticleSystemRenderer>();
		}
		emission.rateOverTimeMultiplier = ashFireInitialRate;
		main.simulationSpeed = ashFireInitialSpeed;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetColor(BaseColorID, Color.white);
		ashFireRenderer.SetPropertyBlock(materialPropertyBlock);
		ashFireParticle.Play();
	}

	public void DeactivateAshFire()
	{
		ashFireParticle.Stop();
	}

	public void FlipFakeChasaToPlayer()
	{
		int num = ((!(fakeChasa.position.x > fakePlayer.position.x)) ? 1 : (-1));
		Vector3 localScale = fakeChasa.localScale;
		fakeChasa.localScale = new Vector3((float)num * Mathf.Abs(localScale.x), localScale.y, localScale.z);
	}

	public void PlayDropMaskSfx()
	{
		if (fakeChasaSpine.Skeleton.Skin.Name != "mask_off")
		{
			CutsceneProducer.SoundManager.PlayOneShotAttached(maskDropEvent, fakeChasa);
		}
	}

	private async UniTask WaitAndStartEventAsync()
	{
		cutsceneTriggers.ForEach(delegate(ChasaCutsceneTrigger trigger)
		{
			trigger.gameObject.SetActive(value: true);
		});
		CutsceneProducer.UIManager.FieldCanvas.BossBoard.SetBoss(ChasaBoss);
		CutsceneProducer.UIManager.FieldCanvas.BossBoard.ShowAsync().Forget();
		await UniTask.WaitUntil(() => !IsProducing);
		await UniTask.WhenAny(UniTask.WaitUntil(() => cutsceneTriggers.Any((ChasaCutsceneTrigger trigger) => trigger.IsTriggered)), UniTask.Delay(firstEventWaitingMilliSeconds));
		cutsceneTriggers.ForEach(delegate(ChasaCutsceneTrigger trigger)
		{
			trigger.gameObject.SetActive(value: false);
		});
		PlayTimelineAsync(3, blockControl: false, hideCanvas: false).Forget();
	}

	public void StartByDummy(bool byNpc)
	{
		if (!byNpc)
		{
			retryRightAfterDefeat = true;
			base.PlayableDirector.Stop();
			EndTimeline();
		}
		TaskDummyAsync(byNpc).Forget();
		CutsceneProducer.InputManager.StackControlTarget(this).Forget();
	}

	private async UniTask TaskDummyAsync(bool byNpc)
	{
		StopBGM();
		Dummies.ForEach(delegate(Dummy d)
		{
			d.ChangeState(1);
		});
		CutsceneProducer.FieldCanvas.HideAllAsync(instant: false).Forget();
		fadeImageMain.color = Color.clear;
		fadeImageMain.gameObject.SetActive(value: true);
		fadeImageSub.gameObject.SetActive(value: false);
		await UniTask.Delay(300);
		if (IsChasaNotFirst)
		{
			SetChasaMask(value: false);
			ChasaBoss.InitializeStat();
		}
		if (byNpc)
		{
			fakePlayer.SynchronizePositionDirection(CutsceneProducer.Player.transform);
			fakePlayerSpine.SynchronizeAnimation(CutsceneProducer.Player.mover.SkeletonAnimation);
			fakePlayerSpine.AddAnimation("cam_up", 0, loop: true, 0f, 1f, isImmediate: false);
			CutsceneProducer.Player.SetPosition(Util.DisablePosition);
			fakePlayer.gameObject.SetActive(value: true);
			SwitchToFakeSpine(ChasaNpc, fakeChasa, fakeChasaSpine);
			fakeChasa.transform.SynchronizePositionDirection(ChasaNpc.transform);
			fakeChasaSpine.SetAnimation("scene/stand_b", 0, loop: false);
			await fakeChasaSpine.WaitAnimationAsync();
		}
		fakeChasaSpine.SetAnimation("scene/bye_teleport", 0, loop: true, 0f, 1f, isImmediate: false);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(27);
		ApplyMaterialEffectByDummyAsync(isFadeOut: true).Forget();
		CutsceneProducer.SoundManager.PlayOneShotAttached(disappearEvent, fakeChasa);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(16);
		Vector3 position = fadeImageMain.transform.position;
		Vector3 subPos = fadeImageSub.transform.position;
		fadeImageMain.transform.position = new Vector3(Camera.main.transform.position.x, position.y, position.z);
		await FadeScreenByDummyAsync(isOut: true, 30);
		fadeImageMain.color = Color.clear;
		fadeImageSub.color = new Color(0f, 0f, 0f, 0.8f);
		fadeImageSub.transform.position = subPos;
		fadeImageSub.gameObject.SetActive(value: true);
		if (!byNpc)
		{
			playerDefeatEndVcam.gameObject.SetActive(value: false);
		}
		await SwitchFieldCamToCutsceneStartCam(FightArea.Vcam);
		FightArea.Vcam.gameObject.SetActive(value: true);
		ChangeArea();
		ChasaNpc.gameObject.SetActive(value: false);
		fakePlayer.SynchronizePositionDirection(playerFightPosition);
		fakePlayerSpine.SetAnimationInstantly("idle");
		fakeChasa.SynchronizePositionDirection(chasaFightPosition);
		fakeChasaSpine.SetAnimationInstantly(IsChasaNotFirst ? "idle" : "scene/battle_ready_b_loop");
		UniTaskExtension.WaitForFixedUpdatesOrCancel(30).GetAwaiter().OnCompleted(delegate
		{
			PlayEffectByDummyAsync().Forget();
			ApplyMaterialEffectByDummyAsync(isFadeOut: false).Forget();
			fakeChasa.gameObject.SetActive(value: true);
		});
		await FadeScreenByDummyAsync(isOut: false, 45);
		CutsceneProducer.FieldCanvas.ShowAllAsync(instant: false).Forget();
		transparentWalls.ForEach(delegate(GameObject wall)
		{
			wall.SetActive(value: true);
		});
		CutsceneProducer.Player.mover.SynchronizeAnimation(fakePlayerSpine);
		SynchronizePlayer(fakePlayer);
		CutsceneProducer.Player.SetPosition(playerFightPosition.position);
		if (IsChasaNotFirst)
		{
			ChasaBoss.SynchronizeAnimation(fakeChasaSpine);
			fakeChasa.gameObject.SetActive(value: false);
			ChasaBoss.ExitCutsceneState(fakeChasa);
			CutsceneProducer.UIManager.FieldCanvas.BossBoard.ShowAsync().Forget();
			ActivateAshFire();
			StartFight();
		}
		else
		{
			WaitAndStartEventAsync().Forget();
		}
		fadeImageMain.gameObject.SetActive(value: false);
		fadeImageSub.gameObject.SetActive(value: false);
		CutsceneProducer.InputManager.RemoveControlTarget(this).Forget();
		CutsceneProducer.SoundManager.PlayBGM(FightArea.SoundKey);
	}

	private async UniTask FadeScreenByDummyAsync(bool isOut, int frames)
	{
		SpriteRenderer target = (isOut ? fadeImageMain : fadeImageSub);
		await DOTween.Sequence().Append(target.DOAlpha(isOut ? 0.8f : 0f, (float)frames / 60f)).WaitAsync();
	}

	private async UniTask PlayEffectByDummyAsync()
	{
		playerTeleportToParticle.gameObject.SetActive(value: true);
		playerTeleportToParticle.Play();
		chasaTeleportToParticle.gameObject.SetActive(value: true);
		chasaTeleportToParticle.Play();
		CutsceneProducer.SoundManager.PlayOneShotAttached(teleportEvent, fakeChasa);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(37);
		playerTeleportToParticle.gameObject.SetActive(value: false);
		playerTeleportToParticle.Stop();
		chasaTeleportToParticle.gameObject.SetActive(value: false);
		chasaTeleportToParticle.Stop();
	}

	private async UniTask ApplyMaterialEffectByDummyAsync(bool isFadeOut)
	{
		MaterialPropertyBlock playerMpb = new MaterialPropertyBlock();
		MaterialPropertyBlock chasaMpb = new MaterialPropertyBlock();
		if (isFadeOut)
		{
			headVfx1.Stop();
			headVfx2.Stop();
		}
		else
		{
			headVfx1.Play();
			headVfx2.Play();
		}
		for (int f = 0; f < 15; f++)
		{
			playerMpb.Clear();
			chasaMpb.Clear();
			foreach (KeyValuePair<int, (float, float)> item in MpbDictionary)
			{
				int num = f;
				if (isFadeOut && item.Key.Equals(AlphaID))
				{
					num -= 5;
				}
				float t = (float)num / (isFadeOut ? 10f : 15f);
				float a = (isFadeOut ? item.Value.Item1 : item.Value.Item2);
				float b = (isFadeOut ? item.Value.Item2 : item.Value.Item1);
				float value = Mathf.Lerp(a, b, t);
				if (item.Key != HeadParticle)
				{
					playerMpb.SetFloat(item.Key, value);
					if (item.Key != DistortionAmountID)
					{
						chasaMpb.SetFloat(item.Key, value);
					}
				}
			}
			FakePlayerRenderer.SetPropertyBlock(playerMpb);
			FakeChasaRenderer.SetPropertyBlock(chasaMpb);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(1);
		}
		foreach (KeyValuePair<int, (float, float)> item2 in MpbDictionary)
		{
			float value2 = (isFadeOut ? item2.Value.Item2 : item2.Value.Item1);
			playerMpb.SetFloat(item2.Key, value2);
			if (item2.Key != DistortionAmountID)
			{
				chasaMpb.SetFloat(item2.Key, value2);
			}
		}
		FakePlayerRenderer.SetPropertyBlock(playerMpb);
		FakeChasaRenderer.SetPropertyBlock(chasaMpb);
	}

	private void SetPlayerMaterial()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		foreach (KeyValuePair<int, (float, float)> item2 in MpbDictionary)
		{
			float item = item2.Value.Item1;
			materialPropertyBlock.SetFloat(item2.Key, item);
		}
		headVfx1.Play();
		headVfx2.Play();
		FakePlayerRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	private void SetChasaMask(bool value)
	{
		string skin = (value ? "chasa" : "mask_off");
		ChasaBoss.SetSkin(skin);
		fakeChasaSpine.Skeleton.SetSkin(skin);
		fakeChasaSpine.Skeleton.SetSlotsToSetupPose();
	}

	private void AdjustBossSpineOnClear(bool isFacingRight)
	{
		Vector3 bossDiff = ChasaBoss.transform.position - clearChasaStartPosition.position;
		clearChasaStartPosition.Translate(bossDiff);
		clearChasaStartParticles.ForEach(delegate(Transform p)
		{
			p.Translate(bossDiff, Space.World);
		});
		Vector3 localScale = clearChasaStartPosition.localScale;
		Vector3 localScale2 = new Vector3((float)((!isFacingRight) ? 1 : (-1)) * Mathf.Abs(localScale.x), localScale.y, localScale.z);
		clearChasaStartPosition.localScale = localScale2;
		if (!isFacingRight)
		{
			clearChasaStartParticles.ForEach(delegate(Transform p)
			{
				p.ReflectXWithFlip(clearChasaStartPosition.position.x);
			});
		}
		fakeChasa.SynchronizePositionDirection(clearChasaStartPosition);
		SwitchToFakeSpine(ChasaBoss, fakeChasa, fakeChasaSpine);
	}

	private void AdjustPositionsOnClear(bool isFacingRight)
	{
		FlipClearPositions(isFacingRight);
		Vector3 vector = CutsceneProducer.Player.transform.position - clearPlayerStartPosition.position;
		float diffX = vector.x;
		float y = vector.y;
		clearPlayerStartPosition.Translate(diffX, y, 0f);
		float vcamExcessX = 0f;
		if (clearVcam.transform.position.x + diffX > maxVcamPositionX)
		{
			vcamExcessX = clearVcam.transform.position.x + diffX - maxVcamPositionX;
		}
		if (clearPlayerEndPosition.position.x + diffX - vcamExcessX > maxPlayerPositionsX)
		{
			vcamExcessX = clearPlayerEndPosition.position.x + diffX - maxPlayerPositionsX;
		}
		clearPlayerEndPosition.Translate(diffX - vcamExcessX, y, 0f);
		clearChasaEndPosition.Translate(diffX - vcamExcessX, 0f, 0f);
		clearVcam.transform.Translate(diffX - vcamExcessX, 0f, 0f);
		clearVcams.ForEach(delegate(Transform p)
		{
			p.Translate(diffX - vcamExcessX, 0f, 0f);
		});
		clearChasaEndParticles.ForEach(delegate(Transform p)
		{
			p.Translate(diffX - vcamExcessX, 0f, 0f, Space.World);
		});
		FlipFakePlayer(isFacingRight);
		SwitchChasaEffectDirection(isFacingRight);
	}

	private void FlipClearPositions(bool isRight)
	{
		if (!isRight)
		{
			float playerX = CutsceneProducer.Player.transform.position.x;
			clearPlayerStartPosition.ReflectXWithFlip(playerX);
			clearPlayerEndPosition.ReflectXWithFlip(playerX);
			clearChasaEndPosition.ReflectXWithFlip(playerX);
			clearVcam.transform.ReflectXWithFlip(playerX, includeFlip: false);
			clearVcams.ForEach(delegate(Transform p)
			{
				p.ReflectXWithFlip(playerX, includeFlip: false);
			});
			clearChasaEndParticles.ForEach(delegate(Transform p)
			{
				p.ReflectXWithFlip(playerX);
			});
		}
	}

	private void AdjustChasaPositionOnPlayerDefeat()
	{
		float diffX = ChasaBoss.transform.position.x - defeatChasaStartPosition.position.x;
		defeatChasaStartPosition.Translate(diffX, 0f, 0f);
		defeatChasaStartParticles.ForEach(delegate(Transform p)
		{
			p.Translate(diffX, 0f, 0f, Space.World);
		});
		int sign = ChasaBoss.transform.localScale.x.GetSign();
		Vector3 localScale = defeatChasaStartPosition.localScale;
		defeatChasaStartPosition.localScale = new Vector3((float)sign * Mathf.Abs(localScale.x), localScale.y, localScale.z);
		defeatChasaStartParticles.ForEach(delegate(Transform p)
		{
			Vector3 localScale2 = p.localScale;
			p.localScale = new Vector3((float)sign * Mathf.Abs(localScale2.x), localScale2.y, localScale2.z);
		});
	}

	private void AdjustPositionsOnPlayerDefeat()
	{
		ResetPositionsOnPlayerDefeat();
		bool isFacingRight = CutsceneProducer.Player.mover.IsFacingRight;
		FlipDefeatPositions(isFacingRight);
		float diffX = CutsceneProducer.Player.transform.position.x - defeatPlayerRecoveryPosition.position.x;
		if (playerDefeatEndVcam.transform.position.x + diffX > maxVcamPositionX)
		{
			defeatEndVcamExcessX = playerDefeatEndVcam.transform.position.x + diffX - maxVcamPositionX;
		}
		defeatPlayerRecoveryPosition.Translate(diffX - defeatEndVcamExcessX, 0f, 0f);
		defeatChasaRecoveryPosition.Translate(diffX - defeatEndVcamExcessX, 0f, 0f);
		playerDefeatEndVcam.transform.Translate(diffX - defeatEndVcamExcessX, 0f, 0f);
		defeatChasaRecoveryParticles.ForEach(delegate(Transform p)
		{
			p.Translate(diffX - defeatEndVcamExcessX, 0f, 0f, Space.World);
		});
		FlipFakePlayer(isFacingRight);
		SwitchChasaEffectDirection(isFacingRight);
	}

	private void ResetPositionsOnPlayerDefeat()
	{
		defeatPlayerRecoveryPosition.Translate(defeatEndVcamExcessX, 0f, 0f);
		defeatChasaRecoveryPosition.Translate(defeatEndVcamExcessX, 0f, 0f);
		playerDefeatEndVcam.transform.Translate(defeatEndVcamExcessX, 0f, 0f);
		defeatChasaRecoveryParticles.ForEach(delegate(Transform p)
		{
			p.Translate(defeatEndVcamExcessX, 0f, 0f, Space.World);
		});
		defeatEndVcamExcessX = 0f;
	}

	private void FlipDefeatPositions(bool isRight)
	{
		if (isRight != isDefeatPositionsFacingRight)
		{
			float playerX = CutsceneProducer.Player.transform.position.x;
			defeatPlayerStartPosition.ReflectXWithFlip(playerX);
			defeatPlayerRecoveryPosition.ReflectXWithFlip(playerX);
			defeatChasaRecoveryPosition.ReflectXWithFlip(playerX);
			playerDefeatEndVcam.transform.ReflectXWithFlip(playerX, includeFlip: false);
			defeatChasaRecoveryParticles.ForEach(delegate(Transform p)
			{
				p.ReflectXWithFlip(playerX);
			});
			isDefeatPositionsFacingRight = !isDefeatPositionsFacingRight;
		}
	}

	private void FlipFakePlayer(bool isRight)
	{
		Vector3 localScale = fakePlayer.localScale;
		if (!(localScale.x * (float)(isRight ? 1 : (-1)) > 0f))
		{
			fakePlayer.localScale = new Vector3(0f - localScale.x, localScale.y, localScale.z);
		}
	}

	private void ChangeAshFireRate(float rateMultiplier)
	{
		ParticleSystem.EmissionModule emission = ashFireParticle.emission;
		if (ashFireInitialRate < 0f)
		{
			ashFireInitialRate = emission.rateOverTimeMultiplier;
		}
		emission.rateOverTimeMultiplier = ashFireInitialRate * rateMultiplier;
	}

	private void SwitchChasaEffectDirection(bool isPlayerFacingRight)
	{
		playerRightEffectParents.ForEach(delegate(GameObject go)
		{
			go.SetActive(isPlayerFacingRight);
		});
		playerLeftEffectParents.ForEach(delegate(GameObject go)
		{
			go.SetActive(!isPlayerFacingRight);
		});
	}

	private void GiveReward()
	{
	}

	private void InitializeAshFire()
	{
		if (!isAshFireInitialized)
		{
			ParticleSystem.EmissionModule emission = ashFireParticle.emission;
			ParticleSystem.MainModule main = ashFireParticle.main;
			ashFireInitialRate = emission.rateOverTimeMultiplier;
			ashFireInitialSpeed = main.simulationSpeed;
			isAshFireInitialized = true;
		}
	}

	private void StartFight()
	{
		fightTime = Time.time;
		ChasaBoss.ResetStunCount();
	}

	private void AddFightNum()
	{
		CutsceneProducer.EnemyFightData.AddBossTry(BossName);
	}

	private void ClearFight()
	{
		CutsceneProducer.EnemyFightData.TryBreakBossClearRecord(BossName, Time.time - fightTime);
		fightTime = 0f;
		CutsceneProducer.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(BossName, isClear: true);
	}
}
