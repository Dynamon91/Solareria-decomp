using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR;

public class EnemyWithSightPlaceholder : EnemyPlaceHolder
{
	[SerializeField]
	private Vector2 visualCenter;

	[SerializeField]
	private Zone patrolSightArea;

	public override async UniTask<Enemy> InstantiateCharacterAsync(int sortingOrder)
	{
		Enemy obj = await base.InstantiateCharacterAsync(sortingOrder);
		if (obj is Creature creature)
		{
			creature.SetPatrolSight(patrolSightArea);
		}
		return obj;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = patrolSightArea.GetZoneOf(base.transform);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(zoneOf.Center + visualCenter, zoneOf.Size);
	}
}
