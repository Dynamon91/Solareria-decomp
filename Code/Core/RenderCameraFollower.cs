using Library;
using UnityEngine;

public class RenderCameraFollower : MonoBehaviour
{
	private Camera main;

	private Camera cameraComponent;

	private void Awake()
	{
		main = Camera.main;
		cameraComponent = GetComponent<Camera>();
	}

	private void LateUpdate()
	{
		base.transform.SynchronizeValues(main.transform);
		cameraComponent.orthographicSize = main.orthographicSize;
	}
}
