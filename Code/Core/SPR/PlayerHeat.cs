using System;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Achievement;
using SPR.UI.Field;

namespace SPR;

public class PlayerHeat
{
	public class LastEmberHandler : IHeatEffect
	{
		public HeatSkillType HeatSkillType => HeatSkillType.LastEmber;

		public bool IsActive { get; private set; }

		public event Action OnUse = delegate
		{
		};

		public void Activate()
		{
			IsActive = true;
			PlayerMover.PSM.ChangeState(new PlayerLastEmberState(PlayerMover.PSM));
			PlaySucceededEventAsync().Forget();
		}

		public void Deactivate()
		{
			IsActive = false;
			PlayerStatusBoard.HeatBoard.ResetLastEmberEffect();
		}

		private async UniTask PlaySucceededEventAsync()
		{
			await UniTask.WaitUntil(() => Game.Instance.Player.mover.GetCurrentAnim(0).Name == "last_ember");
			PlayerStatusBoard.HeatBoard.PlayLastEmberParticle();
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(75);
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Ingame/SFX_UI_LastEmber_Feedback");
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(25);
			this.OnUse?.Invoke();
			PlayerStat.AddHeatGauge(0f - PlayerStat.HeatGauge);
			PlayerStatusBoard.HeatBoard.PlayHeatLevelLastEmberSequence();
		}
	}

	public const int InitialActiveLevel = 2;

	public const int TotalHeatLevel = 5;

	private static float heatGainSpeed;

	public static LastEmberHandler LastEmber;

	public ClampedInt Level { get; private set; } = new ClampedInt(-1, -1, 0);

	public ClampedFloat Gauge { get; private set; }

	public static bool OnLastEmber
	{
		get
		{
			return LastEmber.IsActive;
		}
		set
		{
			if (value)
			{
				LastEmber.Activate();
			}
			else
			{
				LastEmber.Deactivate();
			}
		}
	}

	private static PlayerStatusBoard PlayerStatusBoard => Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private static PlayerController Player => Game.Instance.Player;

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerHeat()
	{
		Level = new ClampedInt(-1, -1, 0);
		Gauge = new ClampedFloat(0f, 0f, 0f);
		heatGainSpeed = 1f;
		LastEmber = new LastEmberHandler();
	}

	public void Initialize(int current, int max)
	{
		if (max > 0)
		{
			SetLevel_Internal(current, max);
			UpdateUI();
		}
	}

	public void SetLevel(int current, int max)
	{
		int value = Level.Value;
		SetLevel_Internal(current, max);
		OnLevelChanged(value, current);
		UpdateUI();
		if (max == 5)
		{
			AchievementHelper.Unlock("UnlockAllHeatCore");
		}
	}

	private void SetLevel_Internal(int current, int max)
	{
		int value = Level.Value;
		if (current < 0)
		{
			current = 0;
		}
		Level = new ClampedInt(current, 0, max);
		float current2 = ((value != current) ? ((float)GetTotalGauge(current)) : Gauge.Value);
		Gauge = new ClampedFloat(current2, 0f, GetTotalGauge(max));
	}

	public static void SetGainSpeed(float value)
	{
		heatGainSpeed = value;
	}

	public void AddGauge(float amount)
	{
		if (amount > 0f)
		{
			amount *= heatGainSpeed;
		}
		Gauge += amount;
		int num = 0;
		int num2 = 0;
		for (int i = 1; i <= Level.Max; i++)
		{
			num2 += GetMaxGaugeOfLevel(i);
			if (Gauge.Value < (float)num2)
			{
				break;
			}
			num = i;
		}
		int value = Level.Value;
		if (num != value)
		{
			int num3 = num - value;
			Level += num3;
			OnLevelChanged(value, Level.Value);
		}
		UpdateUI();
	}

	public void ResetGauge(bool isFulled)
	{
		if (isFulled)
		{
			AddGauge(Gauge.Max);
			int value = ((Level.Max > 2) ? 1 : 0);
			SoundManager.PlayOneShotWithParameter("event:/SFX/Player/Fever/SFX_FeverLevelUp", "FeverLevel", value);
		}
		else
		{
			int value2 = Level.Value;
			Level -= Level.Value;
			Gauge -= Gauge.Value;
			OnLevelChanged(value2, Level.Value);
			UpdateUI();
		}
	}

	public void OnFixedUpdate()
	{
	}

	private void OnLevelChanged(int pre, int current)
	{
		if (current > pre)
		{
			if (pre > -1)
			{
				SetHeatEffectActive(value: true);
			}
		}
		else if (current < pre && current < 1)
		{
			SetHeatEffectActive(value: false);
		}
		DebugLog.GeneralLog($"열기 레벨 : {current}");
	}

	private void UpdateUI()
	{
		PlayerStatusBoard.UpdateHeatBoard(Level.Max, Gauge.Value);
	}

	private static void SetHeatEffectActive(bool value)
	{
		if (!value)
		{
			foreach (InputItem heatEffectInputItem in PlayerMover.PSM.HeatEffectInputItems)
			{
				heatEffectInputItem.Active = false;
			}
			return;
		}
		if (OnLastEmber)
		{
			return;
		}
		foreach (InputItem heatEffectInputItem2 in PlayerMover.PSM.HeatEffectInputItems)
		{
			if (!(heatEffectInputItem2 is ParryActionInputItem) && !heatEffectInputItem2.Active && CanActivateSkill(heatEffectInputItem2.CastObject<IHeatEffect>().HeatSkillType))
			{
				heatEffectInputItem2.Active = true;
			}
		}
	}

	public static int GetTotalGauge(int maxLevel)
	{
		int num = 0;
		for (int i = 1; i <= maxLevel; i++)
		{
			num += GetMaxGaugeOfLevel(i);
		}
		return num;
	}

	public static int GetMaxGaugeOfLevel(int targetLevel)
	{
		return 1;
	}

	public static bool CanActivateSkill(HeatSkillType type)
	{
		return GetHeatSkillOpenLevel(type) <= PlayerStat.HeatMaxLevel;
	}

	public static bool CanUseSkill(HeatSkillType type)
	{
		if (!CanActivateSkill(type))
		{
			return false;
		}
		if (type == HeatSkillType.LastEmber && OnLastEmber)
		{
			return false;
		}
		return GetHeatSkillUsage(type) <= PlayerStat.HeatLevel;
	}

	public static HeatSkillType GetCurrentUnlockSkillType(int level)
	{
		return level switch
		{
			2 => HeatSkillType.Ignition, 
			3 => HeatSkillType.HyperPyronAction, 
			4 => HeatSkillType.HyperBurst, 
			5 => HeatSkillType.LastEmber, 
			_ => throw new ArgumentOutOfRangeException("level", level, null), 
		};
	}

	public static int GetHeatSkillUsage(HeatSkillType type)
	{
		return type switch
		{
			HeatSkillType.Ignition => 1, 
			HeatSkillType.HyperPyronAction => 1, 
			HeatSkillType.HyperBurst => 2, 
			HeatSkillType.LastEmber => 1, 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
	}

	private static int GetHeatSkillOpenLevel(HeatSkillType type)
	{
		return type switch
		{
			HeatSkillType.Ignition => 2, 
			HeatSkillType.HyperPyronAction => 3, 
			HeatSkillType.HyperBurst => 4, 
			HeatSkillType.LastEmber => 5, 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
	}
}
