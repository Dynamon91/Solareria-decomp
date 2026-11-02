using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FSM;
using Library;
using SPR.UI.Field;
using UnityEngine;

namespace SPR;

public class PlayerTeleportState : PlayerAsyncState
{
	public const int TeleportChargeFrames = 180;

	private PoolingObject returnStartVfx;

	private PoolingObject returnEndVfx;

	private EventInstance returnInstance;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerTeleportState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (psm.RollingInputItem.CanExecute(Time.time))
		{
			psm.RollingInputItem.Execute();
		}
	}

	protected override void OnEnter(IState prevState)
	{
	}

	protected override void OnExit(IState nextState)
	{
		FieldCanvas.TeleportBoard.TryRemoveControlOfThis();
		if (FieldCanvas.TeleportBoard.IsTeleporting)
		{
			PlayerMover.PSM.SetStateOptionActive(StateOption.Immortal, active: false);
		}
		if (returnStartVfx != null)
		{
			ParticleSystem p = returnStartVfx.transform.GetChild(0).GetComponent<ParticleSystem>();
			p.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			UniTask.WaitWhile(() => p.IsAlive(withChildren: true)).GetAwaiter().OnCompleted(delegate
			{
				returnStartVfx.ReturnToPool();
			});
		}
		if (returnInstance.isValid())
		{
			returnInstance.stop(STOP_MODE.ALLOWFADEOUT);
			returnInstance.release();
		}
		if (returnEndVfx != null)
		{
			ParticleSystem p2 = returnEndVfx.transform.GetChild(0).GetComponent<ParticleSystem>();
			p2.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			UniTask.WaitWhile(() => p2.IsAlive(withChildren: true)).GetAwaiter().OnCompleted(delegate
			{
				returnEndVfx.ReturnToPool();
			});
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.SetAnimation("return_start", loop: false);
		PlayerMover.AddAnimation("return_loop");
		returnStartVfx = Game.Instance.ObjectPoolManager.GetObject<PoolingObject>("Player_Return_Start.prefab", PlayerMover.transform.position, PlayerMover.transform);
		returnInstance = SoundManager.PlayOneShotAttached("event:/SFX/Player/System/SFX_Player_Recall_Start", PlayerMover.transform);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(180, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.PSM.TeleportInputItem.IsSuccessCharging = true;
		if (IsUnableArea())
		{
			PlayerMover.PSM.TeleportInputItem.IsSuccessCharging = false;
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_Teleport_Warning_UnableArea");
			psm.ChangeState(psm.IdleState);
			return;
		}
		await InputManager.StackControlTarget(FieldCanvas.TeleportBoard);
		PlayerMover.PSM.TeleportInputItem.IsSuccessCharging = false;
		await UniTaskExtension.WaitUntilOrCancel(() => FieldCanvas.TeleportBoard.IsTeleporting | InputManager.IsCurrentControlTarget(PlayerState.PlayerController), token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		if (FieldCanvas.TeleportBoard.IsTeleporting)
		{
			PlayerMover.SetAnimation("return_end", loop: false);
			returnEndVfx = Game.Instance.ObjectPoolManager.GetObject<PoolingObject>("Player_Return_End.prefab", PlayerMover.transform.position, PlayerMover.transform);
			SoundManager.PlayOneShotAttached("event:/SFX/Player/System/SFX_Player_Recall_End", PlayerMover.transform);
			PlayerMover.PSM.SetStateOptionActive(StateOption.Immortal, active: true);
			await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
			if (token.IsCancellationRequested)
			{
				return;
			}
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(16, token);
		}
		psm.ChangeState(psm.IdleState);
	}

	private static bool IsUnableArea()
	{
		return Game.Instance.CurrentFieldID.SectorName == SectorName.SolinLab;
	}
}
