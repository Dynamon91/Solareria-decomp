using System;
using Library;

namespace SPR;

public class DonationTrigger : Trigger
{
	private readonly int donatedOhn;

	public DonationTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("Donation Trigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		if (!value.TryParseInt(out donatedOhn))
		{
			throw new FormatException("Donation Trigger에서 Value는 정수여야 합니다. 현재 값 : " + value);
		}
	}

	public override bool CheckTrigger()
	{
		return Trigger.SaveData.WanderingData.TotalDonation >= donatedOhn;
	}
}
