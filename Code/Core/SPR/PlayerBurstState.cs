using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using SPR.FSM;
using UnityEngine;

namespace SPR;

public class PlayerBurstState : PlayerAsyncState, ITaskValid
{
	private readonly AttackTask attackTask;

	private const float GroundAcceptDistance = 2f;

	public bool IsTaskValid
	{
		get
		{
			if (attackTask != null)
			{
				return attackTask.IsValid();
			}
			return false;
		}
	}

	public PlayerBurstState(PlayerStateMachine machine, AttackTask attackTask)
		: base(machine)
	{
		this.attackTask = attackTask;
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Immortal | StateOption.BlockHeavyCharge, active: true);
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(attackTask.StateOption | StateOption.Immortal | StateOption.BlockHeavyCharge, active: false);
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		Enemy enemy = ((attackTask is BurstAttackTask) ? psm.BurstInputItem.TargetEnemy : psm.HyperBurstInputItem.TargetEnemy);
		PlayerMover.ResetRelativeVelocity();
		await attackTask.Attack(token);
		if (token.IsCancellationRequested)
		{
			psm.BurstInputItem.TargetEnemy = null;
			return;
		}
		if (enemy.IsInGroggyState)
		{
			enemy.OnHitBurst();
			enemy.StateMachine.ChangeState(new GroggyEndState(enemy));
		}
		psm.BurstInputItem.TargetEnemy = null;
		psm.ChangeState(psm.IdleState);
	}

	protected override void OnFixedUpdate(float dt)
	{
		attackTask.FixedUpdate();
		if (PathFinder.FindNode(PlayerMover.VisualBottom).Shift(count: Mathf.FloorToInt((float)PlayerMover.FacingDirection.GetSignX() * 2f), sign: PlayerMover.FacingDirection.GetSignX(), allowNull: true) == null)
		{
			PlayerMover.HorizontalMove(0f);
		}
	}

	public void SetSkillID(string firstID, string secondID)
	{
		AttackTask attackTask = this.attackTask;
		if (!(attackTask is BurstAttackTask burstAttackTask))
		{
			if (attackTask is HyperBurstAttackTask hyperBurstAttackTask)
			{
				hyperBurstAttackTask.SetSkillID(firstID, secondID);
			}
		}
		else
		{
			burstAttackTask.SetSkillID(firstID, secondID);
		}
	}
}
