using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.FSM.Base;

namespace SPR;

public class HaperAmbushState : CreatureState
{
	private readonly Haper haper;

	private Sequence jumpSequence;

	private bool isFirst;

	public HaperAmbushState(Creature enemy)
		: base(enemy)
	{
		haper = enemy.CastObject<Haper>();
		isFirst = true;
	}

	public override void Enter(IState prevState)
	{
		if (isFirst)
		{
			haper.SetAnimation("idle");
			isFirst = false;
		}
		else
		{
			SetAnimation().Forget();
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (haper.IsAnimComplete(0) && haper.IsPlayerInSightBound())
		{
			ChangeState(haper.JumpState);
		}
	}

	private async UniTaskVoid SetAnimation()
	{
		haper.SetAnimation("hide", loop: false);
		await UniTask.WaitUntil(() => haper.IsAnimComplete(0), PlayerLoopTiming.Update, haper.OnDieToken);
		haper.SetAnimation("idle");
	}
}
