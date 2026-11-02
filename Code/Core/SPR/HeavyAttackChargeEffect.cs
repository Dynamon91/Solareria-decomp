using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SPR;

public class HeavyAttackChargeEffect : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private ParticleSystem chargingParticle;

	[SerializeField]
	private ParticleSystem chargingSuccessParticle;

	private Light2D light2D;

	private Sequence lightSequence;

	private List<float> alphaValues;

	private const float LightDuration = 0.5f;

	public string PrefabKey => "HeavyAttack_Charging.prefab";

	public GameObject GameObject => base.gameObject;

	private void Awake()
	{
		light2D = chargingSuccessParticle.GetComponentInChildren<Light2D>();
	}

	public void InitializeBeforeUse()
	{
		ParticleSystem[] componentsInChildren = chargingParticle.GetComponentsInChildren<ParticleSystem>();
		if (alphaValues.IsNullOrEmpty())
		{
			alphaValues = new List<float>();
			ParticleSystem[] array = componentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				ParticleSystem.MainModule main = array[i].main;
				alphaValues.Add(main.startColor.color.a);
			}
		}
		else
		{
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				ParticleSystem.MainModule main2 = componentsInChildren[j].main;
				Color color = main2.startColor.color;
				color.a = alphaValues[j];
				main2.startColor = color;
			}
		}
		base.gameObject.SetActive(value: true);
		chargingParticle.gameObject.SetActive(value: true);
		chargingSuccessParticle.gameObject.SetActive(value: false);
	}

	public void PlayParticle(bool success)
	{
		if (success)
		{
			if (chargingParticle.isPlaying)
			{
				chargingParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				chargingParticle.gameObject.SetActive(value: false);
			}
			chargingSuccessParticle.gameObject.SetActive(value: true);
			chargingSuccessParticle.Play();
		}
		else
		{
			chargingParticle.Play();
		}
	}

	public async void ReturnToPool()
	{
		if (chargingParticle.isPlaying)
		{
			ParticleSystem[] componentsInChildren = chargingParticle.GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem particleSystem in componentsInChildren)
			{
				ParticleSystem.MainModule main = particleSystem.main;
				DOTween.To(() => main.startColor.color.a, delegate(float x)
				{
					Color color = main.startColor.color;
					color.a = x;
					main.startColor = color;
				}, 0f, 0.5f);
			}
			chargingParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			await UniTask.WaitForSeconds(0.5f, ignoreTimeScale: true);
		}
		if (chargingSuccessParticle.isPlaying)
		{
			chargingSuccessParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			lightSequence?.Kill();
			await UniTask.WaitUntil(() => !chargingSuccessParticle.isEmitting);
		}
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
