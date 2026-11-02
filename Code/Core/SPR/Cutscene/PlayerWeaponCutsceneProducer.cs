using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.UI.Tutorial;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class PlayerWeaponCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private SkeletonAnimation fakePlayerSpine;

	[SerializeField]
	private ItemObject cutsceneItem;

	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private float startVcamBlendTime;

	[SerializeField]
	private BoxCollider2D bgmTrigger;

	private static PureItem WristbandItem => new PureItem("PyronWristband");

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			cutsceneItem.gameObject.SetActive(value: true);
			break;
		case 1:
			cutsceneItem.gameObject.SetActive(value: false);
			CutsceneProducer.Player.PlayerStat.ActivateBasicAttack(value: true);
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		Vector3 initialPlayerPos = fakePlayer.position;
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround, PlayerLoopTiming.Update, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		await CutsceneProducer.Player.AdjustPlayerPositionAsync(initialPlayerPos);
		if (!token.IsCancellationRequested)
		{
			await BlendFieldCamToCutsceneStartCamAsync(startVcam, startVcamBlendTime);
			if (!token.IsCancellationRequested)
			{
				fakePlayerSpine.SynchronizeAnimation(CutsceneProducer.Player.mover.SkeletonAnimation);
				CutsceneProducer.Player.SetPosition(Util.DisablePosition);
			}
		}
	}

	protected override void OnEndTimeline(int state)
	{
		SynchronizePlayer(fakePlayer);
		SynchronizeEndVcamAsync(startVcam).Forget();
		CutsceneProducer.Player.PlayerStat.ActivateBasicAttack(value: true);
		CutsceneProducer.UIManager.FieldCanvas.TutorialMessageBoard.AddMessage(ActionName.Attack);
		bgmTrigger.gameObject.SetActive(value: true);
	}

	protected override void OnSkipTimeline(int state)
	{
		cutsceneItem.gameObject.SetActive(value: false);
		WristbandItem.AddToPlayerData(ItemAcquireDisplayOrder.None, ignoreIfAlreadyOwned: true);
	}

	public void OpenInteractionPanel()
	{
		TempAsync().Forget();
	}

	private async UniTask TempAsync()
	{
		base.PlayableDirector.Pause();
		WristbandItem.AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanelImmediately);
		await UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(this));
		base.PlayableDirector.Play();
	}

	private void Update()
	{
		if (bgmTrigger.gameObject.activeInHierarchy && bgmTrigger.IsTouchingLayers(LayerUtility.PlayerLayer))
		{
			bgmTrigger.gameObject.SetActive(value: false);
			Game.Instance.SoundManager.PlayBGM();
		}
	}
}
