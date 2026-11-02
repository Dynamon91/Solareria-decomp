namespace SPR;

public class PhaseTransitionCutsceneState : CutsceneState
{
	public PhaseTransitionCutsceneState(BossCharacter boss, int stateIndex, bool blockControl = false, bool hideCanvas = true)
		: base(boss, stateIndex, blockControl, hideCanvas)
	{
	}

	public override void Enter(IState prevState)
	{
		base.Enter(prevState);
		Boss.Groggy.ChangeValueBy(Boss.Groggy.Value + Boss.PhaseTransitionGroggyRecovery);
	}

	public override void Exit(IState nextState)
	{
		Boss.IncreasePhase();
	}
}
