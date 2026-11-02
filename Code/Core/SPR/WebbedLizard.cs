using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

public class WebbedLizard : Creature
{
	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<WebbedLizard> attack1;

	[SerializeField]
	private GenericAttackSequence<WebbedLizard> attack2;

	[Header("References")]
	[SerializeField]
	private Transform bubbleTransform;

	public override string Name => "WebbedLizard";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(attack1, () => new List<AttackSequence> { attack1 });
		AttackPatterns.TryAdd(attack2, () => new List<AttackSequence> { attack2 });
		SetAnimation("bubble_idle", loop: true, 1f, 1);
		BeginParamActions.Add("SpitBubble", delegate(int _, Parameter[] param)
		{
			SpitBubble(param[0].Float, param[1].Int);
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack1, attack2 };
	}

	protected override void OnDie()
	{
		base.OnDie();
		RemoveAnimation(1);
	}

	private void SpitBubble(float speed, int duration)
	{
		Enemy.PoolManager.GetObject<WebbedLizardBubble>("WebbedLizardBubble.prefab", base.transform.position + new Vector3((float)FacingDirection.GetSignX() * bubbleTransform.localPosition.x, bubbleTransform.localPosition.y)).Initialize(FacingDirection.ToVector2(), speed, duration);
	}
}
