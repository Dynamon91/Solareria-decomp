using System;
using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Grakal : Creature
{
	[Header("패턴")]
	[SerializeField]
	private GenericAttackSequence<Grakal> charge;

	[SerializeField]
	private GenericAttackSequence<Grakal> chargeAttack;

	[SerializeField]
	private GenericAttackSequence<Grakal> attack1;

	[SerializeField]
	private GenericAttackSequence<Grakal> attack2;

	[Header("파라미터")]
	[SerializeField]
	private float chargeStartDistance;

	[SerializeField]
	private float playerDistance;

	[SerializeField]
	private float gravity;

	[Header("Reference")]
	[SerializeField]
	private ParticleSystem chargeSmokeParticle;

	private GrakalChargeAttackState chargeAttackState;

	public override string Name => "Grakal";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		chargeAttackState = new GrakalChargeAttackState(this, chargeAttack);
		AddAttackSequence(charge);
		AddAttackSequence(chargeAttack);
		AddAction(ActionTiming.OnFrame, "Charge", Charge);
		AddAction(ActionTiming.Begin, "StopChargeEffect", (Action)delegate
		{
			chargeSmokeParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		});
		AddAttack(charge, () => new List<AttackSequence> { charge });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { charge, chargeAttack, attack1, attack2 };
	}

	public override bool IsPlayerInSightBound()
	{
		if (!base.IsPlayerInSightBound())
		{
			AttackChance.Clear();
			AddAttack(charge, () => new List<AttackSequence> { charge });
			AddAttackSequence(charge);
			RemoveAttack(attack1);
			RemoveAttack(attack2);
			RemoveAttackSequence(attack1);
			RemoveAttackSequence(attack2);
		}
		return base.IsPlayerInSightBound();
	}

	public void OnGravity()
	{
		if (!Physics2D.Raycast(VisualBottom, Vector2.down, 1f, LayerUtility.PlatformLayer))
		{
			base.transform.position += Vector3.down * (gravity * Time.fixedDeltaTime);
			return;
		}
		Vector3 position = base.transform.position;
		base.transform.position = new Vector3(position.x, Mathf.FloorToInt(position.y));
	}

	private void Charge()
	{
		if (Vector2.Distance(VisualBottom, Enemy.Player.VisualBottom) <= playerDistance)
		{
			base.StateMachine.ChangeState(chargeAttackState);
			AttackChance.Clear();
			RemoveAttackSequence(charge);
			RemoveAttack(charge);
			AddAttack(attack1, () => new List<AttackSequence> { attack1 });
			AddAttack(attack2, () => new List<AttackSequence> { attack2 });
			AddAttackSequence(attack1);
			AddAttackSequence(attack2);
		}
		else if ((object)Physics2D.OverlapBox(VisualCenter + new Vector2(HitBox.size.x / 2f * (float)FacingDirection.GetSignX(), 0f), new Vector2((float)base.DetectPathWidth / 2f, 1f), 0f, LayerUtility.PlatformLayer) != null)
		{
			base.StateMachine.ChangeState(base.PatrolState);
			DisableVFXAll();
		}
	}
}
