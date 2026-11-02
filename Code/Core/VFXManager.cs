using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
	[SerializeField]
	private List<EffectInfo> vfxList;

	private static VFXManager _instance;

	private readonly Dictionary<VFXEnum, List<ParticleSystem>> _vfxPoolDictionary = new Dictionary<VFXEnum, List<ParticleSystem>>();

	public static VFXManager Instance
	{
		get
		{
			if (_instance.IsNull())
			{
				_instance = Object.FindFirstObjectByType<VFXManager>();
			}
			return _instance;
		}
	}

	public void EnableVFX(VFXEnum particleName, Transform source)
	{
		EffectInfo effectInfo = vfxList.Find((EffectInfo x) => x.name == particleName);
		if (effectInfo.name != VFXEnum.None)
		{
			ParticleSystem disabledEffect = GetDisabledEffect(particleName);
			Vector2 vector = (Vector2)source.position + ((source.localScale.x > 0f) ? new Vector2(1f, 1f) : new Vector2(-1f, 1f)) * effectInfo.localPos;
			Transform parent = (effectInfo.followSource ? source : base.transform);
			Transform obj = disabledEffect.transform;
			obj.position = vector;
			obj.parent = parent;
			disabledEffect.gameObject.SetActive(value: true);
			disabledEffect.Play();
		}
	}

	private ParticleSystem GetDisabledEffect(VFXEnum particleName)
	{
		if (!_vfxPoolDictionary.ContainsKey(particleName))
		{
			_vfxPoolDictionary.Add(particleName, new List<ParticleSystem>());
		}
		List<ParticleSystem> list = _vfxPoolDictionary[particleName];
		using (IEnumerator<ParticleSystem> enumerator = list.Where((ParticleSystem particle) => !particle.isPlaying).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				ParticleSystem current = enumerator.Current;
				current.Stop();
				return current;
			}
		}
		ParticleSystem particleSystem = InstantiateByName(particleName);
		list.Add(particleSystem);
		return particleSystem;
	}

	private ParticleSystem InstantiateByName(VFXEnum particleName)
	{
		EffectInfo effectInfo = vfxList.Find((EffectInfo x) => x.name == particleName);
		if (effectInfo.name == VFXEnum.None)
		{
			return null;
		}
		ParticleSystem particleSystem = Object.Instantiate(effectInfo.particle);
		particleSystem.gameObject.SetActive(value: false);
		return particleSystem;
	}
}
