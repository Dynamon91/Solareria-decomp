using System;
using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class MovingPlatformController : MonoBehaviour, IStateSavable
{
	[Serializable]
	private class SolinPipeController
	{
		[SerializeField]
		private DictionaryListValueWrapper<int, SolinPipe> indexToPipes;

		public void LoadState(int index)
		{
			foreach (DictionaryWrapper<int, List<SolinPipe>>.KeyValueSet keyValuePair in indexToPipes.KeyValuePairs)
			{
				if (keyValuePair.Key != index)
				{
					keyValuePair.Value.ForEach(delegate(SolinPipe pipe)
					{
						pipe.SetDefault(isOpen: true);
					});
				}
			}
			indexToPipes[index].ForEach(delegate(SolinPipe pipe)
			{
				pipe.SetDefault(isOpen: false);
			});
		}

		public void OpenPipe(int index)
		{
			indexToPipes[index].ForEach(delegate(SolinPipe pipe)
			{
				pipe.Move(isOpen: true);
			});
		}

		public void ClosePipe(int index)
		{
			indexToPipes[index].ForEach(delegate(SolinPipe pipe)
			{
				pipe.Move(isOpen: false);
			});
		}
	}

	[SerializeField]
	private GameObject environment;

	[SerializeField]
	private float speed;

	[SerializeField]
	private List<Vector2> positions;

	[SerializeField]
	private SolinPipeController pipeController;

	private List<MovingPlatform> movingPlatforms;

	private List<(ParallaxLayer parallaxLayer, List<Transform> childTransforms)> parallaxChildTransforms;

	private CinemachineImpulseSource impulseSource;

	private CinemachineBrain cineBrain;

	private MovingPlatformSoundPlayer soundPlayer;

	private Vector2 originPosition;

	private int currentState;

	public string ID => "MovingPlatform_" + Game.Instance.CurrentFieldID.ToString();

	private CinemachineImpulseSource ImpulseSource => impulseSource ?? (impulseSource = GetComponent<CinemachineImpulseSource>());

	private CinemachineBrain CineBrain => cineBrain ?? (cineBrain = Camera.main.GetComponent<CinemachineBrain>());

	private MovingPlatformSoundPlayer SoundPlayer => soundPlayer ?? (soundPlayer = GetComponent<MovingPlatformSoundPlayer>());

	private void Awake()
	{
		originPosition = base.transform.position;
	}

	public void LoadState(int state)
	{
		currentState = state;
		base.transform.position = GetTargetPosition(state);
		movingPlatforms = new List<MovingPlatform>(base.transform.childCount - 1);
		for (int i = 0; i < base.transform.childCount - 1; i++)
		{
			movingPlatforms.Add(base.transform.GetChild(i).GetComponent<MovingPlatform>());
		}
		movingPlatforms.ForEach(delegate(MovingPlatform p)
		{
			p.ModifyPath(setGround: true);
		});
		parallaxChildTransforms = new List<(ParallaxLayer, List<Transform>)>();
		ParallaxLayer[] componentsInChildren = environment.GetComponentsInChildren<ParallaxLayer>();
		foreach (ParallaxLayer parallaxLayer in componentsInChildren)
		{
			int childCount = parallaxLayer.transform.childCount;
			if (childCount != 0)
			{
				List<Transform> list = new List<Transform>(childCount);
				for (int num2 = 0; num2 < childCount; num2++)
				{
					list.Add(parallaxLayer.transform.GetChild(num2));
				}
				parallaxChildTransforms.Add((parallaxLayer, list));
			}
		}
		pipeController.LoadState(state);
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
			currentState = state;
		}
	}

	public async UniTask MoveAsync(CinemachineVirtualCamera interactVcam, int index)
	{
		ICinemachineCamera originVcam = CineBrain.ActiveVirtualCamera;
		CinemachineVirtualCamera virtualCamera = originVcam.CastObject<CinemachineVirtualCamera>();
		float originSize = virtualCamera.m_Lens.OrthographicSize;
		if (!(interactVcam != null))
		{
			await DOTween.To(() => virtualCamera.m_Lens.OrthographicSize, delegate(float x)
			{
				virtualCamera.m_Lens.OrthographicSize = x;
			}, 24f, 1f).WaitAsync();
		}
		else
		{
			interactVcam.gameObject.SetActive(value: true);
			await UniTask.WaitUntil(() => !CineBrain.IsBlending && CineBrain.ActiveVirtualCamera == interactVcam);
		}
		bool hasFollow = !CineBrain.ActiveVirtualCamera.Follow.IsNull();
		CinemachineFramingTransposer framingTransposer = CineBrain.ActiveVirtualCamera.CastObject<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineFramingTransposer>();
		float originScreenY = framingTransposer.m_ScreenY;
		float originPosY = CineBrain.ActiveVirtualCamera.VirtualCameraGameObject.transform.position.y;
		pipeController.OpenPipe(currentState);
		SoundPlayer.PlayDisconnect();
		ImpulseSource.GenerateImpulse();
		await UniTask.WaitForSeconds(1f);
		movingPlatforms.ForEach(delegate(MovingPlatform p)
		{
			p.ModifyPath(setGround: false);
		});
		Vector2 targetPosition = GetTargetPosition(index);
		float num = targetPosition.y - base.transform.position.y;
		foreach (var parallaxChildTransform in parallaxChildTransforms)
		{
			float y = parallaxChildTransform.parallaxLayer.multiplier.y;
			foreach (Transform item in parallaxChildTransform.childTransforms)
			{
				float endValue = item.position.y + num * y;
				item.DOMoveY(endValue, speed * y).SetSpeedBased().SetEase(Ease.InOutSine);
			}
		}
		float moveDuration = 1f;
		if (hasFollow)
		{
			float endValue2 = originScreenY - (float)num.GetSign() * 0.0625f;
			DOTween.To(() => framingTransposer.m_ScreenY, delegate(float x)
			{
				framingTransposer.m_ScreenY = x;
			}, endValue2, moveDuration).SetEase(Ease.InOutSine);
		}
		else
		{
			float endValue3 = originPosY + (float)num.GetSign() * 5f;
			CineBrain.ActiveVirtualCamera.VirtualCameraGameObject.transform.DOMoveY(endValue3, moveDuration).SetEase(Ease.InOutSine);
		}
		UniTask.WaitForSeconds(Mathf.Abs(num) / speed - moveDuration).GetAwaiter().OnCompleted(delegate
		{
			if (hasFollow)
			{
				DOTween.To(() => framingTransposer.m_ScreenY, delegate(float x)
				{
					framingTransposer.m_ScreenY = x;
				}, originScreenY, moveDuration);
			}
			else
			{
				CineBrain.ActiveVirtualCamera.VirtualCameraGameObject.transform.DOMoveY(originPosY, moveDuration).SetEase(Ease.InOutSine);
			}
		});
		SoundPlayer.PlayOperate();
		await base.transform.DOMoveY(targetPosition.y, speed).SetSpeedBased().SetEase(Ease.InOutSine)
			.WaitAsync();
		movingPlatforms.ForEach(delegate(MovingPlatform p)
		{
			p.ModifyPath(setGround: true);
		});
		ImpulseSource.GenerateImpulse(ImpulseSource.m_DefaultVelocity / 2f);
		SoundPlayer.PlayStop();
		pipeController.ClosePipe(index);
		SoundPlayer.PlayConnect();
		await UniTask.WaitForSeconds(3f);
		if (!(interactVcam != null))
		{
			await DOTween.To(() => virtualCamera.m_Lens.OrthographicSize, delegate(float x)
			{
				virtualCamera.m_Lens.OrthographicSize = x;
			}, originSize, 1f).WaitAsync();
		}
		else
		{
			interactVcam.gameObject.SetActive(value: false);
			await UniTask.WaitForSeconds(1.5f);
			await UniTask.WaitUntil(() => !CineBrain.IsBlending && CineBrain.ActiveVirtualCamera == originVcam);
		}
		ChangeState(index);
	}

	private Vector2 GetTargetPosition(int index)
	{
		return originPosition + positions[index];
	}
}
