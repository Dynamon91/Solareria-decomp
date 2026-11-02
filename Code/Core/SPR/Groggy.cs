using SPR.Library;
using UnityEngine;

namespace SPR;

public class Groggy
{
	private readonly int initialMaxValue;

	private readonly float recoveryPerFrame;

	private readonly float stabilizeRecoveryPerFrame;

	private readonly int maxValueIncrease;

	private readonly int stabilizeFrame;

	private readonly FrameTimer stabilizeTimer;

	public int Duration { get; }

	public float Value { get; private set; }

	public int MaxValue { get; private set; }

	public float DamageMultiplier { get; } = 1f;

	public Groggy(string id)
	{
		EnemyStatData enemyStatData = Game.DataManager.GetEnemyStatData(id);
		if (enemyStatData.InitialMax <= 0)
		{
			stabilizeTimer = new FrameTimer(enemyStatData.StabilizeFrame);
			return;
		}
		initialMaxValue = enemyStatData.InitialMax;
		Duration = enemyStatData.Duration;
		MaxValue = Mathf.CeilToInt((float)initialMaxValue * Game.DataManager.GetEnemyMultiplier(Game.Instance.SaveData.ClearCount).Groggy);
		maxValueIncrease = Mathf.CeilToInt((float)enemyStatData.MaxIncrease * Game.DataManager.GetEnemyMultiplier(Game.Instance.SaveData.ClearCount).GroggyMaxIncrease);
		recoveryPerFrame = enemyStatData.RecoveryPerFrame;
		stabilizeRecoveryPerFrame = enemyStatData.StabilizeRecoveryPerFrame;
		stabilizeTimer = new FrameTimer(enemyStatData.StabilizeFrame);
		DamageMultiplier = enemyStatData.DamageMultiplier;
		Value = MaxValue;
	}

	public void OnFixedUpdate(Enemy enemy)
	{
		stabilizeTimer?.Tick();
		RecoverGroggy(enemy);
	}

	public void ChangeValueBy(float changeAmount)
	{
		if (MaxValue > 0)
		{
			Value = Mathf.Clamp(Value + changeAmount, 0f, MaxValue);
		}
	}

	public void PerfectRecoverFromGroggy()
	{
		MaxValue += maxValueIncrease;
		Value = MaxValue;
	}

	public void ResetGroggy(bool includeMax)
	{
		if (includeMax)
		{
			MaxValue = initialMaxValue;
		}
		Value = MaxValue;
	}

	public void ResetTimer()
	{
		stabilizeTimer.Reset();
	}

	private void RecoverGroggy(Enemy enemy)
	{
		if (!enemy.IsInGroggyState)
		{
			float changeAmount = (stabilizeTimer.IsRunning ? recoveryPerFrame : (recoveryPerFrame + stabilizeRecoveryPerFrame));
			ChangeValueBy(changeAmount);
		}
	}
}
