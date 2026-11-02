using Library;

namespace SPR;

public class SameAnimation : Trigger
{
	private readonly ActorName npcName;

	private readonly string animation;

	public SameAnimation(string target, string value)
	{
		npcName = target.ToEnum<ActorName>();
		animation = value;
	}

	public override bool CheckTrigger()
	{
		return Game.Instance.FieldController.FindNpc(npcName.ToString()).GetCurrentAnim(0).Name == animation;
	}
}
