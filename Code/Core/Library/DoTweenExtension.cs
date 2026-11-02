using Spine.Unity;
using System;
using System.Globalization;
using Coffee.UIEffects;
using DG.Tweening;
using Dreamteck.Splines;
using SPR;
using Spine;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Library;

public static class DoTweenExtension
{
	public static Tween DOAlpha(this CanvasGroup canvasGroup, float endValue, float duration)
	{
		return DOTween.To(() => canvasGroup.alpha, delegate(float x)
		{
			canvasGroup.alpha = x;
		}, endValue, duration);
	}

	public static Tween DOAlpha(this SpriteRenderer target, float endValue, float duration)
	{
		Color color = target.color;
		color.a = endValue;
		return target.DOColor(color, duration);
	}

	public static Tween DOAlpha(this Image target, float endValue, float duration)
	{
		Color color = target.color;
		color.a = endValue;
		return target.DOColor(color, duration);
	}

	public static Tween DOAlpha(this TextMeshProUGUI target, float endValue, float duration)
	{
		Color color = target.color;
		color.a = endValue;
		return target.DOColor(color, duration);
	}

	public static Tween DOColor(this Skeleton skeleton, Color endValue, float duration)
	{
		return DOTween.To(skeleton.GetColor, skeleton.SetColor, endValue, duration);
	}

	public static Tween DOAlpha(this Skeleton skeleton, float endValue, float duration)
	{
		return DOTween.To(() => skeleton.A, delegate(float x)
		{
			skeleton.A = x;
		}, endValue, duration);
	}

	public static Tween DOAlpha(this ParticleSystem particle, float endValue, float duration)
	{
		ParticleSystem.MainModule main = particle.main;
		return DOTween.To(() => particle.main.startColor.color.a, delegate(float x)
		{
			Color color = particle.main.startColor.color;
			main.startColor = new Color(color.r, color.g, color.b, x);
		}, endValue, duration);
	}

	public static Tween DoLightOuterRadius(this Light2D light, float endValue, float duration)
	{
		return DOTween.To(() => light.pointLightOuterRadius, delegate(float x)
		{
			light.pointLightOuterRadius = x;
		}, endValue, duration);
	}

	public static Tween DoLightIntensity(this Light2D light, float endValue, float duration)
	{
		return DOTween.To(() => light.intensity, delegate(float x)
		{
			light.intensity = x;
		}, endValue, duration);
	}

	public static Tween DoLightVolumeIntensity(this Light2D light, float endValue, float duration)
	{
		return DOTween.To(() => light.volumeIntensity, delegate(float x)
		{
			light.volumeIntensity = x;
		}, endValue, duration);
	}

	public static Tween DoVelocity(this Rigidbody2D rb2D, Vector2 endValue, float duration)
	{
		return DOTween.To(() => rb2D.velocity, delegate(Vector2 x)
		{
			rb2D.velocity = x;
		}, endValue, duration);
	}

	public static Tween DOAngularVelocity(this Rigidbody2D rb2D, float endValue, float duration)
	{
		return DOTween.To(() => rb2D.angularVelocity, delegate(float x)
		{
			rb2D.angularVelocity = x;
		}, endValue, duration);
	}

	public static Tween DOLifetime(this ParticleSystem particle, float endValue, float duration)
	{
		ParticleSystem.MainModule main = particle.main;
		return DOTween.To(() => main.startLifetime.constant, delegate(float x)
		{
			main.startLifetime = x;
		}, endValue, duration);
	}

	public static DG.Tweening.Sequence DOShakeFloat(this float value, float originValue, float maxByValue, float minByValue, float minToMaxDuration, Ease toMinEase = Ease.InOutSine, Ease toMaxEase = Ease.InOutSine)
	{
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		sequence.Append(DOTween.To(() => value, delegate(float x)
		{
			value = x;
		}, originValue + minByValue, minToMaxDuration).SetEase(toMinEase)).Append(DOTween.To(() => value, delegate(float x)
		{
			value = x;
		}, originValue + maxByValue, minToMaxDuration).SetEase(toMaxEase));
		return sequence;
	}

	public static Tween DOShineColor(this Material material, int shaderPropertyID, Color endValue, float duration)
	{
		return DOTween.To(() => material.GetColor(shaderPropertyID), delegate(Color x)
		{
			material.SetColor(shaderPropertyID, x);
		}, endValue, duration);
	}

	public static Tween DOShaderFloatProperty(this Material material, string shaderPropertyName, float endValue, float duration)
	{
		return material.DOShaderFloatProperty(Shader.PropertyToID(shaderPropertyName), endValue, duration);
	}

