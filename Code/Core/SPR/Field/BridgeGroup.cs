using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using UnityEngine;

namespace SPR.Field;

public class BridgeGroup : MonoBehaviour
{
	[SerializeField]
	private AreaBox areaBox;

	[SerializeField]
	private CinemachineVirtualCamera vcam;

	[SerializeField]
	private float duration;

	[SerializeField]
	private EventReference bridgeEvent;

	private CinemachineBrain cineBrain;

	private PolygonCollider2D polygonColl;

	private readonly List<EarthPortBridge> bridges = new List<EarthPortBridge>();

	private float endPosY;

	private CinemachineBrain CineBrain => cineBrain ?? (cineBrain = Camera.main.GetComponent<CinemachineBrain>());

	private PolygonCollider2D PolygonCollider => polygonColl ?? (polygonColl = GetComponent<PolygonCollider2D>());

	private void Awake()
	{
		bridges.Clear();
		bridges.AddRange(base.transform.parent.GetComponentsInChildren<EarthPortBridge>());
	}

	public void Initialize(bool isMoved, float endPosY)
	{
		this.endPosY = endPosY;
		if (isMoved)
		{
			bridges.ForEach(delegate(EarthPortBridge bridge)
			{
				bridge.LoadMovedState(endPosY);
			});
		}
	}

	public async UniTask ActivateAsync()
	{
		float originBlendTime = CineBrain.m_DefaultBlend.BlendTime;
		CineBrain.m_DefaultBlend.m_Time = 2f;
		ICinemachineCamera originVcam = CineBrain.ActiveVirtualCamera;
		vcam.Priority = cineBrain.ActiveVirtualCamera.Priority + 1;
		vcam.gameObject.SetActive(value: true);
		await UniTask.WaitUntil(() => !CineBrain.IsBlending && CineBrain.ActiveVirtualCamera == vcam);
		await UniTaskExtension.WaitForSeconds(0.6f);
		Game.Instance.SoundManager.PlayOneShot(bridgeEvent);
		await UniTaskExtension.WaitForSeconds(0.15f);
		bridges.ForEach(delegate(EarthPortBridge bridge)
		{
			bridge.MoveBridge(endPosY, duration);
		});
		await UniTaskExtension.WaitForSeconds(duration);
		await UniTaskExtension.WaitForSeconds(0.6f);
		CineBrain.m_DefaultBlend.m_Time = 1f;
		vcam.gameObject.SetActive(value: false);
		await UniTask.WaitUntil(() => !CineBrain.IsBlending && CineBrain.ActiveVirtualCamera == originVcam);
		CineBrain.m_DefaultBlend.m_Time = originBlendTime;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(base.transform.position + (Vector3)areaBox.Center, areaBox.Size.ToVector3());
	}
}
