namespace SPR;

public class StatEffect : PartEffect
{
	private float appliedValues;

	protected readonly bool HasValue;

	private bool isFull;

	private StatType StatType { get; }

	protected float Value { get; set; }

	protected float Rate { get; set; }

	private float MaxValue { get; }

	public bool IsApplied => appliedValues > 0f;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public StatEffect(PartEffectData data, int level)
	{
		StatType = data.StatType;
		HasValue = data.Values.Count != 0;
		if (HasValue)
		{
			Value = data.Values[level];
		}
		else
		{
			Rate = data.Rates[level] / 100f;
		}
		MaxValue = ((data.MaxValues.Count == 0) ? 999f : (HasValue ? data.MaxValues[level] : (data.MaxValues[level] / 100f)));
	}

	public void Apply()
	{
		if (!isFull)
		{
			float num = (HasValue ? Value : Rate);
			if (appliedValues + num >= MaxValue)
			{
				num = MaxValue - appliedValues;
				isFull = true;
			}
			Stat stat = PlayerStat.GetStat(StatType);
			if (HasValue)
			{
				stat.AddPartValue(num);
				DebugLog.GeneralLog($"파츠 효과: {StatType} +{num}");
			}
			else
			{
				float current = stat.Current;
				stat.AddPartRate(num);
				DebugLog.GeneralLog($"파츠 효과: {StatType} +{num * 100f}% ({current * (1f + num)})");
			}
			appliedValues += num;
		}
	}

	public void Clear()
	{
		if (IsApplied)
		{
			Stat stat = PlayerStat.GetStat(StatType);
			if (HasValue)
			{
				stat.AddPartValue(0f - appliedValues);
				DebugLog.GeneralLog($"파츠 효과: {StatType} -{appliedValues}");
			}
			else
			{
				float current = stat.Current;
				stat.AddPartRate(0f - appliedValues);
				DebugLog.GeneralLog($"파츠 효과: {StatType} -{appliedValues * 100f}% ({current / (1f + Rate)})");
			}
			appliedValues = 0f;
			isFull = false;
		}
	}

	public void SetValue(float value)
	{
		Value = value;
	}
}