	public static Tween DOShaderFloatProperty(this Material material, int shaderNameID, float endValue, float duration)
	{
		return DOTween.To(() => material.GetFloat(shaderNameID), delegate(float x)
		{
			material.SetFloat(shaderNameID, x);
		}, endValue, duration);
	}

	public static Tween DOFloatPropertyBlock(this MaterialPropertyBlock block, int nameID, float endValue, float duration, SpineObject spineObject)
	{
		block.SetFloat(nameID, spineObject.GetMaterial().GetFloat(nameID));
		return DOTween.To(() => block.GetFloat(nameID), delegate(float x)
		{
			block.SetFloat(nameID, x);
			spineObject.SetPropertyBlock(block);
		}, endValue, duration);
	}

	public static Tween DOFloatPropertyBlock(this MaterialPropertyBlock block, int nameID, float endValue, float duration, MeshRenderer meshRenderer)
	{
		block.SetFloat(nameID, meshRenderer.material.GetFloat(nameID));
		return DOTween.To(() => block.GetFloat(nameID), delegate(float x)
		{
			block.SetFloat(nameID, x);
			meshRenderer.SetPropertyBlock(block);
		}, endValue, duration);
	}

	public static Tween DOFloatPropertyBlock(this MaterialPropertyBlock block, int nameID, float endValue, float duration, Renderer renderer)
	{
		return DOTween.To(() => block.GetFloat(nameID), delegate(float x)
		{
			block.SetFloat(nameID, x);
			renderer.SetPropertyBlock(block);
		}, endValue, duration);
	}

	public static DG.Tweening.Sequence DOBounceInPlace(this RectTransform rt, Vector2 initialPos, int size = 20, int horizontal = 0, int vertical = -1)
	{
		float x = initialPos.x;
		float y = initialPos.y;
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		if (horizontal != 0)
		{
			sequence.Append(rt.DOAnchorPosX(x + (float)(size * horizontal), 0.3f)).SetEase(Ease.InOutCubic).Append(rt.DOAnchorPosX(x - (float)size * 0.6f * (float)horizontal, 0.18f))
				.SetEase(Ease.OutCubic)
				.Append(rt.DOAnchorPosX(x + (float)size * 0.35f * (float)horizontal, 0.1f))
				.SetEase(Ease.OutCubic)
				.Append(rt.DOAnchorPosX(x - (float)size * 0.2f * (float)horizontal, 0.06f))
				.SetEase(Ease.OutCubic)
				.Append(rt.DOAnchorPosX(x, 0.04f))
				.SetEase(Ease.OutCubic);
		}
		if (vertical != 0)
		{
			sequence.Append(rt.DOAnchorPosY(y + (float)(size * vertical), 0.3f)).SetEase(Ease.InOutCubic).Append(rt.DOAnchorPosY(y - (float)size * 0.6f * (float)vertical, 0.18f))
				.SetEase(Ease.OutCubic)
				.Append(rt.DOAnchorPosY(y + (float)size * 0.35f * (float)vertical, 0.1f))
				.SetEase(Ease.OutCubic)
				.Append(rt.DOAnchorPosY(y - (float)size * 0.2f * (float)vertical, 0.06f))
				.SetEase(Ease.OutCubic)
				.Append(rt.DOAnchorPosY(y, 0.04f))
				.SetEase(Ease.OutCubic);
		}
		return sequence;
	}

	public static Tween DOIntText(this TextMeshProUGUI text, int endValue, float duration)
	{
		return DOTween.To(() => text.text.ParseIntOrDefault(), delegate(int x)
		{
			text.text = x.ToString(CultureInfo.InvariantCulture);
		}, endValue, duration);
	}

	public static Tween DOIntTextWithPrefix(this TextMeshProUGUI text, int endValue, string prefix, float duration)
	{
		return DOTween.To(() => text.text.Replace(prefix, null).ParseIntOrDefault(), delegate(int x)
		{
			text.text = prefix + x.ToString(CultureInfo.InvariantCulture);
		}, endValue, duration);
	}

	public static Tween DODissolve(this UIDissolve target, float endValue, float duration)
	{
		return DOTween.To(() => target.effectFactor, delegate(float x)
		{
			target.effectFactor = x;
		}, endValue, duration);
	}

	[Obsolete("Use DOScroll(Scrollbar scrollbar, float value, float seconds) instead.")]
	public static Tween DOScroll(this Scrollbar scrollbar, float value, float seconds)
	{
		return DOTween.To(() => scrollbar.value, delegate(float x)
		{
			scrollbar.value = x;
		}, value, seconds);
	}

