using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class VeritaAura : MonoBehaviour
{
	[SerializeField]
	private Transform initialPos;

	private MeshRenderer meshRenderer;

	private List<ParticleSystem> allParticles;

	private MaterialPropertyBlock materialPropertyBlock;

	private float alpha;

	private const string FadeProperty = "_Alpha";

	private static readonly int FadePropertyID = Shader.PropertyToID("_Alpha");

	private bool isInitialized;

	private bool isPlaying;

	private Transform originalParent;

	private void Awake()
	{
		allParticles = GetComponentsInChildren<ParticleSystem>(includeInactive: true).ToList();
		materialPropertyBlock = new MaterialPropertyBlock();
	}

	public void Initialize(SpineObject spine)
	{
		meshRenderer = spine.GetComponentInChildren<MeshRenderer>();
		originalParent = base.transform.parent;
		base.transform.SetParent(spine.transform);
		ConnectSpine(spine.SkeletonRenderer);
		allParticles.ForEach(delegate(ParticleSystem p)
		{
			p.Play();
		});
		isInitialized = true;
		isPlaying = true;
	}

	private void ConnectSpine(SkeletonRenderer skeletonRenderer)
	{
		BoneFollower[] componentsInChildren = GetComponentsInChildren<BoneFollower>();
		foreach (BoneFollower obj in componentsInChildren)
		{
			obj.skeletonRenderer = skeletonRenderer;
			obj.Initialize();
			obj.LateUpdate();
		}
	}

	public void StartEnding(SkeletonRenderer spine)
	{
		base.transform.SetParent(null);
		ConnectSpine(spine);
		allParticles.ForEach(delegate(ParticleSystem p)
		{
			p.Play();
		});
		isInitialized = false;
	}

	public void FinalizeUpdate()
	{
		allParticles.ForEach(delegate(ParticleSystem p)
		{
			p.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		});
		UniTask.WaitUntil(() => allParticles.All((ParticleSystem p) => !p.IsAlive())).GetAwaiter().OnCompleted(delegate
		{
			base.transform.SetParent(originalParent);
			base.gameObject.SetActive(value: false);
		});
	}

	private void Update()
	{
		if (!isInitialized || !meshRenderer.gameObject.activeInHierarchy)
		{
			return;
		}
		materialPropertyBlock.Clear();
		meshRenderer.GetPropertyBlock(materialPropertyBlock);
		if (materialPropertyBlock.isEmpty || !materialPropertyBlock.HasFloat(FadePropertyID))
		{
			return;
		}
		alpha = materialPropertyBlock.GetFloat(FadePropertyID);
		if (alpha == 0f)
		{
			if (isPlaying)
			{
				allParticles.ForEach(delegate(ParticleSystem p)
				{
					p.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				});
				isPlaying = false;
			}
		}
		else if (!isPlaying)
		{
			allParticles.ForEach(delegate(ParticleSystem p)
			{
				p.Play();
			});
			isPlaying = true;
		}
	}
}
