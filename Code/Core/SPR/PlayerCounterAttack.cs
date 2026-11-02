using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public struct PlayerCounterAttack
{
	private readonly List<IDamageable> counterTargets;

	private readonly List<IDamageable> counterDamaged;

	private readonly List<IDamageable> parriedDamageables;

	private readonly PlayerMover playerMover;

	private readonly FlameData flameData;

	private readonly string counterID;

	public PlayerCounterAttack(PlayerMover playerMover, string counterSkillID)
	{
		this.playerMover = playerMover;
		counterTargets = new List<IDamageable>();
		counterDamaged = new List<IDamageable>();
		parriedDamageables = new List<IDamageable>();
		counterID = counterSkillID;
		flameData = Game.DataManager.GetFlameData(counterID);
	}

	public void TryCounterAttack(IDamageable damageable, float multiplier = 1f)
	{
		if (parriedDamageables.Contains(damageable) && playerMover.Damaged.Contains(damageable) && !counterTargets.Contains(damageable))
		{
			counterTargets.Add(damageable);
			CounterAttackAsync(damageable, multiplier).Forget();
			DebugLog.GeneralLog("카운터 추가타 성공! 현재 대상:" + damageable.GetType().FullName);
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (IDamageable parriedDamageable in parriedDamageables)
		{
			stringBuilder.Append(parriedDamageable.GetType().FullName + " ");
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (IDamageable counterTarget in counterTargets)
		{
			stringBuilder2.Append(counterTarget.GetType().FullName + " ");
		}
		StringBuilder stringBuilder3 = new StringBuilder();
		foreach (IDamageable item in playerMover.Damaged)
		{
			stringBuilder3.Append(item.GetType().FullName + " ");
		}
		DebugLog.GeneralLog($"카운터 추가타 실패! 현재 대상:{damageable.GetType().FullName}, 패링한 적:{stringBuilder}, 이미 추가타 맞은 적:{stringBuilder2}, 플레이어가 타격한 적:{stringBuilder3}");
	}

	public void OnParried(IDamageable damageable)
	{
		if (!parriedDamageables.Contains(damageable))
		{
			parriedDamageables.Add(damageable);
		}
	}

	public bool IsParriedTarget(IDamageable damageable)
	{
		return parriedDamageables.Contains(damageable);
	}

	private async UniTask CounterAttackAsync(IDamageable damageable, float multiplier)
	{
		if (damageable is Enemy enemy)
		{
			int num = Mathf.Max(0, flameData.DelayFrame - 15);
			int shootDuration = Mathf.Min(flameData.DelayFrame, 15);
			if (num > 0)
			{
				await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(num);
			}
			for (int i = 0; i < flameData.Count; i++)
			{
				await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(flameData.CycleFrames[i]);
				HeatFlame.ShootFlame(new HeatFlame.FlameParameter(enemy, 0, multiplier, shootDuration, AttackAttribute.Counter));
			}
		}
	}
}