	public static Tween DOScroll(this ScrollRect scrollRect, float value, float duration)
	{
		value = Mathf.Clamp01(value);
		return DOTween.To(() => scrollRect.verticalNormalizedPosition, delegate(float x)
		{
			scrollRect.verticalNormalizedPosition = x;
		}, value, duration);
	}

	public static Tween DOFollow(this SplineFollower follower, double duration, bool needReset = true)
	{
		if (needReset)
		{
			follower.RebuildImmediate();
			follower.SetPercent(0.0);
		}
		return DOTween.To(follower.GetPercent, delegate(double x)
		{
			follower.SetPercent(x);
		}, 1.0, (float)duration);
	}

	public static Tween DOPivotMoveX(this Transform newTransform, float endPosX, float duration)
	{
		return DOTween.To(() => newTransform.position.x, delegate(float x)
		{
			Vector3 position = newTransform.position;
			position.x = x;
			newTransform.position = position;
		}, endPosX, duration);
	}

	public static Tween DOPivotMoveY(this Transform newTransform, float endPosY, float duration)
	{
		return DOTween.To(() => newTransform.position.y, delegate(float y)
		{
			Vector3 position = newTransform.position;
			position.y = y;
			newTransform.position = position;
		}, endPosY, duration);
	}

	public static Tween DOPivotLocalMoveX(this Transform newTransform, float endPosX, float duration)
	{
		return DOTween.To(() => newTransform.localPosition.x, delegate(float x)
		{
			Vector3 localPosition = newTransform.localPosition;
			localPosition.x = x;
			newTransform.localPosition = localPosition;
		}, endPosX, duration);
	}

	public static Tween DOPivotLocalMoveY(this Transform newTransform, float endPosY, float duration)
	{
		return DOTween.To(() => newTransform.localPosition.y, delegate(float y)
		{
			Vector3 localPosition = newTransform.localPosition;
			localPosition.y = y;
			newTransform.localPosition = localPosition;
		}, endPosY, duration);
	}

	public static DG.Tweening.Sequence DONodYOnce(this RectTransform rect, float deltaY, float duration)
	{
		float y = rect.anchoredPosition.y;
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		sequence.Append(rect.DOAnchorPosY(y + deltaY, duration / 2f)).Append(rect.DOAnchorPosY(y, duration / 2f));
		return sequence;
	}

	public static DG.Tweening.Sequence DODrop(this Transform target, Vector2 startPos, Vector2 endPos, float initialVelocityY = 36f, float gravity = -120f)
	{
		float z = target.position.z;
		target.position = new Vector3(startPos.x, startPos.y, z);
		float qa = 0.5f * gravity;
		float qb = initialVelocityY;
		float qc = startPos.y - endPos.y;
		float num = SolvePositiveTime(qa, qb, qc);
		if (num <= 0f)
		{
			Debug.LogWarning($"[DODrop] 유효하지 않은 착지 시간 계산(t={num}). 입력을 확인하세요. " + $"g={gravity}, v0Y={initialVelocityY}, startY={startPos.y}, endY={endPos.y}");
			num = 0.1f;
		}
		float vx = (endPos.x - startPos.x) / num;
		DG.Tweening.Sequence sequence = DOTween.Sequence().SetTarget(target);
		sequence.Append(DOVirtual.Float(0f, num, num, delegate(float t)
		{
			float x = startPos.x + vx * t;
			float y = startPos.y + initialVelocityY * t + 0.5f * gravity * t * t;
			Vector3 position = new Vector3(x, y, z);
			target.position = position;
		}).SetEase(Ease.Linear));
		sequence.OnComplete(delegate
		{
			target.position = new Vector3(endPos.x, endPos.y, z);
		});
		return sequence;
		static float SolvePositiveTime(float num2, float num3, float num5)
		{
			if (Mathf.Abs(num2) < 1E-06f)
			{
				if (Mathf.Abs(num3) < 1E-06f)
				{
					return 0f;
				}
				float num4 = (0f - num5) / num3;
				if (!(num4 > 0f))
				{
					return 0f;
				}
				return num4;
			}
			float num6 = num3 * num3 - 4f * num2 * num5;
			if (num6 < 0f)
			{
				return 0f;
			}
			float num7 = Mathf.Sqrt(num6);
			float num8 = (0f - num3 + num7) / (2f * num2);
			float num9 = (0f - num3 - num7) / (2f * num2);
			if (num8 > 0f && num9 > 0f)
			{
				return Mathf.Max(num8, num9);
			}
			if (num8 > 0f)
			{
				return num8;
			}
			if (num9 > 0f)
			{
				return num9;
			}
			return 0f;
		}
	}
}
