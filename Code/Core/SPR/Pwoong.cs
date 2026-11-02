using Cysharp.Threading.Tasks;
using SPR.Save;

namespace SPR;

public class Pwoong : StoreNPC
{
	private bool isActive;

	private bool isStore;

	private static string Prefix
	{
		get
		{
			if (!Trigger.CheckTrigger("Trigger_PwoongShouldFear"))
			{
				return string.Empty;
			}
			return "fear/";
		}
	}

	private static string AppearAnimation => Prefix + "appear";

	private static string IdleAnimation => Prefix + "idle";

	private static string TalkAnimation => Prefix + "talk_a";

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		isActive = false;
		SetAnimation("appear_off_static");
	}

	public override UniTask OpenServiceAsync()
	{
		isStore = true;
		return base.OpenServiceAsync();
	}

	public override void OnServiceOpening()
	{
	}

	public override void OnServiceClosing()
	{
		isStore = false;
		Disable();
	}

	protected override void EnterTalkAnimation()
	{
		if (!isActive)
		{
			isActive = true;
			SetAnimation(AppearAnimation, loop: false);
			AddAnimation(TalkAnimation);
		}
		else
		{
			SetAnimation(TalkAnimation);
		}
	}

	protected override void OnFinishTalk()
	{
		base.OnFinishTalk();
		if (isStore)
		{
			SetAnimation(IdleAnimation);
		}
		else
		{
			Disable();
		}
	}

	protected override async UniTask WaitBeforeTalk()
	{
		await UniTask.WaitUntil(() => base.SkeletonAnimation.AnimationName == TalkAnimation);
	}

	private void Disable()
	{
		SetAnimation("appear_off", loop: false);
		AddAnimation("appear_off_static");
		isActive = false;
	}
}
