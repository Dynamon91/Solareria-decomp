using UnityEngine;

namespace SPR;

public class TrailClearer : MonoBehaviour
{
	private TrailRenderer trail;

	private void Awake()
	{
		trail = GetComponent<TrailRenderer>();
	}

	private void OnDisable()
	{
		trail.Clear();
	}
}
