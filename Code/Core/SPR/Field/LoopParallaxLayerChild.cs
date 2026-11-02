using Library;
using UnityEngine;

namespace SPR.Field;

public class LoopParallaxLayerChild : MonoBehaviour
{
	private float endPosX;

	private float width;

	private ParallaxLayer parallaxLayer;

	public void Initialize(float endPosX, float width)
	{
		parallaxLayer = GetComponentInParent<ParallaxLayer>();
		this.endPosX = endPosX;
		this.width = width;
	}

	public void Move(float speed)
	{
		int sign = speed.GetSign();
		float num = speed * Time.fixedDeltaTime;
		float num2 = speed * Time.fixedDeltaTime * parallaxLayer.multiplier.x;
		Vector3 position = base.transform.position;
		position.x += num + num2;
		base.transform.position = position;
		if ((sign > 0 && position.x >= endPosX) || (sign < 0 && position.x <= endPosX))
		{
			num2 = (float)(-sign * 2) * width;
			position = base.transform.position;
			position.x += num2;
			base.transform.position = position;
		}
	}
}
