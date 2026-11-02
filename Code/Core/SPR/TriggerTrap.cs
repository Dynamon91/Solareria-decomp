using UnityEngine;

namespace SPR;

public abstract class TriggerTrap : MonoBehaviour
{
	protected static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	public abstract void ActivateTrap();
}
