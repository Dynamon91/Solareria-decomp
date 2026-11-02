using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Field;
using SPR.Save;
using Spine.Unity;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class HaechiLandCutsceneProducer : CutsceneProducer
{
	private const int InitialState = 0;

	private const int ElevatorAppearState = 1;

	private const int FinishedState = 2;

	private const float ElevatorHidePosY = -24.5f;

	private const float ElevatorShowPosY = -4.5f;

	private const float LeverHidePosY = -12.85f;

	private const float LeverShowPosY = -7.85f;

	[SerializeField]
	private Transform playerFigure;

	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private List<GameObject> cutsceneObjects;

	[SerializeField]
	private List<ParticleSystem> cutsceneParticles;

	[SerializeField]
	private Renderer hornLRenderer;

	[SerializeField]
	private BoneFollower boneFollower;

	[Header("Elevator")]
	[SerializeField]
	private TimelineAsset elevatorTimeline;

	[SerializeField]
	private HorizontalElevator elevator;

	[SerializeField]
	private GameObject elevatorLever;

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
		cutsceneObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: false);
		});
		switch (state)
		{
		case 0:
		{
			BindPlayerFigureTweenTrack(value: false);
			FlipPlayerFigure(isRight: true);
			elevator.gameObject.SetActive(value: false);
			elevatorLever.SetActive(value: false);
			Vector3 position2 = elevator.transform.position;
			position2.y = -24.5f;
			elevator.transform.position = position2;
			position2 = elevatorLever.transform.position;
			position2.y = -12.85f;
			elevatorLever.transform.position = position2;
			break;
		}
		case 1:
			base.PlayableDirector.playableAsset = elevatorTimeline;
			break;
		case 2:
		{
			elevator.gameObject.SetActive(value: true);
			elevatorLever.SetActive(value: true);
			Vector3 position = elevator.transform.position;
			position.y = -4.5f;
			elevator.transform.position = position;
			position = elevatorLever.transform.position;
			position.y = -7.85f;
			elevatorLever.transform.position = position;
			elevator.GetComponent<IStateSavable>().InitializeState();
			break;
		}
		}
	}

	protected override UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			cutsceneObjects.ForEach(delegate(GameObject go)
			{
				go.SetActive(value: true);
			});
			CutsceneProducer.FadeCanvas.FadeIn(1f, 0f, Ease.Linear, token).Forget();
			SetHornLayer(value: true);
			break;
		case 1:
			ChangeState(2);
			break;
		}
		return UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		if (state == 0)
		{
			CutsceneProducer.Player.mover.SynchronizeAnimation(playerFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));
			SynchronizePlayer(playerFigure);
			CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
			SynchronizeEndVcamAsync(endVcam).Forget();
			cutsceneParticles.ForEach(delegate(ParticleSystem ps)
			{
				ps.Stop();
			});
			PlayTimelineAsync(1).Forget();
		}
		else if (state >= 1)
		{
			elevator.ChangeState(1);
			elevator.GetComponent<IStateSavable>().InitializeState();
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		BindPlayerFigureTweenTrack(value: true);
		FlipPlayerFigure(isRight: false);
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
	}

	public void FlipPlayerFigure(bool isRight)
	{
		Vector3 localScale = playerFigure.localScale;
		playerFigure.localScale = new Vector3((float)(isRight ? 1 : (-1)) * Mathf.Abs(localScale.x), localScale.y, localScale.z);
	}

	public void BindPlayerFigureTweenTrack(bool value)
	{
		base.PlayableDirector.TrackBinding<TweenTrack>("Player Tween Track", value ? playerFigure : null);
		EnableBoneFollower(!value);
	}

	public void SetHornLayer(bool value)
	{
		hornLRenderer.GetComponent<Renderer>().sortingLayerName = (value ? "Player" : "Character");
	}

	private void EnableBoneFollower(bool value)
	{
		boneFollower.followXYPosition = value;
		boneFollower.enabled = value;
	}
}
