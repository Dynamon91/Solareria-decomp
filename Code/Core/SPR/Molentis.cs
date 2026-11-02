using System.Collections.Generic;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Molentis : FlyingCreature
{
	[SerializeField]
	private GenericAttackSequence<Molentis> attack;

	private Vector2 direction;

	private Vector3 velocity;

	public override string Name => "Molentis";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAction(ActionTiming.Begin, "Move", delegate(Parameter[] p)
		{
			SetVelocity(p[0].Float);
		});
		AddAction(ActionTiming.OnFrame, "Move", delegate(Parameter[] p)
		{
			SweepMove(p[0].Float, p[1].Float);
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	public override void OnEncounterPlayer()
	{
		base.StateMachine.ChangeState(base.AttackState);
	}

	public void SetDirection(Transform targetTransform)
	{
		direction = ((Vector2)targetTransform.position - VisualCenter).normalized;
	}

	public void TestAttack()
	{
		base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { attack }, forceAttack: true));
	}

	private void SetVelocity(float speed)
	{
		direction = (Enemy.Player.GetPositionFromGroud() - VisualCenter).normalized;
		velocity = direction * speed;
	}

	private void SweepMove(float speed, float downSpeed)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, FacingDirection.ToVector2(), HitBox.size.x / 2f, LayerUtility.PlatformLayer);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(base.transform.position, Vector2.down, HitBox.size.y / 2f, LayerUtility.PlatformLayer);
		if ((bool)raycastHit2D)
		{
			velocity.x = 0f;
		}
		if ((bool)raycastHit2D2)
		{
			if (velocity.x != 0f)
			{
				velocity.x = speed * (float)FacingDirection.GetSignX();
			}
			velocity.y = 0f;
		}
		else
		{
			velocity.y -= downSpeed * Time.fixedDeltaTime;
		}
		base.transform.position += velocity * Time.fixedDeltaTime;
	}
}
