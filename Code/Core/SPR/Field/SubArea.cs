using Library;
using UnityEngine;

namespace SPR.Field;

public class SubArea : Area
{
	[Header("Sub Area")]
	[SerializeField]
	private bool confineLookAround = true;

	public override bool ConfineLookAround => confineLookAround;

	protected override void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(areaBox.Center, areaBox.Size.ToVector2());
		DrawPlayerDropPosition();
	}
}
