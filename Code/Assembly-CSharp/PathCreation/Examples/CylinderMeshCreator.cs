using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathCreation.Examples;

public class CylinderMeshCreator : PathSceneTool
{
	public float thickness = 0.15f;

	[Range(3f, 30f)]
	public int resolutionU = 10;

	[Min(0f)]
	public float resolutionV = 20f;

	public Material material;

	[SerializeField]
	[HideInInspector]
	private GameObject meshHolder;

	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private Mesh mesh;

	protected override void PathUpdated()
	{
		if (pathCreator != null)
		{
			AssignMeshComponents();
			AssignMaterials();
			CreateMesh();
		}
	}

	private void CreateMesh()
	{
		List<Vector3> list = new List<Vector3>();
		List<int> list2 = new List<int>();
		int num = Mathf.Max(2, Mathf.RoundToInt(base.path.length * resolutionV) + 1);
		EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop;
		for (int i = 0; i < num; i++)
		{
			float t = (float)i / ((float)num - 1f);
			Vector3 pointAtTime = base.path.GetPointAtTime(t, endOfPathInstruction);
			Vector3 normal = base.path.GetNormal(t, endOfPathInstruction);
			Vector3 direction = base.path.GetDirection(t, endOfPathInstruction);
			Vector3 vector = Vector3.Cross(normal, direction);
			for (int j = 0; j < resolutionU; j++)
			{
				float f = (float)j / (float)resolutionU * (MathF.PI * 2f);
				float num2 = Mathf.Sin(f) * thickness;
				float num3 = Mathf.Cos(f) * thickness;
				Vector3 item = normal * num2 + vector * num3 + pointAtTime;
				list.Add(item);
				if (i < num - 1)
				{
					int num4 = resolutionU * i;
					list2.Add(num4 + j);
					list2.Add(num4 + (j + 1) % resolutionU);
					list2.Add(num4 + j + resolutionU);
					list2.Add(num4 + (j + 1) % resolutionU);
					list2.Add(num4 + (j + 1) % resolutionU + resolutionU);
					list2.Add(num4 + j + resolutionU);
				}
			}
		}
		if (mesh == null)
		{
			mesh = new Mesh();
		}
		else
		{
			mesh.Clear();
		}
		mesh.SetVertices(list);
		mesh.SetTriangles(list2, 0);
		mesh.RecalculateNormals();
	}

	private void AssignMeshComponents()
	{
		if (meshHolder == null)
		{
			meshHolder = new GameObject("Mesh Holder");
		}
		meshHolder.transform.rotation = Quaternion.identity;
		meshHolder.transform.position = Vector3.zero;
		meshHolder.transform.localScale = Vector3.one;
		if (!meshHolder.gameObject.GetComponent<MeshFilter>())
		{
			meshHolder.gameObject.AddComponent<MeshFilter>();
		}
		if (!meshHolder.GetComponent<MeshRenderer>())
		{
			meshHolder.gameObject.AddComponent<MeshRenderer>();
		}
		meshRenderer = meshHolder.GetComponent<MeshRenderer>();
		meshFilter = meshHolder.GetComponent<MeshFilter>();
		if (mesh == null)
		{
			mesh = new Mesh();
		}
		meshFilter.sharedMesh = mesh;
	}

	private void AssignMaterials()
	{
		if (material != null)
		{
			meshRenderer.sharedMaterial = material;
		}
	}
}
