using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class Fog : MonoBehaviour
{
	[SerializeField]
	protected AreaBox coverAreaBox;

	[SerializeField]
	private int margin;

	[SerializeField]
	private bool playRevealSFX;

	[SerializeField]
	private bool isAmbushed;

	private bool isDark;

	private SpriteRenderer spriteRenderer;

	private List<SpriteRenderer> coveredSprites;

	private List<SkeletonAnimation> coveredSkeletonAnimations;

	private const float FadeDuration = 0.5f;

	private SpriteRenderer SpriteRenderer => spriteRenderer ?? (spriteRenderer = GetComponent<SpriteRenderer>());

	private List<SpriteRenderer> CoveredSprites => coveredSprites ?? (coveredSprites = GetComponentsInChildren<SpriteRenderer>().ToList());

	private List<SkeletonAnimation> CoveredSkeletonAnimations => coveredSkeletonAnimations ?? (coveredSkeletonAnimations = GetComponentsInChildren<SkeletonAnimation>().ToList());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public async UniTask FadeOutAsync(bool instant)
	{
		if (!instant && isDark)
		{
			return;
		}
		isDark = true;
		base.gameObject.SetActive(value: true);
		if (instant)
		{
			foreach (SpriteRenderer coveredSprite in CoveredSprites)
			{
				coveredSprite.SetAlpha(1f);
			}
			{
				foreach (SkeletonAnimation coveredSkeletonAnimation in CoveredSkeletonAnimations)
				{
					coveredSkeletonAnimation.Skeleton.SetAlpha(1f);
				}
				return;
			}
		}
		List<UniTask> list = new List<UniTask>();
		foreach (SpriteRenderer coveredSprite2 in CoveredSprites)
		{
			list.Add(coveredSprite2.DOFade(1f, 0.5f).WaitAsync());
		}
		foreach (SkeletonAnimation coveredSkeletonAnimation2 in CoveredSkeletonAnimations)
		{
			list.Add(coveredSkeletonAnimation2.Skeleton.DOAlpha(1f, 0.5f).WaitAsync());
		}
		await UniTask.WhenAll(list);
	}

	public async UniTask FadeInAsync(bool instant)
	{
		if (!instant && !isDark)
		{
			return;
		}
		isDark = false;
		if (instant)
		{
			foreach (SpriteRenderer coveredSprite in CoveredSprites)
			{
				coveredSprite.SetAlpha(0f);
			}
			{
				foreach (SkeletonAnimation coveredSkeletonAnimation in CoveredSkeletonAnimations)
				{
					coveredSkeletonAnimation.Skeleton.SetAlpha(0f);
				}
				return;
			}
		}
		List<UniTask> list = new List<UniTask>();
		foreach (SpriteRenderer coveredSprite2 in CoveredSprites)
		{
			list.Add(coveredSprite2.DOFade(0f, 0.5f).WaitAsync());
		}
		foreach (SkeletonAnimation coveredSkeletonAnimation2 in CoveredSkeletonAnimations)
		{
			list.Add(coveredSkeletonAnimation2.Skeleton.DOAlpha(0f, 0.5f).WaitAsync());
		}
		if (playRevealSFX)
		{
			string path = (isAmbushed ? "event:/SFX/UI/Ingame/SFX_Ambushed" : "event:/SFX/UI/Ingame/SFX_Hidden_Open");
			SoundManager.PlayOneShotAttached(path, base.transform);
		}
		await UniTask.WhenAll(list);
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (coverAreaBox.Center == Vector2.zero)
		{
			ApplyOthersToAreaBox();
			return;
		}
		Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
		Gizmos.DrawCube(coverAreaBox.Center, coverAreaBox.Size.ToVector2());
		base.transform.position = coverAreaBox.Center;
		Vector3 localScale = base.transform.localScale;
		int num = coverAreaBox.Size.x + margin * 2;
		int num2 = coverAreaBox.Size.y + margin * 2;
		_ = SpriteRenderer.size;
		Vector2 size = new Vector2(num, num2) / localScale;
		SpriteRenderer.size = size;
	}

	private void ApplyAreaBoxToOthers()
	{
		Vector3 localScale = base.transform.localScale;
		int num = coverAreaBox.Size.x + margin * 2;
		int num2 = coverAreaBox.Size.y + margin * 2;
		SpriteRenderer.size = new Vector2(num, num2) / localScale;
		base.transform.position = coverAreaBox.Center;
	}

	private void ApplyOthersToAreaBox()
	{
		Vector2 size = SpriteRenderer.size;
		size = new Vector2(size.x - (float)(margin * 2), size.y - (float)(margin * 2));
		Vector2 vector = base.transform.position;
		Vector2 vector2 = vector - size / 2f;
		Vector2 vector3 = vector + size / 2f;
		Debug.Log($"{base.name} : {vector2} ~ {vector3} (size: {size})");
	}
}
