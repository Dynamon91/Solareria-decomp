using Library;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
	public Vector2 multiplier;

	private Transform _cameraTransform;

	private void LateUpdate()
	{
		CalculatePosition();
	}

	private void OnDrawGizmos()
	{
		CalculatePosition();
	}

	private void CalculatePosition()
	{
		_cameraTransform = (Camera.main.IsNull() ? null : Camera.main.transform);
		if (!_cameraTransform.IsNull())
		{
			Vector2 zero = Vector2.zero;
			Vector3 position = _cameraTransform.position;
			zero.x += multiplier.x * position.x;
			zero.y += multiplier.y * position.y;
			base.transform.position = zero;
		}
	}
}
