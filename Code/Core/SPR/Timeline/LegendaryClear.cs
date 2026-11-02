using Cinemachine;
using Cysharp.Threading.Tasks;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class LegendaryClear : MonoBehaviour
{
	[SerializeField]
	private CinemachineVirtualCamera zoomVCam;

	[SerializeField]
	private CinemachineVirtualCamera endVCam;

	[SerializeField]
	private Transform[] effects;

	private PlayableDirector playableDirector;

	public bool IsPlaying => PlayableDirector.state == PlayState.Playing;

	private PlayableDirector PlayableDirector => playableDirector ?? (playableDirector = GetComponent<PlayableDirector>());

	public void PlayTimeline(Vector3 position)
	{
		Vector3 position2 = position;
		position2.y = GraphicHelper.MainCamera.transform.position.y - (GraphicHelper.MainCamera.orthographicSize - zoomVCam.m_Lens.OrthographicSize);
		base.transform.position = position2;
		Transform[] array = effects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].position = position + Vector3.down;
		}
		PlayableDirector.Play();
	}

	public void BlendEndVCam()
	{
		SynchronizeEndVcamAsync().Forget();
	}

	private async UniTaskVoid SynchronizeEndVcamAsync()
	{
		CinemachineBrain cineBrain = GraphicHelper.MainCamera.GetComponent<CinemachineBrain>();
		ICinemachineCamera activeVirtualCamera = cineBrain.ActiveVirtualCamera;
		if (activeVirtualCamera != null && activeVirtualCamera.IsValid)
		{
			endVCam.Priority = cineBrain.ActiveVirtualCamera.Priority + 1;
		}
		endVCam.gameObject.SetActive(value: true);
		CinemachineBlendDefinition.Style originStyle = cineBrain.m_DefaultBlend.m_Style;
		cineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
		await UniTask.Yield();
		cineBrain.m_DefaultBlend.m_Style = originStyle;
		endVCam.gameObject.SetActive(value: false);
	}
}
