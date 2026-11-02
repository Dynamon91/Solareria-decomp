using Library;
using SPR.UI.Memory;
using Spine;

namespace SPR;

public class Muha : StoreNPC
{
	private bool knewArte;

	private static bool KnowArte => MenuMemoryPanel.IsMemoryKnown(ActorName.Arte, Npc.TalkData);

	protected override void StartTalk()
	{
		knewArte = KnowArte;
		base.StartTalk();
	}

	protected override void OnFinishTalk()
	{
		base.OnFinishTalk();
		if (!knewArte && KnowArte)
		{
			Npc.MemoryPanel.UpdateMemory(ActorName.Arte, isClear: false);
		}
	}

	protected override void EnterTalkAnimation()
	{
		SetAnimation("look_start", loop: false);
		AddAnimation("look_start_2", loop: false);
		AddAnimation("look_fly");
	}

	protected override void EnterIdleAnimation()
	{
		SetAnimation("look_end", loop: false);
		AddAnimation("idle_a");
	}

	public override void OnServiceOpening()
	{
		Npc.Player.mover.SetAnimation("cam_up");
		SetAnimation("look_fly");
	}

	public override void OnServiceClosing()
	{
		Npc.Player.mover.SetAnimation("idle");
		SetAnimation("look_end", loop: false);
		AddAnimation("idle_a");
	}

	protected override string GetSoldOutScenario(bool fromPanel)
	{
		if (!fromPanel)
		{
			return base.GetSoldOutScenario(fromPanel: false);
		}
		return "Muha_SoldOut_FromPanel";
	}

	protected override void AddInteractCount(FieldID fieldID)
	{
		if (fieldID.Equals(SectorName.WindshadeSpire, 23))
		{
			fieldID = new FieldID(SectorName.WindshadeSpire, 15);
		}
		base.AddInteractCount(fieldID);
	}

	private void OnIdleComplete(TrackEntry track)
	{
		string text = track.Animation.Name;
		if (text == "idle_a" || text == "idle_b")
		{
			SetAnimation(CryptoRandom.RandomBool() ? "idle_a" : "idle_b");
		}
	}
}
