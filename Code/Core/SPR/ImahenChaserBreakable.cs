using Library;
using SPR.Field;
using SPR.Save;
using Spine;
using UnityEngine;

namespace SPR;

public class ImahenChaserBreakable : BreakableObject, IStateSavable
{
	public enum State
	{
		None,
		Broken
	}

	[SerializeField]
	private string breakAnimationName = "break";

	[SerializeField]
	private ParticleSystem onBreakParticle;

	public override HitType HitType => HitType.Object;

	public string ID => Util.GetID(Game.FieldID, base.name);

	protected override string BreakAnimationName => breakAnimationName;

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!onBreakParticle.IsNull())
		{
			onBreakParticle.Play();
		}
		if (!(hitInfo.Source is PlayerMover))
		{
			return base.TryTakeDamage(hitInfo);
		}
		return false;
	}

	public void LoadState(int state)
	{
		switch ((State)state)
		{
		case State.None:
			base.SkeletonAnimation.SetAnimation("idle");
			break;
		case State.Broken:
		{
			TrackEntry trackEntry = base.SkeletonAnimation.SetAnimation(BreakAnimationName, 0, loop: false);
			trackEntry.AnimationStart = trackEntry.AnimationEnd;
			break;
		}
		}
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}
}
