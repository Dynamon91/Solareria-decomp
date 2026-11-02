using DG.Tweening;
using UnityEngine;

namespace SPR.Field;

public class EarthPortBridge : MonoBehaviour
{
	public void LoadMovedState(float posY)
	{
		Vector3 position = base.transform.position;
		position.y = posY;
		base.transform.position = position;
	}

	public void MoveBridge(float endPosY, float duration)
	{
		base.transform.DOMoveY(endPosY, duration);
	}
}
