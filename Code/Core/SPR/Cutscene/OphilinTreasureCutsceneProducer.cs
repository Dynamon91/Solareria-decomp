using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Cutscene;

public class OphilinTreasureCutsceneProducer : CutsceneProducer
{
	public const int InitialState = 0;

	private const int OpenedState = 1;

	[Header("Vcams")]
	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[Header("Positions")]
	[SerializeField]
	private Transform playerPosition;

	[Header("References")]
	[SerializeField]
	private OtherEntrance entrance;

	[SerializeField]
	private GameObject altarObject;

	[SerializeField]
	private Transform soowooStatue;

	[SerializeField]
	private Transform waterDragonStatue;

	[Header("Values")]
	[SerializeField]
	private float soowooOpenDistance;

	[SerializeField]
	private float waterDragonOpenDistance;

	protected override void SetProducerState(int state)
	{
		if (state != 0 && state != 1)
		{
			DebugLog.GeneralLogError($"{ID}가 가질 수 없는 상태({state}가 들어왔습니다!)");
			throw new InvalidOperationException();
		}
		bool flag = state == 0;
		entrance.gameObject.SetActive(!flag);
		altarObject.SetActive(flag);
		soowooStatue.localPosition += new Vector3(flag ? 0f : soowooOpenDistance, 0f, 0f);
		waterDragonStatue.localPosition += new Vector3(flag ? 0f : waterDragonOpenDistance, 0f, 0f);
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await CutsceneProducer.Player.MoveXPositionAsync(playerPosition.position.x, isRunning: false, token);
		CutsceneProducer.Player.mover.Flip(Direction.Right);
		await BlendFieldCamToCutsceneStartCamAsync(startVcam, 0.8f);
		BindPlayerTracks(value: true, tween: true);
	}

	protected override void OnEndTimeline(int state)
	{
		SustainVcamForFade(endVcam);
		Game.Instance.LoadFieldAsync(SceneDataModel.MoveSpecialModel(Game.Instance.CurrentFieldID.SectorName, entrance.SpecialAddress, entrance.Name)).Forget();
		CutsceneProducer.NotShowFieldCanvasUntilSceneChange();
	}

	protected override void OnSkipTimeline(int state)
	{
		DisableFadeInAfterSkip();
	}
}
