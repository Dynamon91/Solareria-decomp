using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerSpecialParryState : PlayerAsyncState, ICounter
{
	private const string AnimName = "special_parrying";

	public const string CounterID = "heat_parry_counter";

	private const string SkillDataID = "heat_parry";

	private const string BurnDataID = "heat_parry_burn";

	private string counterID;

	private bool canRolling;

	private List<IDamageable> damaged;

	public PlayerCounterAttack CounterAttack { get; set; }

	private AttackAttribute DefaultAttribute => AttackAttribute.Default | AttackAttribute.BlockStuck;

	public PlayerSpecialParryState(PlayerStateMachine machine)
		: base(machine)
	{
		counterID = "heat_parry_counter";
	}

	public void OnParried(IDamageable damageable)
	{
		CounterAttack.OnParried(damageable);
		if (!damaged.Contains(damageable))
		{
			AttackAttribute attackAttribute = DefaultAttribute | AttackAttribute.Counter;
			PlayerMover.Attack(new PlayerMover.AttackParameter(damageable, "heat_parry", 0, attackAttribute));
			OnAttack(damageable);
		}
	}

	public int GetStunCount(Enemy enemy, AttackAttribute attackAttribute)
	{
		return enemy.MaxStunCount;
	}

	public void ChangeID(string id)
	{
		counterID = id;
	}

	protected override void OnEnter(IState prevState)
	{
		CounterAttack = new PlayerCounterAttack(PlayerMover, counterID);
		canRolling = true;
		damaged = new List<IDamageable>();
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Parry, active: false);
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.SetAnimation("special_parrying", loop: false);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.heatParryPredelayFrame, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		canRolling = false;
		PlayerMover.ClearDamagedList();
		psm.SetStateOptionActive(StateOption.Parry, active: true);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.heatParryCounterFrame, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		psm.SetStateOptionActive(StateOption.Parry, active: false);
		for (int i = PlayerAsyncState.pcp.heatParryCounterFrame; i < PlayerAsyncState.pcp.heatParryAttackFrame; i++)
		{
			PlayerMover.HandleAttackHitBox("heat_parry", 0, DefaultAttribute);
			foreach (IDamageable item in PlayerMover.Damaged)
			{
				if (!damaged.Contains(item))
				{
					OnAttack(item);
				}
			}
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return;
			}
		}
		await UniTaskExtension.WaitUntilOrCancel(() => PlayerMover.IsAnimComplete(0), token);
		if (!token.IsCancellationRequested)
		{
			psm.ChangeState(psm.IdleState);
		}
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (canRolling && psm.RollingInputItem.CanExecute(Time.time))
		{
			psm.RollingInputItem.Execute();
		}
		else if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
		}
	}

	private void OnAttack(IDamageable damageable)
	{
		damaged.Add(damageable);
		CounterAttack.TryCounterAttack(damageable);
		if (damageable is Enemy enemy)
		{
			PlayerState.PlayerStat.MakeEnemyBurn(enemy, "heat_parry_burn");
		}
	}
}
