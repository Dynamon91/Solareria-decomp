using Cysharp.Threading.Tasks;
using Library;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class ShellzarRegenerateState : CreatureState
{
	private readonly int ratio;

	private readonly FrameTimer timer;

	private UniTask task;

	public ShellzarRegenerateState(Shellzar enemy, int cycle, int ratio)
		: base(enemy)
	{
		timer = new FrameTimer(cycle);
		this.ratio = ratio;
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SetAnimation("regeneration_idle");
		base.Enemy.CastObject<Creature>().PlayHealTween(loop: true, base.Enemy.GetType());
	}

	public override void Exit(IState nextState)
	{
		base.Enemy.CastObject<Creature>().StopHealTween(base.Enemy.GetType());
	}

	public override void FixedUpdate(float fdt)
	{
		string name = base.Enemy.GetCurrentAnim(0).Name;
		if (name == "regeneration_idle_end" || name == "regeneration_stiff")
		{
			base.Enemy.CastObject<Creature>().StopHealTween(base.Enemy.GetType());
			return;
		}
		if (timer.IsDone)
		{
			timer.Reset();
			base.Creature.CastObject<Shellzar>().HealthRegen(ratio);
		}
		if (base.Creature.Health >= base.Creature.MaxHealth)
		{
			base.Enemy.SetAnimation("regeneration_idle_end", loop: false);
			base.Enemy.CastObject<Creature>().StopHealTween(base.Enemy.GetType());
		}
		else
		{
			timer.Tick();
		}
	}
}
