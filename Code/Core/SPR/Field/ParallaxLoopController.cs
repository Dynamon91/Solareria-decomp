using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR.Field;

public class ParallaxLoopController : MonoBehaviour
{
	[SerializeField]
	private bool test;

	[SerializeField]
	private float speed;

	[SerializeField]
	private List<ParallaxLayer> parallaxLayers;

	[SerializeField]
	private SpriteRenderer widthSpriteRenderer;

	[SerializeField]
	private float width;

	[SerializeField]
	private Vector2 center;

	private List<LoopParallaxLayerChild> parallaxLayerChildren;

	private void OnValidate()
	{
		if (parallaxLayers.IsNullOrEmpty())
		{
			return;
		}
		foreach (ParallaxLayer parallaxLayer in parallaxLayers)
		{
			int childCount = parallaxLayer.transform.childCount;
			if (childCount == 0)
			{
				continue;
			}
			for (int i = 0; i < childCount; i++)
			{
				Transform child = parallaxLayer.transform.GetChild(i);
				if (!child.TryGetComponent<LoopParallaxLayerChild>(out var _))
				{
					child.gameObject.AddComponent<LoopParallaxLayerChild>();
				}
			}
		}
		center = widthSpriteRenderer.transform.position;
		width = widthSpriteRenderer.bounds.size.x;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(center, new Vector3(width, 1f));
	}

	private void Start()
	{
		if (test)
		{
			Initialize();
		}
	}

	private void FixedUpdate()
	{
		if (test)
		{
			Move(speed);
		}
	}

	public void Initialize()
	{
		OnValidate();
		int sign = speed.GetSign();
		float endPosX = center.x + (float)sign * width;
		parallaxLayerChildren = new List<LoopParallaxLayerChild>();
		foreach (ParallaxLayer parallaxLayer2 in parallaxLayers)
		{
			int childCount = parallaxLayer2.transform.childCount;
			if (childCount == 0)
			{
				continue;
			}
			ParallaxLayer parallaxLayer = Object.Instantiate(parallaxLayer2, parallaxLayer2.transform.parent);
			for (int i = 0; i < childCount; i++)
			{
				LoopParallaxLayerChild component = parallaxLayer2.transform.GetChild(i).GetComponent<LoopParallaxLayerChild>();
				if (!component.IsNull())
				{
					component.Initialize(endPosX, width);
					parallaxLayerChildren.Add(component);
				}
			}
			for (int j = 0; j < childCount; j++)
			{
				Transform child = parallaxLayer.transform.GetChild(j);
				Vector3 position = child.position;
				position.x += (float)(-sign) * width;
				child.position = position;
				LoopParallaxLayerChild component2 = child.GetComponent<LoopParallaxLayerChild>();
				if (!component2.IsNull())
				{
					component2.Initialize(endPosX, width);
					parallaxLayerChildren.Add(component2);
				}
			}
		}
	}

	public void Move(float speed)
	{
		parallaxLayerChildren.ForEach(delegate(LoopParallaxLayerChild c)
		{
			c.Move(speed);
		});
	}
}
