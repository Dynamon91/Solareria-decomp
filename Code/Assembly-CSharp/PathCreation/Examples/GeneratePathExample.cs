using UnityEngine;

namespace PathCreation.Examples;

[RequireComponent(typeof(PathCreator))]
public class GeneratePathExample : MonoBehaviour
{
	public bool closedLoop = true;

	public Transform[] waypoints;

	private void Start()
	{
		if (waypoints.Length != 0)
		{
			BezierPath bezierPath = new BezierPath(waypoints, closedLoop, PathSpace.xyz);
			GetComponent<PathCreator>().bezierPath = bezierPath;
		}
	}
}
