using System.Collections.Generic;
using System.Linq;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class SpineEnvironment : MonoBehaviour
{
	[SerializeField]
	private bool disableCulling;

	private SpriteRenderer spriteRenderer;

	private MeshRenderer meshRenderer;

	private List<SkeletonAnimation> skeletonAnimations;

	protected bool UseCulling = true;

	private readonly Vector2[] vertices = new Vector2[4];

	private Camera mainCamera;

	private bool isFirstFrame = true;

	protected SpriteRenderer SpriteRenderer => spriteRenderer ?? (spriteRenderer = GetComponent<SpriteRenderer>());

	protected MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = GetComponentInChildren<SkeletonAnimation>().GetComponent<MeshRenderer>());

	protected List<SkeletonAnimation> SkeletonAnimations => skeletonAnimations ?? (skeletonAnimations = GetComponentsInChildren<SkeletonAnimation>().ToList());

	private Camera MainCamera
	{
		get
		{
			if (mainCamera.IsNull())
			{
				mainCamera = Camera.main;
			}
			return mainCamera;
		}
	}

	protected virtual void Awake()
	{
		if (SkeletonAnimations.Count != 0)
		{
			MeshRenderer.sortingOrder = SpriteRenderer.sortingOrder;
		}
	}

	private void Update()
	{
		if (!disableCulling)
		{
			if (isFirstFrame)
			{
				isFirstFrame = false;
			}
			else
			{
				CullingSpine();
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
	}

	private void CullingSpine()
	{
		if (!UseCulling)
		{
			return;
		}
		Camera camera = MainCamera;
		if ((object)camera == null)
		{
			return;
		}
		float num = (float)Screen.width / (float)Screen.height;
		float num2 = camera.orthographicSize * 2f;
		float x = num2 * num;
		Vector3 center = new Vector3(camera.transform.position.x, camera.transform.position.y);
		Bounds bounds = new Bounds(center, new Vector3(x, num2));
		foreach (SkeletonAnimation skeletonAnimation in SkeletonAnimations)
		{
			MeshRenderer component = skeletonAnimation.GetComponent<MeshRenderer>();
			skeletonAnimation.enabled = bounds.Intersects(component.bounds);
		}
	}
}
