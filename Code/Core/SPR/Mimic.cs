using System.Collections.Generic;
using FMODUnity;
using Library;
using SPR.FSM;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Mimic : Creature
{
	[SerializeField]
	private EventReference appearEvent;

	[Header("히든 여부 설정")]
	[SerializeField]
	private bool hidden;

	[Header("패턴")]
	[SerializeField]
	private GenericAttackSequence<Mimic> attack;

	[SerializeField]
	private GenericAttackSequence<Mimic> grab;

	[Header("히든")]
	[SerializeField]
	private int defaultDefense;

	[SerializeField]
	private int hiddenDefense;

	[SerializeField]
	private int waitFrame;

	[Header("끌어당기기")]
	[SerializeField]
	private Zone grabZone;

	[SerializeField]
	private float pullPower;

	[SerializeField]
	private int pullDuration;

	private MimicHideState hideState;

	private MimicPullState pullState;

	public AttackSequence Attack => attack;

	public override string Name => "Mimic";

	public EventReference AppearEvent => appearEvent;

	protected override int OriginDef
	{
		get
		{
			if (base.StateMachine.CurrentState != hideState)
			{
				return defaultDefense;
			}
			return hiddenDefense;
		}
	}

	public override EnemyState DefaultState
	{
		get
		{
			if (!hidden)
			{
				return base.PatrolState;
			}
			return hideState;
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		pullState = new MimicPullState(this, pullDuration);
		hideState = new MimicHideState(this, waitFrame);
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(attack, () => new List<AttackSequence> { attack });
		AddAttack(grab, () => new List<AttackSequence> { grab });
		AddAction(ActionTiming.Begin, "TryGrabPlayer", TryGrabPlayer);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack, grab };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		int index = 0;
		if (base.StateMachine.PreviousState == hideState)
		{
			return new List<AttackSequence> { base.AllAttackSequences[0] };
		}
		if (IsPlayerInAttackBound(attack))
		{
			index = CryptoRandom.Range(0, 2);
		}
		else if (IsPlayerInAttackBound(grab))
		{
			index = 1;
		}
		return new List<AttackSequence> { base.AllAttackSequences[index] };
	}

	public void PullPlayer()
	{
		Vector3 vector = new Vector3((float)(-FacingDirection.GetSignX()) * pullPower, 0f);
		Enemy.Player.ApplyRelativeVelocity(vector, faceJump: true);
	}

	public bool IsPlayerInSwingBounds()
	{
		return IsPlayerInAttackBound(attack);
	}

	private void TryGrabPlayer()
	{
		Zone zoneOf = grabZone.GetZoneOf(this);
		Collider2D collider2D = Physics2D.OverlapBox(zoneOf.Center, zoneOf.Size, 0f, LayerUtility.PlayerLayer);
		if (!collider2D.IsNull() && collider2D.TryGetComponent<PlayerMover>(out var _))
		{
			HitInfo hitInfo = new HitInfo(this, "Grab");
			Enemy.Player.TryTakeDamage(hitInfo);
			if (!Enemy.Player.CanParry(hitInfo) && Enemy.Player.CanHit)
			{
				base.StateMachine.ChangeState(pullState);
			}
		}
	}
}
