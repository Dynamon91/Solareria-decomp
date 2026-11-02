using UnityEngine;

namespace SPR;

public abstract class ImahenSection : MonoBehaviour
{
	protected bool IsStopped = true;

	public abstract void Initialize();

	public abstract void OnClear();
}
