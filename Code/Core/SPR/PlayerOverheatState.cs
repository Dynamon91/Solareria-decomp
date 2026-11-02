using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;

namespace SPR;

public class PlayerOverheatState : PlayerAsyncState
{
	private const string StartAnimName = "overheat_start";

	private const string LoopAnimName = "overheat_loop";

	private const string EndAnimName = "overheat_end";

	private const string VFXName = "overheat_slash";

	private int heatLevel;

	public PlayerOverheatState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		heatLevel = PlayerStat.HeatLevel;
		PlayerStat.AddHeatGauge(-PlayerHeat.GetHeatSkillUsage(HeatSkillType.Ignition));
		psm.SetStateOptionActive(StateOption.Immortal, active: true);
		PlayerMover.ResetRelativeVelocity();
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Immortal, active: false);
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		PlayerMover.EnableVFX("overheat_slash");
		PlayerMover.SetAnimation("overheat_start", loop: false, 1f, 0, 0f, vfxEnable: false);
		await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerState.PlayerController.HeatEffectController.FindHeatEffect("overheat_wave").TryInvoke(heatLevel);
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		PlayerMover.SetAnimation("overheat_loop", loop: false, 1f, 0, 0f, vfxEnable: false);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(25, token);
		if (!token.IsCancellationRequested)
		{
			PlayerMover.SetAnimation("overheat_end", loop: false, 1f, 0, 0f, vfxEnable: false);
			await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
			if (!token.IsCancellationRequested)
			{
				psm.ChangeState(psm.IdleState);
				psm.SetIgnoreDamage(20);
			}
		}
	}
}
