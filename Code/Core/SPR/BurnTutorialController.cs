using System;
using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Save;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR;

public class BurnTutorialController : MonoBehaviour, IStateSavable
{
	private enum State
	{
		None,
		Watched
	}

	[SerializeField]
	private float dropSpeed;

	[Header("References")]
	[SerializeField]
	private Transform dropPosition;

	[SerializeField]
	private CutsceneTrigger trigger;

	[SerializeField]
	private CinemachineImpulseSource impulseSource;

	[SerializeField]
	private ParryActionGuideCanvas guideCanvas;

	[SerializeField]
	private EventReference dropEvent;

	public string ID => base.name;

	private static InputManager InputManager => Game.Instance.InputManager;

	public void LoadState(int state)
	{
		switch ((State)state)
		{
		case State.None:
			trigger.OnTriggered += delegate
			{
				CreatureAppear().Forget();
			};
			break;
		case State.Watched:
			base.gameObject.SetActive(value: false);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void ChangeState(int state)
	{
		LoadState(state);
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private async UniTask CreatureAppear()
	{
		List<Creature> list = new List<Creature>();
		Sequence sequence = DOTween.Sequence();
		sequence.SetUpdate(UpdateType.Fixed);
		foreach (Transform item in base.transform)
		{
			if (item.TryGetComponent<Creature>(out var creature))
			{
				list.Add(creature);
				creature.gameObject.SetActive(value: true);
				creature.StateMachine.ChangeState(null);
				creature.SetAnimation("falling");
				float num = Mathf.Abs(dropPosition.position.y - creature.Position.y);
				sequence.Join(creature.transform.DOMoveY(dropPosition.position.y, num / dropSpeed).SetEase(Ease.InSine).OnComplete(delegate
				{
					CreatureOnLanded(creature).Forget();
				}));
			}
		}
		foreach (Creature item2 in list)
		{
			item2.transform.SetParent(base.transform.parent);
		}
		impulseSource.GenerateImpulse();
		Game.Instance.SoundManager.PlayOneShot(dropEvent);
		await sequence.WaitAsync();
		Game.Instance.Player.PlayerStat.ResetHeatGauge(isFulled: true);
		await UniTask.WaitForSeconds(0.5f);
		await UniTask.WaitUntil(() => Game.Instance.Player.mover.IsOnGround);
		TutorialCanvas tutorialPanel = Game.Instance.UIManager.TutorialCanvas;
		tutorialPanel.SetData(ActionName.Ignition);
		await InputManager.StackControlTarget(tutorialPanel);
		await UniTask.WaitWhile(() => InputManager.IsCurrentControlTarget(tutorialPanel));
		await UniTask.WaitWhile(() => Game.GetTimeScale() <= 0f);
		Game.SetTimeScale(0f);
		guideCanvas.SetPosition(Game.Instance.Player.mover);
		guideCanvas.Activate();
		await UniTask.WaitUntil(() => Game.Instance.InputManager.overheat);
		Game.SetTimeScale(1f);
		ChangeState(1);
	}

	private async UniTaskVoid CreatureOnLanded(Creature creature)
	{
		creature.SetAnimation("landing", loop: false);
		await UniTask.WaitUntil(() => creature.IsAnimComplete(0, "landing"));
		creature.enabled = true;
		creature.SetConnectedPath();
		creature.StateMachine.ChangeState(creature.DefaultState);
	}
}
