using Cysharp.Threading.Tasks;

namespace SPR;

public class Jima : StoreNPC
{
	private const string ByeScenarioID = "PF02_Jima_Bye";

	private const float AnimationDelay = 1.517f;

	protected override void EnterTalkAnimation()
	{
		SetAnimation("talk_a");
	}

	protected override void OnFinishTalk()
	{
		if (!ServicePanel.gameObject.activeInHierarchy)
		{
			base.OnFinishTalk();
			DisappearOnCondition();
		}
	}

	public override void OnServiceOpening()
	{
		SetAnimation("shop_open", loop: false);
		AddAnimation("shop_open_idle");
	}

	public override void OnServiceClosing()
	{
		SetAnimation("shop_close", loop: false);
		AddAnimation("idle");
		UniTask.WaitForSeconds(1.517f).GetAwaiter().OnCompleted(base.ResumeChasingSight);
		DisappearOnCondition();
	}

	private void DisappearOnCondition()
	{
		if (Trigger.CheckTrigger("Trigger_ShouldJimaDisappear") && base.gameObject.activeInHierarchy)
		{
			Npc.FieldCanvas.HideAllAsync(instant: false).Forget();
			UniTask task = DisappearByScenarioAsync();
			Npc.InputManager.BlockControlForTask(task, "Wait for Jima Disappear").Forget();
		}
	}

	private async UniTask DisappearByScenarioAsync()
	{
		await UniTask.WaitForSeconds(1.517f);
		Npc.TalkCanvas.StartTalk(ScenarioHelper.GetScenario("PF02_Jima_Bye"));
	}
}
