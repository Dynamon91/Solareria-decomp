using System.Collections.Generic;
using System.Linq;
using PathCreation;
using UnityEngine;

namespace SPR;

public class PathFlipper : MonoBehaviour
{
	private PathCreator pathCreator;

	private List<Vector3> originPoints;

	private PathCreator PathCreator => pathCreator ?? (pathCreator = GetComponent<PathCreator>());

	private void Awake()
	{
		pathCreator = GetComponent<PathCreator>();
		originPoints = new List<Vector3>();
		for (int i = 0; i < PathCreator.bezierPath.NumPoints; i++)
		{
			originPoints.Add(PathCreator.bezierPath.GetPoint(i));
		}
		PathCreator.InitializeEditorData(in2DMode: true);
	}

	public void FlipPathTo(bool right)
	{
		base.transform.localScale = new Vector3(right ? 1 : (-1), 1f, 1f);
		List<Vector3> list = originPoints;
		if (!right)
		{
			list = originPoints.Select((Vector3 point) => new Vector3(0f - point.x, point.y, point.z)).ToList();
		}
		for (int num = 0; num < list.Count; num++)
		{
			PathCreator.bezierPath.SetPoint(num, list[num]);
		}
	}

	public void FlipPath()
	{
		List<Vector3> list = (from point in pathCreator.bezierPath.GetPointsInSegment(0)
			select new Vector3(0f - point.x, point.y, point.z)).ToList();
		BezierPath bezierPath = new BezierPath(Vector3.zero, isClosed: false, PathSpace.xy);
		for (int num = 0; num < list.Count; num++)
		{
			bezierPath.SetPoint(num, list[num]);
		}
		new VertexPath(bezierPath, base.transform);
		pathCreator.bezierPath = bezierPath;
	}
}
