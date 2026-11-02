using System.Collections.Generic;
using Library;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class HaperAvoidState : CreatureState
{
	private readonly Haper haper;

	private Vector2 avoidPosition;

	private Stack<PathFinder.Node> path = new Stack<PathFinder.Node>();

	public HaperAvoidState(Creature enemy)
		: base(enemy)
	{
		haper = enemy.CastObject<Haper>();
	}

	public override void Enter(IState prevState)
	{
		avoidPosition = haper.GetAvoidPosition();
		path = PathFinder.FindPath(base.Creature.VisualBottom, avoidPosition);
		haper.SetAnimation("walk");
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (path.Count == 0)
		{
			ChangeState(haper.HaperAmbushState);
			return;
		}
		base.Creature.LookAt(path.Peek().Position);
		base.Creature.Move(path.Peek().Position);
		if (Vector2.Distance(path.Peek().Position, base.Creature.VisualBottom) <= base.Creature.MoveForwardDelta)
		{
			path.Pop();
		}
	}
}
