using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class TalkVcam : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private float blendDuration;

	private CinemachineVirtualCamera vcam;

	private CinemachineBrain cineBrain;

	private float defaultBlendDuration;

	public string PrefabKey => "Talk Vcam.prefab";

	public GameObject GameObject => base.gameObject;

	private CinemachineBrain CineBrain => cineBrain ?? (cineBrain = Camera.main?.GetComponent<CinemachineBrain>());

	public void InitializeBeforeUse()
	{
		if ((object)vcam == null)
		{
			vcam = GetComponent<CinemachineVirtualCamera>();
		}
	}

	public void Activate(Vector2 pos, float size = 12f)
	{
		float defaultBlendTime = CineBrain.m_DefaultBlend.m_Time;
		CineBrain.m_DefaultBlend.m_Time = blendDuration;
		base.transform.position = new Vector3(pos.x, pos.y, -10f);
		vcam.m_Lens.OrthographicSize = size;
		base.transform.position = Game.Instance.FieldController.CurrentArea.ClampCamera(vcam);
		base.gameObject.SetActive(value: true);
		UniTask.WaitForSeconds(blendDuration).GetAwaiter().OnCompleted(delegate
		{
			CineBrain.m_DefaultBlend.m_Time = defaultBlendTime;
		});
	}

	public void Deactivate()
	{
		if (!base.gameObject.IsNull())
		{
			float defaultBlendTime = CineBrain.m_DefaultBlend.m_Time;
			CineBrain.m_DefaultBlend.m_Time = blendDuration;
			base.gameObject.SetActive(value: false);
			UniTask.WaitForSeconds(blendDuration).GetAwaiter().OnCompleted(delegate
			{
				CineBrain.m_DefaultBlend.m_Time = defaultBlendTime;
			});
		}
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
