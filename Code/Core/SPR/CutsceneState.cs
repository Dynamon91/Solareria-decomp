using Cysharp.Threading.Tasks;

namespace SPR;

public class CutsceneState : IState
{
	protected readonly BossCharacter Boss;

	protected readonly int StateIndex;

	protected readonly bool BlockControl;

	protected readonly bool HideCanvas;

	protected CutsceneProducer Producer;

	public CutsceneState(BossCharacter boss, int stateIndex, bool blockControl = false, bool hideCanvas = true)
	{
		Boss = boss;
		StateIndex = stateIndex;
		BlockControl = blockControl;
		HideCanvas = hideCanvas;
		if (!Boss.MyArea.TryGetCutsceneProducer(out Producer))
		{
			DebugLog.GeneralLogError(Boss.name + "을 위한 CutsceneProducer가 없습니다.");
		}
	}

	public virtual void Enter(IState prevState)
	{
		Producer.PlayTimelineAsync(StateIndex, BlockControl, HideCanvas).Forget();
	}

	public virtual void Exit(IState nextState)
	{
	}

	public void FixedUpdate(float fdt)
	{
	}
}
