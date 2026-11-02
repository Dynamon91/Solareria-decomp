using System.Collections.Generic;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Pangolin : Creature
{
	[Header("공격 패턴")]
	[SerializeField]
	private GenericAttackSequence<Pangolin> attack;

	[Header("공격 범위(패링 체크용)")]
	[SerializeField]
	private Zone attackZone;

	[Header("튕겨남 상태 파라미터")]
	[Tooltip("침묵 시간")]
	[SerializeField]
	private int silenceTime;

	[Header("References")]
	[SerializeField]
	private MeshRenderer[] meshRenderers;

	[SerializeField]
	private MeshRenderer bodyRenderer;

	private FrameTimer timer;

	public override string Name => "Pangolin";

	public bool IsCoolTime
	{
		get
		{
			if (timer != null)
			{
				return !timer.IsDone;
			}
			return false;
		}
	}

	public override Bounds Bounds => HitBox.bounds;

	protected override MeshRenderer MeshRenderer => bodyRenderer;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(attack, () => new List<AttackSequence> { attack });
	}

	protected override void OnFixedUpdate()
	{
		if (IsCoolTime)
		{
			timer.Tick();
		}
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		if (!IsCoolTime)
		{
			return base.GetAttackSequences();
		}
		return null;
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		if (base.StateMachine.CurrentState is AttackState)
		{
			int sign = (moveForward ? FacingDirection.GetSignX() : (-FacingDirection.GetSignX()));
			PathFinder.Node node = PathFinder.FindNode(base.transform.position);
			PathFinder.Node node2 = node.Shift(sign, Mathf.RoundToInt(velocityX * deltaTime), Enemy.Player.VisualBottom);
			PathFinder.Node node3 = node.Shift(sign, base.DetectPathWidth / 2, allowNull: true);
			if (node2 == null || node3 == null || node2.Position == base.Position)
			{
				base.MoveX(velocityX, deltaTime, moveForward);
				return;
			}
			Vector2 position = node2.Position;
			position.x = Mathf.Clamp(position.x, base.MoveLeftEnd, base.MoveRightEnd);
			base.transform.position = Vector2.MoveTowards(base.transform.position, position, velocityX * deltaTime);
		}
		else
		{
			base.MoveX(velocityX, deltaTime, moveForward);
		}
	}

	public override void SetPropertyBlock(MaterialPropertyBlock block)
	{
		MeshRenderer[] array = meshRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetPropertyBlock(block);
		}
	}

	public override void OnEnterBounceState()
	{
		ActivateCoolTime();
	}

	public void ActivateCoolTime()
	{
		timer = new FrameTimer(silenceTime);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = attackZone.GetZoneOf(this);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}
}
