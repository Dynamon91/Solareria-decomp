using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class DefenseObject : StuckableObject, IAttackSource
{
	[SerializeField]
	private Transform upRootTransform;

	[SerializeField]
	private Transform downRootTransform;

	[SerializeField]
	private Transform maskTransform;

	[SerializeField]
	private List<Transform> gateTransforms;

	[SerializeField]
	private List<SpriteRenderer> slicedRenderers;

	[SerializeField]
	private CinemachineVirtualCamera vcam;

	[SerializeField]
	private ParticleSystem idleParticle;

	[SerializeField]
	private ParticleSystem touchParticle;

	[SerializeField]
	private EventReference openEvent;

	[SerializeField]
	private EventReference touchEvent;

	private int damageRate;

	private bool isContactingPlayer;

	private Sequence activeSequence;

	private readonly List<Vector2> groundRange = new List<Vector2>();

	private BoxCollider2D boxCol;

	private CinemachineBrain cineBrain;

	private Animator animator;

	public bool IsActive => BoxCollider.enabled;

	private BoxCollider2D BoxCollider => boxCol ?? (boxCol = GetComponent<BoxCollider2D>());

	private CinemachineBrain CineBrain => cineBrain ?? (cineBrain = Camera.main.GetComponent<CinemachineBrain>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static PlayerController Player => Game.Instance.Player;

	private static FieldController FieldController => Game.Instance.FieldController;

	public string Name => "DefenseObject";

	public Direction FacingDirection
	{
		get
		{
			if (!(Player.mover.VisualCenter.x - base.transform.position.x >= 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public void Initialize(int height, bool isLocked, PolygonCollider2D confiner)
	{
		Vector2 size = BoxCollider.size;
		size.y = height;
		BoxCollider.size = size;
		Vector3 localScale = maskTransform.localScale;
		localScale.y = height;
		maskTransform.localScale = localScale;
		Vector3 localPosition = upRootTransform.localPosition;
		localPosition.y = size.y / 2f;
		upRootTransform.localPosition = localPosition;
		localPosition = downRootTransform.localPosition;
		localPosition.y = 0f - size.y / 2f;
		downRootTransform.localPosition = localPosition;
		foreach (SpriteRenderer slicedRenderer in slicedRenderers)
		{
			Vector2 size2 = slicedRenderer.size;
			size2.y = 2.054f * size.y;
			slicedRenderer.size = size2;
		}
		vcam.gameObject.SetActive(value: false);
		vcam.GetComponent<CinemachineConfiner>().m_BoundingShape2D = confiner;
		SetActiveImmediately(isLocked);
		Vector2 vector = (Vector2)base.transform.position - BoxCollider.size / 2f;
		Vector2 vector2 = (Vector2)base.transform.position + new Vector2(BoxCollider.size.x, 0f - BoxCollider.size.y) / 2f;
		for (float num = vector.x - 1f; num <= vector2.x; num += 1f)
		{
			groundRange.Add(new Vector2(num, vector.y));
		}
		FieldController.ModifyPathGraphs(groundRange, !isLocked);
	}

	private void FixedUpdate()
	{
		if (IsActive && AttackUtility.AttackBox(hitInfo: new HitInfo(this, null), center: base.transform.position, size: BoxCollider.size, attackLayer: LayerUtility.PlayerLayer).Count != 0 && !Player.mover.IsImmortal && !Player.mover.IgnoreDamage)
		{
			isContactingPlayer = true;
			if (isContactingPlayer)
			{
				isContactingPlayer = false;
				touchParticle.Play();
				StuckableObject.SoundManager.PlayOneShotAttached(touchEvent, base.transform);
			}
		}
	}

	public async UniTask ActivateAsync()
	{
		ICinemachineCamera originVcam = CineBrain.ActiveVirtualCamera;
		vcam.gameObject.SetActive(value: true);
		await UniTask.WaitUntil(() => !CineBrain.IsBlending && CineBrain.ActiveVirtualCamera == vcam);
		BoxCollider.enabled = false;
		idleParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		await ActiveGateAsync(isLocked: false, instant: false);
		await UniTaskExtension.WaitForSeconds(0.6f);
		vcam.gameObject.SetActive(value: false);
		await UniTask.WaitUntil(() => !CineBrain.IsBlending && CineBrain.ActiveVirtualCamera == originVcam);
		FieldController.ModifyPathGraphs(groundRange, setGround: true);
	}

	private void SetActiveImmediately(bool isLocked)
	{
		if (isLocked)
		{
			idleParticle.Play();
		}
		else
		{
			isContactingPlayer = false;
			idleParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}
		ActiveGateAsync(isLocked, instant: true).Forget();
		BoxCollider.enabled = isLocked;
		touchParticle.Stop();
	}

	private async UniTask ActiveGateAsync(bool isLocked, bool instant)
	{
		Animator.enabled = isLocked;
		activeSequence?.Kill();
		float num = (isLocked ? 0f : (upRootTransform.localPosition.y * 4f));
		if (instant)
		{
			foreach (Transform gateTransform in gateTransforms)
			{
				gateTransform.localPosition = new Vector3(0f, num);
			}
			return;
		}
		activeSequence = DOTween.Sequence();
		foreach (Transform gateTransform2 in gateTransforms)
		{
			activeSequence.Join(gateTransform2.DOLocalMoveY(num, 2f / 3f).SetEase(Ease.InOutSine));
		}
		StuckableObject.SoundManager.PlayOneShot(openEvent);
		await activeSequence.WaitAsync();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (hitInfo.AttackerType != HitType.Player)
		{
			return false;
		}
		Animator.Play("Defense_Object_hit");
		touchParticle.Play();
		StuckableObject.SoundManager.PlayOneShotAttached(touchEvent, base.transform);
		return true;
	}
}
