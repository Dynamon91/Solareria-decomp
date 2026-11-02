using Cinemachine;
using UnityEngine;

namespace SPR.Field;

public class AutoCameraZoom : MonoBehaviour
{
	[SerializeField]
	private float leftSize;

	[SerializeField]
	private float rightSize;

	[SerializeField]
	private Zone zoomZone;

	[Header("Force Size")]
	[SerializeField]
	private Zone forceSizeZone;

	[SerializeField]
	private float forceSize;

	private Area area;

	private CinemachineVirtualCamera targetCamera;

	private static PlayerMover Player => Game.Instance.Player.mover;

	private Area Area => GetComponent<Area>();

	public void SetRightSize(float value)
	{
		rightSize = value;
	}

	private void Update()
	{
		if (!(Game.Instance.FieldController.CurrentArea != Area))
		{
			if (zoomZone.Contains(Player.Position))
			{
				Area.Vcam.m_Lens.OrthographicSize = GetOthoSizeFromPosition();
			}
			if (forceSize > 0f && forceSizeZone.Contains(Player.Position))
			{
				Area.Vcam.m_Lens.OrthographicSize = forceSize;
			}
		}
	}

	private float GetOthoSizeFromPosition()
	{
		float num = Mathf.Abs(zoomZone.UpperRight.x - zoomZone.LowerLeft.x);
		float num2 = (Player.Position.x - zoomZone.LowerLeft.x) / num;
		float num3 = Mathf.Abs(leftSize - rightSize);
		return leftSize + num3 * num2 * (float)((leftSize < rightSize) ? 1 : (-1));
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0.5f, 0f);
		Gizmos.DrawWireCube(zoomZone.Center, zoomZone.Size);
		Gizmos.color = new Color(0f, 0.5f, 1f);
		Gizmos.DrawWireCube(forceSizeZone.Center, forceSizeZone.Size);
	}
}
