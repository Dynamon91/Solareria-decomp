using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR;

public class GroggyTimelineEffect : TimelineEffect
{
	[SerializeField]
	private Enemy enemy;

	[SerializeField]
	private Transform enemyCenter;

	[SerializeField]
	private CinemachineVirtualCamera groggyVcam;

	private bool isBound;

	private static PlayerMover Player => Game.Instance.Player.mover;

	public override void Initialize()
	{
		base.Initialize();
		BindTimelineAsset();
	}

	public override void Play(bool loop = false, float time = 0f)
	{
		groggyVcam.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = Game.Instance.FieldController.CurrentArea.ConfineCollider;
		BindTimelineAsset();
		Vector2 vector = ((Vector2)enemyCenter.position + Player.VisualCenter) / 2f;
		Transform obj = groggyVcam.transform;
		obj.position = new Vector3(z: obj.position.z, x: vector.x, y: vector.y);
		base.Play(loop, time);
	}

	public void ReBindVCam(CinemachineVirtualCamera vcam)
	{
		List<TimelineClip> list = (((TimelineAsset)base.Director.playableAsset).GetOutputTracks().First((TrackAsset asset) => asset.GetType() == typeof(CinemachineTrack)) as CinemachineTrack).GetClips().ToList();
		ExposedReference<CinemachineVirtualCameraBase> virtualCamera = new ExposedReference<CinemachineVirtualCameraBase>
		{
			defaultValue = vcam
		};
		((CinemachineShot)list[0].asset).VirtualCamera = virtualCamera;
		((CinemachineShot)list[list.Count - 1].asset).VirtualCamera = virtualCamera;
	}

	private void BindTimelineAsset()
	{
		if (!isBound)
		{
			CinemachineTrack cinemachineTrack = ((TimelineAsset)base.Director.playableAsset).GetOutputTracks().First((TrackAsset asset) => asset.GetType() == typeof(CinemachineTrack)) as CinemachineTrack;
			CinemachineBrain component = Camera.main.GetComponent<CinemachineBrain>();
			base.Director.SetGenericBinding(cinemachineTrack, component);
			List<TimelineClip> list = cinemachineTrack.GetClips().ToList();
			ExposedReference<CinemachineVirtualCameraBase> virtualCamera = new ExposedReference<CinemachineVirtualCameraBase>
			{
				defaultValue = enemy.MyArea.Vcam
			};
			((CinemachineShot)list[0].asset).VirtualCamera = virtualCamera;
			((CinemachineShot)list[list.Count - 1].asset).VirtualCamera = virtualCamera;
			isBound = true;
		}
	}
}
