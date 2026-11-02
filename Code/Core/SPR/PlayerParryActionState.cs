using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;

namespace SPR;

public class PlayerParryActionState : PlayerAsyncState, ITaskValid
{
	private readonly ParryActionTask attackTask;

	private List<Enemy> targetEnemies;

	public static bool IsCurrentState { get; private set; }

	public bool IsTaskValid
	{
		get
		{
			ParryActionTask parryActionTask = attackTask;
			if (parryActionTask != null)
			{
				if (parryActionTask is PyronAttackTask pyronAttackTask)
				{
					return pyronAttackTask.IsValid() || pyronAttackTask.InProgress;
				}
				return attackTask.IsValid();
			}
			return false;
		}
	}

	public PlayerParryActionState(PlayerStateMachine machine, ParryActionTask task)
		: base(machine)
	{
		attackTask = task;
	}

	public void SetTargetEnemies(List<Enemy> enemies)
	{
		targetEnemies = enemies;
		attackTask.SetEnemies(targetEnemies);
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: true);
		PlayerMover.Flip(psm.MoveDirectionInt(includeFacing: true));
		IsCurrentState = true;
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await attackTask.Attack(token);
		if (!token.IsCancellationRequested)
		{
			psm.ChangeState(psm.IdleState);
		}
	}

	protected override void OnFixedUpdate(float fdt)
	{
		attackTask.FixedUpdate();
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(attackTask.StateOption | StateOption.BlockHeavyCharge, active: false);
		PlayerMover.ResetRelativeVelocity();
		targetEnemies.Clear();
		IsCurrentState = false;
	}

	public void SetFlameID(string flameID)
	{
		attackTask.SetFlameID(flameID);
	}

	public void SetSkillID(string skillID)
	{
		attackTask.SetSkillID(skillID);
	}
}
