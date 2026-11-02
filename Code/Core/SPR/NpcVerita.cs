using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.Cutscene;
using SPR.Save;
using SPR.UI.Upgrade;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR;

public class NpcVerita : ServiceNPC<Unit>, ILevelUpProvider
{
	[Header("Verita")]
	[SerializeField]
	private float deskDistance;

	[SerializeField]
	private TimelineAsset levelUpMotionTimeline;

	[SerializeField]
	private int standUpMoveDistance;

	[Header("Verita WhiteOut")]
	[SerializeField]
	private PlayableDirector whiteOutDirector;

	[SerializeField]
	private float uiWhiteOutPredelay;

	[SerializeField]
	private float uiWhiteOutDuration;

	private Vector2 initialPos = Util.DisablePosition;

	private PlayableDirector director;

	private CancellationTokenSource heatTokenSource;

	private bool isInteracting;

	private bool isStandUp;

	private const string DefaultIdleAnimation = "scene/final/stand";

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	protected override ServicePanel<Unit> ServicePanel => Game.Instance.UIManager.FieldCanvas.LevelUpPanel;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		if (initialPos == Util.DisablePosition)
		{
			initialPos = base.transform.position;
		}
		base.ShouldChaseSight = false;
		isStandUp = false;
		SetSkin("Light");
		SetPositionAnimation();
	}

	private void SetPositionAnimation()
	{
		if (Trigger.CheckTrigger("Trigger_VeritaQuestProgress") && !Trigger.CheckTrigger("Trigger_VeritaQuestComplete"))
		{
			SetAnimation("scene/push_out");
			base.transform.position = initialPos + Vector2.left * deskDistance;
		}
		else
		{
			EnterIdleAnimation();
			base.transform.position = initialPos;
		}
	}

	protected override async UniTask MoveNPCBeforeTalkAsync()
	{
		if (!isStandUp)
		{
			isStandUp = true;
			playerInteractPositionX -= standUpMoveDistance;
			Vector2 standUpPos = initialPos + Vector2.left * standUpMoveDistance;
			await SetAnimationAsync("scene/sit_up");
			SetAnimation("walk");
			await base.transform.DOLocalMoveX(standUpPos.x, 1f).SetEase(Ease.Linear).WaitAsync();
			SetAnimation("scene/final/stand");
		}
	}

	protected override void PlayTotAnimationOnFinished(PlayerMover mover)
	{
		if (!isInteracting)
		{
			base.PlayTotAnimationOnFinished(mover);
		}
	}

	protected override void OnFinishTalk()
	{
		if (!isInteracting)
		{
			EnterIdleAnimation();
		}
	}

	protected override void EnterIdleAnimation()
	{
		SetAnimation(isStandUp ? "scene/final/stand" : "scene/sit_idle");
	}

	protected override void EnterTalkAnimation()
	{
		SetAnimation("scene/final/stand");
	}

	public override async UniTask DisappearAsync()
	{
		SetAnimation("scene/warp", loop: false);
		await UniTaskExtension.WaitForSeconds(1.667f);
		base.gameObject.SetActive(value: false);
	}

	protected override async UniTask DisappearWithFadeAsync()
	{
		await Npc.FadeCanvas.FadeOut(0.5f, 0.5f);
		EnterIdleAnimation();
		await UniTask.WaitForSeconds(0.5f);
		Npc.FieldCanvas.HideAllAsync(instant: true).Forget();
		await Npc.FadeCanvas.FadeIn(0.5f);
		Npc.FieldCanvas.ShowAllAsync(instant: false).Forget();
	}

	public async UniTask UpgradeHeatAsync()
	{
		isInteracting = true;
		BoxCollider2D coll = GetComponent<BoxCollider2D>();
		coll.enabled = false;
		await MoveToUpgradeAsync();
		await HeatUpgradeCutsceneProducer.GetProducer().PlayTimelineAsync(PlayerStat.HeatMaxLevel, blockControl: true);
		await MoveToReturnAsync();
		Npc.Player.mover.SetAnimation("idle");
		coll.enabled = true;
		isInteracting = false;
	}

	private async UniTask MoveToUpgradeAsync()
	{
		SetAnimation("scene/final/stand");
		await base.transform.DOMoveX(initialPos.x - deskDistance, 0.5f).SetEase(Ease.InOutSine).WaitAsync();
		SetAnimation("scene/charge", loop: false);
		await UniTask.WaitForSeconds(1.5f, ignoreTimeScale: false, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref heatTokenSource));
	}

	private async UniTask MoveToReturnAsync()
	{
		Vector2 vector = initialPos + Vector2.left * standUpMoveDistance;
		float f = vector.x - base.transform.position.x;
		Flip(f.GetSign());
		SetAnimation("scene/final/stand");
		await base.transform.DOMoveX(vector.x, 0.5f).SetEase(Ease.InOutSine).WaitAsync();
		Flip(-1f);
		EnterIdleAnimation();
	}

	private void OnDisable()
	{
		heatTokenSource?.Cancel();
	}

	public override void OnServiceOpening()
	{
		isInteracting = true;
		SetAnimation("scene/final/stand");
	}

	public override void OnServiceClosing()
	{
		isInteracting = false;
		SetAnimation("scene/final/stand");
		PlayTotAnimationOnFinished(Npc.Player.mover);
	}

	public async UniTask PlayLevelUpMotionAsync()
	{
		Director.playableAsset = levelUpMotionTimeline;
		Director.Play();
		await UniTask.WaitWhile(() => Director.state == PlayState.Playing);
		SetAnimation("scene/final/stand");
	}

	public async UniTask TeleportToGate()
	{
		Game.Instance.LoadFieldAsync(SceneDataModel.MoveModel(new FieldID(SectorName.Library, 4), "VeritaGateTeleportExit"), skipExit: true).Forget();
		await UniTask.WaitUntil(() => Npc.FadeCanvas.IsBlack);
	}

	public async UniTask WhiteOutAsync()
	{
		UniTask.WaitForSeconds(uiWhiteOutPredelay).GetAwaiter().OnCompleted(delegate
		{
			Npc.FadeCanvas.FadeWhiteOut(uiWhiteOutDuration).Forget();
		});
		whiteOutDirector.Play();
		await UniTask.WhenAll(UniTask.WaitUntil(() => whiteOutDirector.state == PlayState.Paused), UniTask.WaitUntil(() => Npc.FadeCanvas.IsBlack));
	}
}
