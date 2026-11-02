using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.VFX;

namespace SPR;

public class BaranTornado : MonoBehaviour, IPoolableObject, IAttackSource
{
	private const float ETCDefaultDissolve = 0.5f;

	private const float TornadoDefaultDissolve = 0.65f;

	[Header("Particle")]
	[SerializeField]
	private ParticleSystem groundSmoke;

	[SerializeField]
	private ParticleSystem coreSmoke;

	[Header("Dissolve")]
	[SerializeField]
	private MeshRenderer[] toZeroDissolve;

	[SerializeField]
	private MeshRenderer[] toOneDissolve;

	[SerializeField]
	private VisualEffect[] lightningEffects;

	[SerializeField]
	private VisualEffect[] slashEffects;

	private FrameTimer timer;

	private int interval;

	private Lazy<BoxCollider2D> box2D;

	public string PrefabKey => "VFX/Baran_Tornado.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "Baran_SwordEnergy";

	public Direction FacingDirection => Direction.Right;

	public void InitializeBeforeUse()
	{
		ResetAlpha();
		ResetDissolve();
		groundSmoke.Play();
		coreSmoke.Play();
		base.gameObject.SetActive(value: true);
		DOTween.Kill(base.transform);
		Appear(1f);
	}

	public void ReturnToPool()
	{
		timer = null;
		Disappear(1f).Forget();
	}

	private void Awake()
	{
		box2D = new Lazy<BoxCollider2D>(base.GetComponent<BoxCollider2D>);
	}

	private void FixedUpdate()
	{
		if (timer == null)
		{
			return;
		}
		if (timer.IsDone)
		{
			ReturnToPool();
			return;
		}
		timer.Tick();
		if (timer.PassedFrame % interval == 0)
		{
			HandleOverlap();
		}
	}

	public void Initialize(int duration, int attackInterval)
	{
		timer = new FrameTimer(duration);
		interval = attackInterval;
	}

	private void Appear(float duration)
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Append(groundSmoke.DOAlpha(1f, duration));
		sequence.Join(coreSmoke.DOAlpha(1f, duration).SetEase(Ease.Linear));
		lightningEffects.ForEach((VisualEffect x) => sequence.Join(DoDissolve(x, 0f, duration).SetEase(Ease.Linear)));
		slashEffects.ForEach((VisualEffect x) => sequence.Join(DoDissolve(x, 0f, duration).SetEase(Ease.Linear)));
		slashEffects.ForEach((VisualEffect x) => sequence.Join(DoDissolve(x, 0f, duration).SetEase(Ease.Linear)));
		toZeroDissolve.ForEach((MeshRenderer x) => sequence.Join(DoDissolve(x, new MaterialPropertyBlock(), 0.5f, duration).SetEase(Ease.Linear)));
		sequence.Join(DoDissolve(toOneDissolve[0], new MaterialPropertyBlock(), 0.65f, 1f).SetEase(Ease.Linear));
	}

	private async UniTaskVoid Disappear(float duration)
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Append(groundSmoke.DOAlpha(0f, duration));
		sequence.Join(coreSmoke.DOAlpha(0f, duration).SetEase(Ease.Linear));
		lightningEffects.ForEach((VisualEffect x) => sequence.Join(DoDissolve(x, 1f, duration).SetEase(Ease.Linear)));
		slashEffects.ForEach((VisualEffect x) => sequence.Join(DoDissolve(x, 3f, duration).SetEase(Ease.Linear)));
		slashEffects.ForEach((VisualEffect x) => sequence.Join(DoDissolve(x, 3f, duration).SetEase(Ease.Linear)));
		toZeroDissolve.ForEach((MeshRenderer x) => sequence.Join(DoDissolve(x, new MaterialPropertyBlock(), 0f, duration).SetEase(Ease.Linear)));
		sequence.Join(DoDissolve(toOneDissolve[0], new MaterialPropertyBlock(), 1f, duration).SetEase(Ease.Linear));
		await sequence.WaitAsync();
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void ResetAlpha()
	{
		ParticleSystem.MainModule main = groundSmoke.main;
		ParticleSystem.MainModule main2 = coreSmoke.main;
		Color color = main.startColor.color;
		Color color2 = main2.startColor.color;
		main.startColor = new Color(color.r, color.g, color.b, 0f);
		main2.startColor = new Color(color2.r, color2.g, color2.b, 0f);
	}

	private void ResetDissolve()
	{
		int propertyID = Shader.PropertyToID("_Dissolve");
		toZeroDissolve.ForEach(delegate(MeshRenderer x)
		{
			DOTween.Kill(x);
			MaterialPropertyBlock materialPropertyBlock2 = new MaterialPropertyBlock();
			materialPropertyBlock2.SetFloat(propertyID, 0f);
			x.SetPropertyBlock(materialPropertyBlock2);
		});
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(propertyID, 1f);
		toOneDissolve[0].SetPropertyBlock(materialPropertyBlock);
		lightningEffects.ForEach(delegate(VisualEffect x)
		{
			x.SetFloat("Dissolve", 1f);
		});
		slashEffects.ForEach(delegate(VisualEffect x)
		{
			x.SetFloat("Dissolve", 3f);
		});
	}

	private Tween DoDissolve(MeshRenderer meshRenderer, MaterialPropertyBlock mpb, float endValue, float duration)
	{
		int propertyID = Shader.PropertyToID("_Dissolve");
		return DOTween.To(delegate
		{
			meshRenderer.GetPropertyBlock(mpb);
			return mpb.GetFloat(propertyID);
		}, delegate(float x)
		{
			mpb.SetFloat(propertyID, x);
			meshRenderer.SetPropertyBlock(mpb);
		}, endValue, duration);
	}

	private Tween DoDissolve(VisualEffect visualEffect, float endValue, float duration)
	{
		return DOTween.To(() => visualEffect.GetFloat("Dissolve"), delegate(float x)
		{
			visualEffect.SetFloat("Dissolve", x);
		}, endValue, duration);
	}

	private void HandleOverlap()
	{
		Vector3 localScale = base.transform.localScale;
		Vector2 point = (Vector2)base.transform.position + box2D.Value.offset;
		Vector2 size = box2D.Value.size * localScale;
		Collider2D collider2D = Physics2D.OverlapBox(point, size, 0f, LayerUtility.PlayerLayer);
		if (!collider2D.IsNull() && collider2D.TryGetComponent<PlayerMover>(out var component))
		{
			component.TryTakeDamage(new HitInfo(this, string.Empty));
		}
	}
}
