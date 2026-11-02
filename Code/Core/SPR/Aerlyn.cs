using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class Aerlyn : FlyingCreature
{
	[SerializeField]
	private float avoidSpeed;

	private AerlynAvoidState avoidState;

	private AerialPathNode currentNode;

	public override string Name => "Aerlyn";

	public override string MoveAnimation => "fly";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		avoidState = new AerlynAvoidState(this);
		currentNode = Game.Instance.FieldController.AerialPath.Root;
	}

	public override void OnEncounterPlayer()
	{
		base.StateMachine.ChangeState(avoidState);
	}

	public void Move(float deltaTime)
	{
		if (base.transform.position == currentNode.Position)
		{
			if (currentNode.IsEnd)
			{
				base.gameObject.SetActive(value: false);
				return;
			}
			currentNode = currentNode.GetNextNode();
			if (currentNode.IsEnd)
			{
				Game.Instance.RuntimeData.AddDieCreature(Game.Instance.CurrentFieldID, base.PlaceholderPosition);
				return;
			}
		}
		LookAt(currentNode.Position);
		base.transform.position = Vector3.MoveTowards(base.transform.position, currentNode.Position, avoidSpeed * deltaTime);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}
}
