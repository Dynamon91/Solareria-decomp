using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.ObjectPool;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class MoldinWeapon : SpineObject, IPoolableObject, IEventListener
{
	[Serializable]
	private class SortingTiming
	{
		[SerializeField]
		public string weaponName;

		[SerializeField]
		public List<MinMax> timings;
	}

	[Serializable]
	private class FadeData
	{
		[SerializeField]
		public string weaponName;

		[SerializeField]
		public MinMax value;
	}

	[SerializeField]
	private string address;

	private Transform followTarget;

	private Tween tween;

	[SerializeField]
	private List<SortingTiming> sortingTimings;

	[SerializeField]
	private List<FadeData> fadeData;

	[SerializeField]
	private SkeletonPartsRenderer[] skeletonParts;

	private readonly Dictionary<string, List<MinMax>> sortingDict = new Dictionary<string, List<MinMax>>();

	private readonly Dictionary<string, MinMax> fadeDict = new Dictionary<string, MinMax>();

	private string currentAnimation;

	public override string Name => "";

	public virtual string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	private void Awake()
	{
		LazySingleton<MessageSystem>.Instance.Subscribe<MoldinWeaponDisappearEvent>(this);
		foreach (SortingTiming sortingTiming in sortingTimings)
		{
			sortingDict.TryAdd(sortingTiming.weaponName, sortingTiming.timings);
		}
		foreach (FadeData fadeDatum in fadeData)
		{
			fadeDict.TryAdd(fadeDatum.weaponName, fadeDatum.value);
		}
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<MoldinWeaponDisappearEvent>(this);
	}

	public virtual void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		currentAnimation = null;
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is MoldinWeaponDisappearEvent && base.gameObject.activeSelf)
		{
			tween.Kill();
			RemoveAnimation();
			ApplyFade(appear: false, 1f);
			DisableVFXAll();
		}
	}

	public void SetWeapon(string animationName)
	{
		base.SkeletonAnimation.AnimationState.ClearTrack(0);
		base.SkeletonRenderer.Skeleton.SetToSetupPose();
		currentAnimation = animationName;
		SetAnimation(currentAnimation, loop: false);
	}

	public void StopFollowing(int fadeDuration)
	{
		base.transform.SetParent(null);
		ApplyFade(appear: false, Util.FrameToSeconds(fadeDuration));
	}

	public void ApplyFade(bool appear, float duration, string animationName = null, bool autoReturn = true)
	{
		if (string.IsNullOrEmpty(currentAnimation))
		{
			currentAnimation = GetCurrentAnim(0).Name;
		}
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(Shader.PropertyToID("_FadeAmount"), appear ? fadeDict[currentAnimation].min : 0f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		tween = DOFade(materialPropertyBlock, appear ? 0f : fadeDict[currentAnimation].max, duration).OnComplete(delegate
		{
			if (!appear && autoReturn)
			{
				ReturnToPoolAsync(string.IsNullOrEmpty(animationName) ? currentAnimation : animationName).Forget();
			}
		});
	}

	public async UniTaskVoid ApplyFade(bool appear, float duration, float wait, string animationName = null)
	{
		if (string.IsNullOrEmpty(currentAnimation))
		{
			currentAnimation = GetCurrentAnim(0).Name;
		}
		await UniTask.WaitForSeconds(wait);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(Shader.PropertyToID("_FadeAmount"), appear ? fadeDict[currentAnimation].min : 0f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		tween = DOFade(materialPropertyBlock, appear ? 0f : fadeDict[currentAnimation].max, duration).OnComplete(delegate
		{
			if (!appear)
			{
				ReturnToPoolAsync(string.IsNullOrEmpty(animationName) ? currentAnimation : animationName).Forget();
			}
		});
	}

	public void SetLayerOrder(string weaponName, int frame)
	{
		if (!sortingDict.TryGetValue(weaponName, out var value))
		{
			return;
		}
		foreach (MinMax item in value)
		{
			if (item.min > (float)frame || (float)frame > item.max)
			{
				skeletonParts.ForEach((SkeletonPartsRenderer p) => p.MeshRenderer.sortingOrder = 1);
				base.SortingOrder = 1;
				continue;
			}
			skeletonParts.ForEach((SkeletonPartsRenderer p) => p.MeshRenderer.sortingOrder = -1);
			base.SortingOrder = -1;
			break;
		}
	}

	private Tween DOFade(MaterialPropertyBlock mpb, float endValue, float duration)
	{
		return DOTween.To(() => mpb.GetFloat(Shader.PropertyToID("_FadeAmount")), delegate(float x)
		{
			mpb.SetFloat(Shader.PropertyToID("_FadeAmount"), x);
			MeshRenderer.SetPropertyBlock(mpb);
		}, endValue, duration);
	}

	private async UniTaskVoid ReturnToPoolAsync(string animationName)
	{
		await UniTask.WaitUntil(() => base.VfxController.IsTimelineEnd(animationName));
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
