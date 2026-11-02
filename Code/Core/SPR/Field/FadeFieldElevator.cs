using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using UnityEngine;

namespace SPR.Field;

public class FadeFieldElevator : FieldElevator
{
	[Header("Fade")]
	[SerializeField]
	private float fadeWaitDuration;

	private CancellationTokenSource tokenSource;

	private static InputManager InputManager => Game.Instance.InputManager;

	public override void StartMoveToEnd()
	{
		PlayMoveAsync(TaskUtil.RefreshToken(ref tokenSource)).Forget();
	}

	protected override void OnStartMoving()
	{
		base.OnStartMoving();
		int state = ((FloorState != State.DownOrLeftFloor) ? 1 : 0);
		ChangeState(state);
	}

	private float GetFadeOutPoint(bool isMovingUp)
	{
		float num = (isMovingUp ? downFloorWorldPosY : upFloorWorldPosY);
		int num2 = (isMovingUp ? 1 : (-1));
		return num + (float)num2 * speed * 5f;
	}

	private float GetFadeInPoint(bool isMovingUp)
	{
		float num = (isMovingUp ? upFloorWorldPosY : downFloorWorldPosY);
		int num2 = (isMovingUp ? 1 : (-1));
		return num - (float)num2 * speed * 5f;
	}

	private async UniTask PlayMoveAsync(CancellationToken token)
	{
		bool isMovingUp = FloorState == State.DownOrLeftFloor;
		int dir = (isMovingUp ? 1 : (-1));
		float fadeOutPoint = GetFadeOutPoint(isMovingUp);
		float fadeOutPosY = fadeOutPoint - (float)dir * 5f;
		float endPos = fadeOutPoint + (float)dir * 5f;
		float endPosY = (isMovingUp ? upFloorWorldPosY : downFloorWorldPosY);
		BlockComplete = true;
		StartMove(endPos, ignoreWait: false, ignoreState: true);
		await UniTask.WaitUntil(() => (float)dir * (base.transform.position.y - fadeOutPosY) > 0f, PlayerLoopTiming.Update, token);
		await InputManager.BlockControlForTask(FadeOut(token), "Fade Out");
		if (token.IsCancellationRequested)
		{
			return;
		}
		float fadeInPoint = GetFadeInPoint(isMovingUp);
		Vector3 position = base.transform.position;
		float num = fadeInPoint - position.y;
		_ = base.MyArea.AreaBox;
		Vector3 position2 = Elevator.PlayerMover.transform.position;
		position2.y += num;
		base.transform.position = new Vector3(position.x, fadeInPoint);
		Game.Instance.Player.SetPosition(position2, Game.Instance.Player.mover.IsFacingRight);
		BlockComplete = false;
		StartMove(endPosY, ignoreWait: true, ignoreState: true);
		await InputManager.BlockControlForTask(FadeIn(token), "Fade In");
		if (!token.IsCancellationRequested)
		{
			await UniTask.WaitUntil(() => FloorState != State.Moving, PlayerLoopTiming.Update, token);
		}
	}

	private async UniTask FadeOut(CancellationToken token, bool waitElevator = true)
	{
		await Game.Instance.UIManager.FadeCanvas.FadeOut(0.5f, 0f, token);
		if (waitElevator)
		{
			await UniTask.WaitUntil(() => FloorState != State.Moving, PlayerLoopTiming.Update, token);
		}
		await UniTask.WaitForSeconds(fadeWaitDuration, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
	}

	private async UniTask FadeIn(CancellationToken token)
	{
		await Game.Instance.UIManager.FadeCanvas.FadeIn(0.5f, 0f, Ease.Linear, token);
	}

	protected override void OnGetOff()
	{
		if (BlockComplete)
		{
			tokenSource?.Cancel();
			bool num = base.Rb2D.velocity.y > 0f;
			float endPos = (num ? upFloorWorldPosY : downFloorWorldPosY);
			BlockComplete = false;
			StartMove(endPos, ignoreWait: true);
			Game.Instance.UIManager.FadeCanvas.FadeIn(0.25f).Forget();
			if (!num)
			{
				FadeWithPlayerAsync(TaskUtil.RefreshToken(ref tokenSource)).Forget();
			}
		}
	}

	private async UniTask FadeWithPlayerAsync(CancellationToken token)
	{
		float fadeOutPoint = GetFadeOutPoint(isMovingUp: false);
		await UniTaskExtension.WaitUntilOrCancel(() => Elevator.PlayerMover.transform.position.y < fadeOutPoint, token);
		if (!token.IsCancellationRequested)
		{
			await InputManager.BlockControlForTask(FadeOut(token, waitElevator: false), "Fade Out");
			if (!token.IsCancellationRequested)
			{
				Game.Instance.Player.SetPosition(Util.DisablePosition);
				Elevator.PlayerMover.PSM.ChangeState(Elevator.PlayerMover.PSM.IdleState);
				await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
				float fadeInPoint = GetFadeInPoint(isMovingUp: false);
				AreaBox areaBox = base.MyArea.AreaBox;
				Vector3 position = Elevator.PlayerMover.transform.position;
				position.x = Mathf.Clamp(position.x, areaBox.LowerLeft.x, areaBox.UpperRight.y);
				position.y = fadeInPoint;
				Game.Instance.Player.SetPosition(position, Elevator.PlayerMover.IsFacingRight);
				await FadeIn(token);
			}
		}
	}

	private void OnDisable()
	{
		tokenSource?.Cancel();
	}
}
