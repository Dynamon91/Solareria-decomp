using Cysharp.Threading.Tasks;
using SPR.UI.Memory;
using SPR.UI.Upgrade;
using UnityEngine;

namespace SPR;

public abstract class ServiceNPC<TArg> : Npc, IService
{
	[Header("Service NPC")]
	[SerializeField]
	protected GameObject serviceVcam;

	[SerializeField]
	protected TArg panelArg;

	public ActorName OwnerName => npcName;

	public GameObject ServiceGameObject => base.gameObject;

	protected abstract ServicePanel<TArg> ServicePanel { get; }

	public virtual async UniTask OpenServiceAsync()
	{
		if ((object)serviceVcam == null)
		{
			DebugLog.GeneralLogError($"{npcName}에게 Service용 Vcam이 할당되지 않았습니다!");
		}
		ServicePanel.Initialize(this, serviceVcam, panelArg);
		await Npc.InputManager.StackControlTarget(ServicePanel);
	}

	public void PlaySoldOutScenario(bool fromPanel = false)
	{
		bool flag = MenuMemoryPanel.IsMemoryCleared(npcName);
		Scenario scenario = ScenarioHelper.GetScenario(GetSoldOutScenario(fromPanel));
		if (Npc.InputManager.ContainsInControlStack(Npc.TalkCanvas))
		{
			Npc.TalkCanvas.StackScenario(scenario);
		}
		else
		{
			ActivateTalkVcam();
			EnterTalkAnimation();
			Npc.TalkCanvas.StartTalk(scenario, OnFinishSoldOutTalk);
		}
		Npc.MemoryPanel.TryUpdateClearedMemory(flag, npcName);
	}

	protected virtual string GetSoldOutScenario(bool fromPanel)
	{
		return Name + "_SoldOut";
	}

	protected virtual void OnFinishSoldOutTalk()
	{
		DisableTalkVcam();
		base.OnFinishTalk();
	}

	public abstract void OnServiceOpening();

	public abstract void OnServiceClosing();
}
