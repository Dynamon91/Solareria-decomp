using SPR.UI.Field;

namespace SPR;

public abstract class StaminaInputItem : InputItem
{
	private bool inputThisFrame;

	private static PlayerStatusBoard PlayerStatusBoard => Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard;

	protected StaminaInputItem(IState state)
		: base(state)
	{
	}

	protected bool CheckStaminaCondition()
	{
		bool num = InputItem.PlayerController.PlayerStat.GetStat<Stamina>().Current > 0f;
		if (!num && inputThisFrame)
		{
			PlayerStatusBoard.PlayNoStaminaEffect();
		}
		inputThisFrame = false;
		return num;
	}

	protected void SetInputThisFrame()
	{
		inputThisFrame = true;
	}
}
