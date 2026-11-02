using Dreamteck.Splines;
using UnityEngine;

namespace SPR;

public class TrailEffect : MonoBehaviour
{
	private SplineFollower follower;

	private void Awake()
	{
		follower = GetComponent<SplineFollower>();
	}

	private void OnEnable()
	{
		follower.Restart();
	}
}
