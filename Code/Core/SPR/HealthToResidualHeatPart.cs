using SPR.UI.Menu;
using SPR.UI.Rest;

namespace SPR;

public class HealthToResidualHeatPart : SimpleStatEffectPart
{
	private int debuffValue;

	private int debuffCycleFrame;

	private int currentFrame;

	private static InputManager InputManager => Game.Instance.InputManager;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		debuffValue = (int)partEffectData.Values[base.Level];
		debuffCycleFrame = partEffectData.CycleFrame;
		currentFrame = 0;
	}

	public override void Reset()
	{
		currentFrame = 0;
	}

	public override void OnFixedUpdate()
	{
		if ((InputManager.ControlTarget == null || InputManager.IsCurrentControlTarget(Game.Instance.Player) || InputManager.IsCurrentControlTarget(Game.Instance.DevController) || InputManager.ControlTarget is MenuPanel) && !InputManager.ContainControlTarget(typeof(RestCanvas)) && Game.Instance.Player.PlayerStat.GetStat<Health>().CurrentHealth > 0)
		{
			currentFrame++;
			if (currentFrame >= debuffCycleFrame)
			{
				Game.Instance.Player.PlayerStat.GetStat<Health>().AddHealth(-debuffValue);
				Game.Instance.Player.PlayerStat.ResidualHeat.AddAmount(debuffValue);
				currentFrame = 0;
			}
		}
	}
}
