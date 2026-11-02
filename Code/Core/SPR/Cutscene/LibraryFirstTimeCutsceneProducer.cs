using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.UI.Field;
using UnityEngine;

namespace SPR.Cutscene;

public class LibraryFirstTimeCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private CinemachineVirtualCamera vcam;

	[SerializeField]
	private Transform veritaFigure;

	[SerializeField]
	private Transform playerPosition;

	[SerializeField]
	private Transform playerPositionPortal;

	[SerializeField]
	private List<Transform> positions;

	[SerializeField]
	private CutsceneTrigger triggerEntrance;

	[SerializeField]
	private CutsceneTrigger triggerPortal;

	[Header("VFX")]
	[SerializeField]
	private ParticleSystem resonanceFlameParticle;

	[SerializeField]
	private ParticleSystem flameParticle;

	[SerializeField]
	private Transform resonanceStartPosition;

	[SerializeField]
	private Transform resonanceFinishPosition;

	[SerializeField]
	private float resonanceDuration;

	private bool isEntrance;

	private PureItem PartItem => new PureItem("Health");

	private static InteractionPanel InteractionPanel => Game.Instance.UIManager.FieldCanvas.InteractionPanel;

	private static RuntimeData RuntimeData => Game.Instance.RuntimeData;

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			triggerEntrance.OnTriggered += delegate
			{
				StartCutscene(entrance: true);
			};
			triggerPortal.OnTriggered += delegate
			{
				StartCutscene(entrance: false);
			};
			return;
		case 1:
			if (Trigger.CheckTrigger("Trigger_EnterLibraryWithPortal"))
			{
				triggerEntrance.OnTriggered += delegate
				{
					CutsceneProducer.TalkCanvas.ShowSpecialTalk("Library_FirstEntranceAfterPortal").Forget();
					ChangeState(2);
				};
				return;
			}
			break;
		}
		base.gameObject.SetActive(value: false);
	}

	private void StartCutscene(bool entrance)
	{
		isEntrance = entrance;
		UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(CutsceneProducer.Player)).GetAwaiter().OnCompleted(delegate
		{
			PlayTimelineAsync(0, blockControl: true).Forget();
		});
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround, PlayerLoopTiming.Update, token);
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.GetCurrentAnim(0).Name == "idle", PlayerLoopTiming.Update, token);
		SetPositions();
		await BlendFieldCamToCutsceneStartCamAsync(vcam);
		BindPlayerTracks(value: true);
	}

	protected override void OnEndTimeline(int state)
	{
		SynchronizeEndVcamAsync(vcam).Forget();
		BindPlayerTracks(value: false);
		triggerEntrance.ClearAction();
		triggerPortal.ClearAction();
		CutsceneProducer.AddInteractCount(ActorName.Verita);
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		resonanceFlameParticle.gameObject.SetActive(value: false);
		flameParticle.gameObject.SetActive(value: false);
		PartItem.AddToPlayerData(ItemAcquireDisplayOrder.None, ignoreIfAlreadyOwned: true);
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public override void ExitLoop(int index)
	{
	}

	public void PlayResonanceParticle()
	{
		resonanceFlameParticle.transform.position = resonanceStartPosition.position;
		resonanceFlameParticle.gameObject.SetActive(value: true);
		resonanceFlameParticle.Play();
		resonanceFlameParticle.transform.DOMove(resonanceFinishPosition.position, resonanceDuration).SetEase(Ease.OutSine).OnComplete(delegate
		{
			resonanceFlameParticle.Stop();
			UniTask.WaitUntil(() => !flameParticle.IsAlive()).GetAwaiter().OnCompleted(delegate
			{
				resonanceFlameParticle.gameObject.SetActive(value: false);
			});
		});
	}

	public void PlayFlameParticle()
	{
		flameParticle.gameObject.SetActive(value: true);
		flameParticle.Play();
		UniTask.WaitUntil(() => !flameParticle.IsAlive()).GetAwaiter().OnCompleted(delegate
		{
			flameParticle.Stop();
			flameParticle.gameObject.SetActive(value: false);
		});
	}

	public void AcquirePart()
	{
		AcquirePartAsync().Forget();
	}

	private async UniTask AcquirePartAsync()
	{
		base.PlayableDirector.Pause();
		PartItem.AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanelImmediately);
		await UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(this));
		base.PlayableDirector.Play();
	}

	private void SetPositions()
	{
		Transform obj = (isEntrance ? CutsceneProducer.Player.mover.transform : playerPositionPortal);
		if (!isEntrance)
		{
			playerPositionPortal.Translate(CutsceneProducer.Player.mover.VisualBottom.x - playerPositionPortal.position.x, 0f, 0f);
		}
		Vector3 vector = obj.position - playerPosition.position;
		foreach (Transform position in positions)
		{
			position.Translate(vector.x, vector.y, 0f);
		}
		veritaFigure.SetDirection(isRight: true);
	}
}
