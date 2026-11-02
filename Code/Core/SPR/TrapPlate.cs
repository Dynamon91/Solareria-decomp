using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class TrapPlate : MonoBehaviour
{
	[SerializeField]
	private List<TriggerTrap> trapList;

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.TryGetComponent<PlayerMover>(out var _))
		{
			trapList.ForEach(delegate(TriggerTrap t)
			{
				t.ActivateTrap();
			});
		}
	}
}
