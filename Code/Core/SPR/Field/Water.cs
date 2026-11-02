using Game2DWaterKit;
using UnityEngine;

namespace SPR.Field;

public class Water : DamageZone
{
	private Game2DWater waterRenderer;

	private Game2DWater WaterRenderer => waterRenderer ?? (waterRenderer = GetComponent<Game2DWater>());

	public override string Name => "Water";

	public float GetSurfaceYPosition()
	{
		float num = (WaterRenderer.IsInitialized ? WaterRenderer.MainModule.Height : WaterRenderer.GetComponent<BoxCollider2D>().size.y);
		return base.transform.position.y + num / 2f;
	}
}
