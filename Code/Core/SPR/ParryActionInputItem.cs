using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SPR;

public abstract class ParryActionInputItem : InputItem
{
	private CancellationTokenSource bulletTimeTokenSource;

	private List<Enemy> targetEnemies;

	protected abstract Func<bool> getInput { get; }

	protected ParryActionInputItem(IState state)
		: base(state)
	{
	}

	public override void UpdateInput(float currentTime)
	{
		if (Game.Instance.InputManager.IsCurrentControlTarget(InputItem.PlayerController) && getInput())
		{
			base.InputTime = currentTime;
			if (CanExecute(currentTime))
			{
				Execute();
			}
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (PlayerParryActionState.IsCurrentState)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > Time.fixedDeltaTime)
		{
			return false;
		}
		return true;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		((PlayerParryActionState)base.TargetState).SetTargetEnemies(targetEnemies.ToList());
		base.Active = false;
	}

	protected override void OnActiveChanged()
	{
		if (!base.Active)
		{
			Game.Instance.GraphicManager.ParryActionDirector.CancelPlayingTimeline();
			targetEnemies?.Clear();
		}
	}

	public void AddEnemy(Enemy enemy)
	{
		if (targetEnemies == null)
		{
			targetEnemies = new List<Enemy>();
		}
		targetEnemies.Add(enemy);
	}
}
