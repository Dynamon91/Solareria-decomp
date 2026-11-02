namespace SPR.CoreStone;

public abstract class CoreStonePassive
{
	private int appliedRank;

	public void ApplyPassive(int rank)
	{
		ResetPassive();
		for (int i = 2; i <= rank; i++)
		{
			switch (i)
			{
			case 2:
				ApplyFirstPassive();
				break;
			case 3:
				ApplySecondPassive();
				break;
			}
		}
		appliedRank = rank;
	}

	public void Reset()
	{
		ResetPassive();
		appliedRank = 0;
	}

	private void ResetPassive()
	{
		if (appliedRank >= 2)
		{
			ResetFirstPassive();
			if (appliedRank >= 3)
			{
				ResetSecondPassive();
			}
		}
	}

	protected abstract void ApplyFirstPassive();

	protected abstract void ApplySecondPassive();

	protected abstract void ResetFirstPassive();

	protected abstract void ResetSecondPassive();
}
