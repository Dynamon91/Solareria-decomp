using UnityEngine;

namespace SPR;

public class AshBlastInputItem : StaminaInputItem
{
	private readonly float earlyInputAcceptTime;

	private bool inputCombo;

	private int attackCount;

	private const int AttackMaxCount = 3;

	public AshBlastInputItem(IState state)
		: base(state)
	{
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.skillB)
		{
			base.InputTime = currentTime;
			inputCombo = attackCount > 0;
			SetInputThisFrame();
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (attackCount != 0)
		{
			return inputCombo;
		}
		if (base.LastSuccessTime > InputItem.PlayerMover.LastGroundedTime)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyInputAcceptTime)
		{
			return false;
		}
		if (!CheckStaminaCondition())
		{
			return false;
		}
		return true;
	}

	public void ResetInput()
	{
		attackCount = 0;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		inputCombo = false;
		attackCount = (int)Mathf.Repeat(++attackCount, 3f);
	}
}
