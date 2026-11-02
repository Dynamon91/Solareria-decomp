using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class VeritaAimArrow : SpineObject, IPoolableObject
{
	private enum State
	{
		Aim,
		Shoot,
		Finished
	}

	[SerializeField]
	private EventReference shootEvent;

	private List<ParticleSystem> loopParticles;

	private BoxCollider2D boxColl;

	private float speed;

	private int currentFrameCount;

	private int delayFrame;

	private Vector3 aimDirection;

	private Vector2 overlapCenter;

	private Vector2 overlapSize;

	private float angle;

	private State state;

	public string PrefabKey => "Verita Space Arrow.prefab";

	public GameObject GameObject => base.gameObject;

	public override string Name => "Verita_Rush_Arrow";

	public override Direction FacingDirection
	{
		get
		{
			if (!(aimDirection.x >= 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	private BoxCollider2D BoxCollider => boxColl ?? (boxColl = GetComponent<BoxCollider2D>());

	private void Awake()
	{
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
		loopParticles = new List<ParticleSystem>(componentsInChildren.Length);
		ParticleSystem[] array = componentsInChildren;
		foreach (ParticleSystem particleSystem in array)
		{
			if (particleSystem.main.loop)
			{
				loopParticles.Add(particleSystem);
			}
		}
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(float speed, int delayFrame)
	{
		state = State.Aim;
		this.speed = speed;
		this.delayFrame = delayFrame;
		currentFrameCount = 0;
		loopParticles.ForEach(delegate(ParticleSystem p)
		{
			ParticleSystem.MainModule main = p.main;
			main.loop = true;
		});
		AimPlayer();
		SetTintColorAsync(Color.white, 0).Forget();
		base.gameObject.SetActive(value: true);
		SetAnimation("arrow_small_start", loop: true, 1f, 0, 0f, vfxEnable: false);
	}

	private void FixedUpdate()
	{
		if (state == State.Shoot)
		{
			base.transform.position += aimDirection * speed * Time.fixedDeltaTime;
			FastSpeedSetting();
			HitInfo hitInfo = new HitInfo(this, string.Empty);
			List<IDamageable> list = AttackUtility.AttackBox(overlapCenter, overlapSize, angle, hitInfo, (int)LayerUtility.PlayerLayer | (int)LayerUtility.PlatformLayer);
			if (list.Count > 0)
			{
				if (hitInfo.TypeToAttack.HasFlag(HitType.Player) && list.FirstOrDefault((IDamageable d) => d is PlayerMover) != null && Game.Instance.Player.mover.CanHit)
				{
					ReturnToPool();
				}
				else if (list.FirstOrDefault((IDamageable d) => d is SPR.Field.Platform) != null)
				{
					DisappearAsync().Forget();
				}
			}
		}
		else if (state == State.Aim)
		{
			if (currentFrameCount >= delayFrame)
			{
				state = State.Shoot;
				SetAnimation("arrow_small", loop: false, 1f, 0, 0f, vfxEnable: false);
				AddAnimation("arrow_small_loop");
				Game.Instance.SoundManager.PlayOneShotAttached(shootEvent, base.transform);
			}
			else
			{
				currentFrameCount++;
				AimPlayer();
			}
		}
	}

	private void FastSpeedSetting()
	{
		float num = speed * Time.fixedDeltaTime;
		Vector2 size = BoxCollider.size;
		float x = num / 2f - size.x / 2f;
		overlapCenter = base.transform.position;
		overlapCenter += aimDirection * (BoxCollider.offset + new Vector2(x, 0f));
		overlapSize = new Vector2(num + size.x, size.y);
	}

	private void AimPlayer()
	{
		Vector2 vector = ((Vector3)Game.Instance.Player.mover.VisualCenter - base.transform.position).normalized;
		aimDirection = vector;
		angle = vector.GetAngle();
		base.transform.rotation = Quaternion.Euler(0f, 0f, angle);
	}

	private async UniTask DisappearAsync()
	{
		state = State.Finished;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(60);
		loopParticles.ForEach(delegate(ParticleSystem p)
		{
			p.StopLooping(includeChildren: false);
		});
		await SetTintColorAsync(new Color(1f, 1f, 1f, 0f), 60);
		ReturnToPool();
	}
}
