using Cysharp.Threading.Tasks;
using Library;

namespace SPR.CoreStone;

public class GuardPassive : CoreStonePassive
{
	private PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private PlayerStateMachine PSM => Game.Instance.Player.mover.PSM;

	protected override void ApplyFirstPassive()
	{
		PlayerStat.GetStat<GuardDamageReduceRate>().AddTraitPassive(5f);
	}

	protected override void ApplySecondPassive()
	{
		PSM.BrokenGuardState.CastObject<PlayerBrokenGuardState>().OnBroken += ApplyDefenseOnBrokenGuard;
	}

	protected override void ResetFirstPassive()
	{
		PlayerStat.GetStat<GuardDamageReduceRate>().AddTraitPassive(-5f);
	}

	protected override void ResetSecondPassive()
	{
		PSM.BrokenGuardState.CastObject<PlayerBrokenGuardState>().OnBroken -= ApplyDefenseOnBrokenGuard;
	}

	private void ApplyDefenseOnBrokenGuard()
	{
		ApplyDefenseAsync().Forget();
	}

	private async UniTaskVoid ApplyDefenseAsync()
	{
		Defense defense = PlayerStat.GetStat<Defense>();
		defense.AddTraitPassiveRate(0.5f);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(180);
		defense.AddTraitPassiveRate(-0.5f);
	}
}
