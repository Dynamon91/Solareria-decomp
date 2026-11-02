namespace SPR;

public class IsRested : Trigger
{
	private readonly FieldID fieldID;

	private readonly bool targetValue;

	public IsRested(string target, string value)
	{
		targetValue = value[0] != '!';
		string address;
		if (!targetValue)
		{
			address = value.Substring(1, value.Length - 1);
		}
		else
		{
			address = value;
		}
		fieldID = FieldID.ConvertToFieldID(address);
	}

	public override bool CheckTrigger()
	{
		return Trigger.SaveData.WorldData.HasInteractedBonfire(fieldID) == targetValue;
	}
}
