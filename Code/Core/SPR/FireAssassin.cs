using System;
using System.Collections.Generic;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR;

public class FireAssassin : Creature
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<FireAssassin> claw;

	[SerializeField]
	private GenericAttackSequence<FireAssassin> swingCombo;

	public override string Name => "FireAssassin";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(claw, () => new List<AttackSequence> { claw });
		AddAttack(swingCombo, () => new List<AttackSequence> { swingCombo });
		AddAction(ActionTiming.Begin, "Teleport", (Action<int, Parameter[]>)delegate
		{
			base.SkeletonRenderer.skeleton.SetAlpha(0f);
		});
		AddAction(ActionTiming.End, "Teleport", delegate(Parameter[] p)
		{
			Teleport(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "Drop", Drop);
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { claw, swingCombo };
	}

	private void Teleport(float height)
	{
		base.SkeletonRenderer.skeleton.SetAlpha(1f);
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x, Enemy.Player.GetPositionFromGroud().y + height);
	}

	private void Drop(int duration)
	{
		base.transform.DOMoveY(Enemy.Player.GetPositionFromGroud().y, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}
}
