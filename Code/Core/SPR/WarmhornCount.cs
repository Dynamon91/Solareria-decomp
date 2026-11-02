using System.Threading;
using LapinLibrary;
using Library;
using StaticData;

namespace SPR;

public class WarmhornCount : Stat
{
	private CancellationTokenSource healTokenSource;

	private bool isPotionCountFixed;

	public int HealAmount => (int)Stat.PlayerStat.GetStat<WarmhornHealAmount>().Max;

	public int MaxPotionCount
	{
		get
		{
			if (!isPotionCountFixed)
			{
				return (int)base.Max;
			}
			return 1;
		}
	}

	protected override bool UsingDecimal => false;

	public WarmhornCount(float initial, Difficulty difficulty)
		: base(StatType.WarmhornCount, initial, difficulty)
	{
	}

	public void AddMaxPotion(int amount)
	{
		AddInitial(amount);
		if (isPotionCountFixed && base.Current > (float)MaxPotionCount)
		{
			ChangeAmount((float)MaxPotionCount - base.Current);
		}
		UpdatePotionBoard();
	}

	protected override void OnAddInitial(float currenInitial)
	{
		Game.Instance.SaveData.PlayerData.SetPotionCount((int)currenInitial);
	}

	public void ResetPotion()
	{
		AddCurrentPotion(MaxPotionCount);
	}

	public void AddCurrentPotion(int count)
	{
		ChangeAmount(count);
		if (isPotionCountFixed && base.Current > (float)MaxPotionCount)
		{
			ChangeAmount((float)MaxPotionCount - base.Current);
		}
		UpdatePotionBoard();
	}

	public void FixPotionCount(bool shouldFix)
	{
		isPotionCountFixed = shouldFix;
		ChangeAmount((float)MaxPotionCount - base.Current);
		UpdatePotionBoard();
	}

	public void UsePotion(int count)
	{
		AddCurrentPotion(-count);
		if (Stat.PlayerStat.GetStat<WarmhornHealAmount>().CanHealInstantly)
		{
			AddHealth(count * HealAmount, healInstantly: true);
		}
		else
		{
			HealAsync(TaskUtil.RefreshToken(ref healTokenSource));
		}
		static void AddHealth(int value, bool healInstantly)
		{
			Stat.PlayerStat.ResidualHeat.RemoveResidualHeat(value);
			Stat.PlayerStat.GetStat<Health>().AddHealth(value, healInstantly);
		}
		async void HealAsync(CancellationToken token)
		{
			Stat.FieldCanvas.PlayerStatusBoard.PlayHealTween(loop: true, GetType());
			(int, int) healDurationCycle = Game.Instance.Player.PlayerPart.GetHealDurationCycle();
			int durationFrame = healDurationCycle.Item1;
			int cycleFrame = healDurationCycle.Item2;
			int frameCount = 0;
			float health = 0f;
			int sum = 0;
			int endHealth = count * HealAmount;
			int num = durationFrame / cycleFrame + 1;
			int delta = endHealth / num;
			while (!token.IsCancellationRequested)
			{
				if (frameCount >= durationFrame)
				{
					if (sum < endHealth)
					{
						AddHealth(endHealth - sum, healInstantly: false);
					}
					break;
				}
				if (frameCount % cycleFrame == 0)
				{
					health += (float)delta;
					int num2 = (int)health;
					AddHealth(num2, healInstantly: false);
					sum += num2;
					health -= (float)num2;
				}
				await UniTaskExtension.WaitOneFrame();
				frameCount++;
			}
			Stat.FieldCanvas.PlayerStatusBoard.StopHealTween(GetType());
		}
	}

	public void CancelHeal()
	{
		healTokenSource?.Cancel();
	}

	public void UpdatePotionBoard()
	{
		Stat.FieldCanvas.PotionBoard.UpdatePotionBoard((int)base.Current);
	}

	protected override void OnMaxChanged()
	{
		if (isPotionCountFixed && base.Current > (float)MaxPotionCount)
		{
			ChangeAmount((float)MaxPotionCount - base.Current);
		}
		UpdatePotionBoard();
	}
}
