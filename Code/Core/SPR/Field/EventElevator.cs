using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Cutscene;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class EventElevator : BrokenElevator
{
	[SerializeField]
	private ElevatorBackgroundController backgroundController;

	[SerializeField]
	private Collider2D waveTriggerCollider;

	[SerializeField]
	private MyosoElevatorCutsceneProducer myosoCutsceneProducer;

	private EnemyWaveTrigger enemyWaveTrigger;

	private static AreaBox AreaBox => Game.Instance.FieldController.CurrentArea.AreaBox;

	protected override void Awake()
	{
		base.Awake();
		enemyWaveTrigger = waveTriggerCollider.GetComponent<EnemyWaveTrigger>();
		waveTriggerCollider.enabled = false;
		lightAnimator.gameObject.SetActive(value: false);
	}

	protected override void LoadBrokenState()
	{
		base.LoadBrokenState();
		base.transform.GetChild(0).gameObject.SetActive(value: false);
		SetColliderActive(value: false);
	}

	protected override List<int> GetDisableEnterStates()
	{
		List<int> disableEnterStates = base.GetDisableEnterStates();
		disableEnterStates.Add(4);
		return disableEnterStates;
	}

	protected override IEnumerator MoveRoutine_Internal(Rigidbody2D rb, Vector2 endPos, float speed, CancellationToken token)
	{
		DeactivatePreviousPath();
		PlayMoveFXs();
		yield return rb.Move(AreaBox.Center, 0f, speed, token);
		ModifyElevatorToPath();
		Game.Instance.UIManager.FieldCanvas.ShowAllAsync(instant: true).Forget();
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(EntranceElevator.CurrentFieldID.SectorName, out var data) && data.TryGetObjectStateNum(enemyWaveTrigger.ID, out var state) && state != 0)
		{
			DeactivatePreviousPath();
			yield return rb.Move(endPos, 0f, speed, token);
			StopMoveFXs();
			ModifyElevatorToPath();
			yield break;
		}
		yield return EventRoutine(speed);
		DeactivatePreviousPath();
		myosoCutsceneProducer.SetActionOnElevatorStop(StopMoveFXs);
		OnCompleteMoving();
		yield return UniTask.ToCoroutine(async delegate
		{
			await myosoCutsceneProducer.PlayTimelineAsync(0, blockControl: true);
		});
		ModifyElevatorToPath();
	}

	private IEnumerator EventRoutine(float speed)
	{
		waveTriggerCollider.enabled = true;
		backgroundController.Move(speed, enemyWaveTrigger.AreaBox, base.transform.position);
		elevatorSpine.SetAnimation("scene/down_loop");
		yield return new WaitUntil(() => enemyWaveTrigger.IsDone);
		DebugLog.GeneralLog("엘리베이터 전투가 종료되었습니다.");
	}
}
