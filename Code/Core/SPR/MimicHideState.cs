using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class MimicHideState : CreatureState
{
	private readonly Mimic mimic;

	private readonly FrameTimer timer;

	private bool isAnimPlaying;

	public MimicHideState(Creature enemy, int waitFrame)
		: base(enemy)
	{
		mimic = enemy.CastObject<Mimic>();
		timer = new FrameTimer(waitFrame);
	}

	public override void Enter(IState prevState)
	{
		isAnimPlaying = false;
		mimic.SetAnimation("hiding");
		timer.Reset();
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (mimic.Health < mimic.MaxHealth && !isAnimPlaying)
		{
			EscapeHide(mimic.PatrolState).Forget();
		}
		if (mimic.IsPlayerInSwingBounds() && !isAnimPlaying)
		{
			if (timer.IsDone)
			{
				EscapeHide(new AttackState(mimic, () => new List<AttackSequence> { mimic.Attack }, forceAttack: true)).Forget();
			}
			timer.Tick();
		}
		else
		{
			timer.Reset();
		}
	}

	private async UniTaskVoid EscapeHide(EnemyState state)
	{
		isAnimPlaying = true;
		mimic.SetAnimation("acting", loop: false);
		Game.Instance.SoundManager.PlayOneShotAttached(mimic.AppearEvent, mimic.transform);
		await UniTask.WaitUntil(() => mimic.IsAnimComplete(0));
		ChangeState(state);
	}
}
