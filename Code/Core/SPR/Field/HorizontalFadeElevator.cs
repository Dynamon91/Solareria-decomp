using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.Library;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class HorizontalFadeElevator : HorizontalElevator, IVcamConfiner
{
	[Header("Sequence")]
	[SerializeField]
	private GameObject vcam;

	[SerializeField]
	private GameObject leftVcam;

	[SerializeField]
	private Vector2 leftFadeInPosition;

	[SerializeField]
	private GameObject rightVcam;

	[SerializeField]
	private Vector2 rightFadeInPosition;

	[SerializeField]
	private float fadeInDuration = 1f;

	[SerializeField]
	private float fadeWaitDuration = 2f;

	private CancellationTokenSource tokenSource;

	private FieldServiceCanvas serviceCanvas;

	private Vector2 originPos;

	private Vector2 movedPosition;

	private ElevatorCallbackBase onStart;

	protected override bool IsEntranceElevator => false;

	protected override bool IsOnLeft => base.transform.position.x < leftFadeInPosition.x;

	public bool ConfineLookAround => true;

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector2 size = floorCollider.size;
		Vector3 center = floorCollider.bounds.center;
		Vector2 collidePosition = PhysicsUtility.GetCollidePosition(Vector2.right, (Vector2)center + size.x * Vector2.right, size, 600f);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(collidePosition, size);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(leftFadeInPosition + floorCollider.offset, size);
		Gizmos.DrawWireCube(rightFadeInPosition + floorCollider.offset, size);
	}

	protected override void Awake()
	{
		base.Awake();
		originPos = base.transform.position;
		Vector2 size = floorCollider.size;
		Vector3 center = floorCollider.bounds.center;
		movedPosition = new Vector2(PhysicsUtility.GetCollidePosition(Vector2.right, (Vector2)center + size.x * Vector2.right, size, 600f).x, originPos.y);
		leftPosition = originPos;
		rightPosition = movedPosition;
		onStart = GetComponent<ElevatorCallbackBase>();
	}

	public override void StartMoveToEnd()
	{
		PlayMoveAsync().Forget();
	}

	public override void OnEndRide()
	{
		if (Game.Instance.CurrentFieldID.Equals(SectorName.AbandonedTunnels, 22))
		{
			onStart?.Invoke();
		}
	}

	protected override void FixedUpdate()
	{
		if (!Game.Instance.UIManager.FadeCanvas.IsBlack)
		{
			base.FixedUpdate();
		}
	}

	private async UniTask PlayMoveAsync()
	{
		SetTransparentWallActive(active: true);
		CancellationToken token = TaskUtil.RefreshToken(ref tokenSource);
		Game.Instance.UIManager.FieldCanvas.HideAllAsync(instant: false).Forget();
		SetVcamOffsetIfExist(vcam);
		vcam.SetActive(value: true);
		await UniTask.WaitForSeconds(1f, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		bool isMovingRight = FloorState == State.DownOrLeftFloor;
		float endPosX = (isMovingRight ? movedPosition.x : originPos.x);
		int dir = (isMovingRight ? 1 : (-1));
		float num = -205f;
		float fadeOutPosX = num - (float)dir * 5f;
		float endPos = num + (float)dir * 5f;
		BlockComplete = true;
		IsMovingWithFade = false;
		StartMove(endPos, ignoreWait: false, ignoreState: true);
		IsMovingWithFade = true;
		await UniTask.WaitUntil(() => (float)dir * (base.transform.position.x - fadeOutPosX) > 0f, PlayerLoopTiming.Update, token);
		await Game.Instance.UIManager.FadeCanvas.FadeOut(0.25f, 0f, token);
		await UniTask.WaitUntil(() => FloorState != State.Moving, PlayerLoopTiming.Update, token);
		GameObject endVcam = (isMovingRight ? rightVcam : leftVcam);
		SetVcamOffsetIfExist(endVcam);
		endVcam.SetActive(value: true);
		vcam.SetActive(value: false);
		Vector3 position = Elevator.PlayerMover.transform.position;
		Vector2 vector = (isMovingRight ? rightFadeInPosition : leftFadeInPosition);
		float num2 = vector.x - base.transform.position.x;
		position.x += num2;
		base.transform.position = vector;
		Game.Instance.Player.SetPosition(position, num2 > 0f);
		await UniTask.WaitForSeconds(fadeWaitDuration, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		BlockComplete = false;
		StartMove(endPosX, ignoreWait: true, ignoreState: true);
		await Game.Instance.UIManager.FadeCanvas.FadeIn(fadeInDuration, 0f, Ease.Linear, token);
		IsMovingWithFade = false;
		await UniTask.WaitUntil(() => FloorState != State.Moving, PlayerLoopTiming.Update, token);
		endVcam.SetActive(value: false);
		lightSpriteRenderer.DOAlpha(0f, 0.5f);
		await Game.Instance.UIManager.FieldCanvas.ShowAllAsync(instant: false);
	}

	private void OnDisable()
	{
		tokenSource?.Cancel();
	}

	private static void SetVcamOffsetIfExist(GameObject vcamObject)
	{
		CinemachineCameraOffset component = vcamObject.GetComponent<CinemachineCameraOffset>();
		if (!component.IsNull())
		{
			component.m_Offset = Vector3.zero;
			component.m_ApplyAfter = CinemachineCore.Stage.Aim;
		}
	}
}
