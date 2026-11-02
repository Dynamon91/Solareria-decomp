using Cinemachine;
using UnityEngine;

namespace SPR;

public class VcamZoomBlender : MonoBehaviour
{
	[SerializeField]
	private float leftSize;

	[SerializeField]
	private float rightSize;

	[SerializeField]
	private float leftPos;

	[SerializeField]
	private float rightPos;

	private CinemachineVirtualCamera vcam;

	private float v;

	private CinemachineVirtualCamera Vcam => vcam ?? (vcam = GetComponent<CinemachineVirtualCamera>());

	private void Update()
	{
		float t;
		float size = GetSize(out t);
		Vcam.m_Lens.OrthographicSize = ((t >= -1f && t <= 2f) ? Mathf.SmoothDamp(Vcam.m_Lens.OrthographicSize, size, ref v, 0.5f) : size);
	}

	private float GetSize(out float t)
	{
		float x = Game.Instance.Player.mover.VisualBottom.x;
		t = (x - leftPos) / (rightPos - leftPos);
		return Mathf.Lerp(leftSize, rightSize, t);
	}

	public void SetLensSize()
	{
		float t;
		float size = GetSize(out t);
		Vcam.m_Lens.OrthographicSize = size;
	}
}
