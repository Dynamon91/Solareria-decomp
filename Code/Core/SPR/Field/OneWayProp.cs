using UnityEngine;

namespace SPR.Field;

public abstract class OneWayProp : MonoBehaviour
{
	private SpriteRenderer sr;

	protected SpriteRenderer SpriteRenderer => sr ?? (sr = GetComponent<SpriteRenderer>());

	public abstract void Initialize(int slope);
}
