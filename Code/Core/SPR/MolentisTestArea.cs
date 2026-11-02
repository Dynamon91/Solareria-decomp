using SPR.Field;
using UnityEngine;

namespace SPR;

public class MolentisTestArea : Area
{
	[SerializeField]
	private Transform molentisStartTransform;

	[SerializeField]
	private Transform targetTransform;

	private Molentis molentis;

	private Molentis Molentis => molentis ?? (molentis = GetComponentInChildren<Molentis>());
}
