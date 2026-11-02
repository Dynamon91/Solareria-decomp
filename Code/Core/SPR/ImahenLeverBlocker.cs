using System;
using FMODUnity;
using Spine;
using UnityEngine;

namespace SPR;

public class ImahenLeverBlocker : ImahenBlockerBase, IDamageable
{
	[SerializeField]
	private BoxCollider2D box2D;

	[SerializeField]
	private EventReference stuckEvent;

	private State currentState;

	public override string Name => "";

	public HitType HitType => HitType.Object;

	private void Awake()
	{
		SetAnimation("close_idle");
		AddAnimationEvent(AnimState.Start, delegate(TrackEntry track)
		{
			if (track.Animation.Name == "open")
			{
				box2D.enabled = false;
			}
		});
	}

	public override void OnTriggered()
	{
		SetAnimation("open", loop: false);
		ChangeState(1);
		currentState = State.Opened;
	}

	public override void LoadState(int state)
	{
		currentState = (State)state;
		if (Game.Instance.CurrentFieldID.ToString() == "SolinLab_90")
		{
			state = 0;
		}
		switch ((State)state)
		{
		case State.Closed:
			SetAnimation("close_idle");
			break;
		case State.Opened:
			SetAnimation("open_idle");
			box2D.enabled = false;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (currentState == State.Opened)
		{
			return false;
		}
		PlayerMover mover = Game.Instance.Player.mover;
		mover.PSM.ChangeState(mover.PSM.AttackStuckState);
		Game.Instance.SoundManager.PlayOneShot(stuckEvent);
		return false;
	}
}
