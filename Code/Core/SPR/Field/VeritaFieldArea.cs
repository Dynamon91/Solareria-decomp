using Library;
using UnityEngine;

namespace SPR.Field;

public class VeritaFieldArea : SubArea
{
	protected override void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(areaBox.Center, areaBox.Size.ToVector2());
		DrawPlayerDropPosition();
	}
}
