namespace SPR;

public class NeverTrigger : Trigger
{
	public override bool CheckTrigger()
	{
		return false;
	}
}
