using Library;
using UnityEngine;

namespace SPR.Field;

public class FidielArea : SubArea
{
	[Header("Fidiel Area")]
	[SerializeField]
	private PolygonCollider2D phase1ConfineCollider;

	[SerializeField]
	private PolygonCollider2D phase2ConfineCollider;

	[Header("ThrowArea")]
	[SerializeField]
	private AreaBox phase1Area;

	[SerializeField]
	private AreaBox phase2Area;

	private Fidiel fidiel;

	public AreaBox Phase1Area => phase1Area;

	public AreaBox Phase2Area => phase2Area;

	private Fidiel Fidiel => fidiel ?? (fidiel = GetComponentInChildren<Fidiel>(includeInactive: true));

	public override PolygonCollider2D ConfineCollider
	{
		get
		{
			if (Fidiel.Phase != 1)
			{
				return phase2ConfineCollider;
			}
			return phase1ConfineCollider;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(phase1Area.Center, phase1Area.Size.ToVector3());
		Gizmos.DrawWireCube(phase2Area.Center, phase2Area.Size.ToVector3());
	}
}
