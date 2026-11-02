using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Library;
using SPR.Save;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class TiedPlatform : MonoBehaviour, IStateSavable, IAttackSource, IRideable, IGround
{
	[ReadOnlyInspector]
	[SerializeField]
	private Vector2Int positionInt;

	[Space]
	[Space]
	[Header("Values")]
	[SerializeField]
	private float targetGravity;

	[SerializeField]
	private float terminalSpeedY;

	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	[SerializeField]
	private Vector2 ropeSize;

	[Header("References")]
	[SerializeField]
	private MeshRenderer rockMeshRenderer;

	[SerializeField]
	private BoxCollider2D platform;

	[SerializeField]
	private TiedPlatformRope rope;

	[SerializeField]
	private ParticleSystem idleParticle;

	[SerializeField]
	private ParticleSystem burnParticle;

	[SerializeField]
	private ParticleSystem rockBurnParticle;

	[Header("Sfx")]
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference cutEvent;

	[SerializeField]
	private EventReference fallEvent;

	[SerializeField]
	private EventReference impactEvent;

	private Area area;

	private SkeletonAnimation platformSkeleton;

	private SkeletonAnimation ropeSkeleton;

	private EventInstance fallLoopInstance;

	private bool isFalling;

	private Vector2 velocity;

	private float targetY;

	private readonly List<IDamageable> damaged = new List<IDamageable>();

	private static readonly int FadeAmount = Shader.PropertyToID("_FadeAmount");

	private static readonly int FadeBurnTransition = Shader.PropertyToID("_FadeBurnTransition");

	private static readonly int FadeBurnWidth = Shader.PropertyToID("_FadeBurnWidth");

	public const int TiedState = 0;

	public const int FallingState = 1;

	public const string NameConst = "TiedPlatform";

	private const string IdleAnim = "idle";

	private const string HitAnim = "hit";

	private const string FallingAnim = "falling";

	private const string FallenAnim = "static";

	private Area Area => area ?? (area = GetComponentInParent<Area>());

	private BoxCollider2D RopeCollider => rope.RopeCollider;

	public int CurrentState { get; private set; }

	public string Name => "TiedPlatform";

	public string ID => string.Format("{0}_{1}_{2}", "TiedPlatform", Game.Instance.CurrentFieldID, base.transform.position.ToVector2Int());

	public Direction FacingDirection => Direction.Down;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterial;

	private static LayerMask PlatformLayer => LayerUtility.PlatformLayer;

	private static FieldController FieldController => Game.Instance.FieldController;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public Vector2 Velocity => velocity;

	private void Awake()
	{
		platformSkeleton = platform.GetComponentInChildren<SkeletonAnimation>();
		ropeSkeleton = rope.GetComponentInChildren<SkeletonAnimation>();
		rope.transform.SetParent(base.transform.parent);
	}

	public void LoadState(int state)
	{
		CurrentState = state;
		switch (CurrentState)
		{
		case 0:
			rope.Initialize(this, delegate
			{
				platformSkeleton.AnimationState.SetAnimation(0, "down_hit", loop: false);
				ropeSkeleton.AnimationState.SetAnimation(0, "up_hit", loop: false);
				SoundManager.PlayOneShotAttached(hitEvent, rope.transform);
			}, delegate
			{
				ChangeState(1);
				StartFallAsync().Forget();
			});
			platformSkeleton.AnimationState.SetAnimation(0, "down_idle", loop: true);
			ropeSkeleton.AnimationState.SetAnimation(0, "up_idle", loop: true);
			idleParticle.Play();
			SetDefaultMaterialProperty();
			break;
		case 1:
			platformSkeleton.AnimationState.SetAnimation(0, "down_static", loop: true);
			ropeSkeleton.AnimationState.SetAnimation(0, "up_static", loop: true);
			FallImmediately();
			break;
		}
		isFalling = false;
	}

	public void ChangeState(int state)
	{
		CurrentState = state;
		if (CurrentState == 1)
		{
			RaycastHit2D fallenHit = GetFallenHit();
			targetY = (((object)fallenHit.collider == null) ? float.NegativeInfinity : (fallenHit.centroid.y + platform.size.y / 2f));
			velocity = Vector2.zero;
			damaged.Clear();
		}
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, CurrentState);
		}
	}

	private async UniTask StartFallAsync()
	{
		PlayFallEffect();
		platformSkeleton.AnimationState.SetAnimation(0, "down_falling", loop: false);
		ropeSkeleton.AnimationState.SetAnimation(0, "up_falling", loop: false);
		platformSkeleton.AnimationState.AddAnimation(0, "down_static", loop: true, 0f);
		ropeSkeleton.AnimationState.AddAnimation(0, "up_static", loop: true, 0f);
		SoundManager.PlayOneShotAttached(cutEvent, rope.transform);
		fallLoopInstance = SoundManager.PlayOneShotAttached(fallEvent, base.transform);
		for (int i = 0; i < 50; i++)
		{
			if (FallOneFrame_Internal())
			{
				return;
			}
			await UniTaskExtension.WaitOneFrame();
		}
		isFalling = true;
	}

	private void SetDefaultMaterialProperty()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FadeAmount, -0.1f);
		materialPropertyBlock.SetFloat(FadeBurnTransition, 0.01f);
		materialPropertyBlock.SetFloat(FadeBurnWidth, 0.01f);
		rockMeshRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	private void PlayFallEffect()
	{
		idleParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		burnParticle.Play();
		rockBurnParticle.Play();
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FadeAmount, -0.1f);
		materialPropertyBlock.SetFloat(FadeBurnTransition, 0.01f);
		materialPropertyBlock.SetFloat(FadeBurnWidth, 0f);
		rockMeshRenderer.SetPropertyBlock(materialPropertyBlock);
		float fixedDeltaTime = Time.fixedDeltaTime;
		DOTween.Sequence().AppendInterval(fixedDeltaTime * 50f).Append(materialPropertyBlock.DOFloatPropertyBlock(FadeAmount, 0.237f, fixedDeltaTime * 15f, rockMeshRenderer))
			.Join(materialPropertyBlock.DOFloatPropertyBlock(FadeBurnTransition, 0.053f, fixedDeltaTime * 15f, rockMeshRenderer))
			.Join(materialPropertyBlock.DOFloatPropertyBlock(FadeBurnWidth, 0.107f, fixedDeltaTime * 15f, rockMeshRenderer))
			.Play();
	}

	private void FixedUpdate()
	{
		if (isFalling && CurrentState == 1)
		{
			FallOneFrame_Internal(targetGravity);
		}
	}

	private bool FallOneFrame_Internal(float gravityCoef = 1f)
	{
		velocity += Physics2D.gravity * Time.fixedDeltaTime * gravityCoef;
		velocity.y = Mathf.Max(velocity.y, 0f - terminalSpeedY);
		Vector2 vector = velocity * Time.fixedDeltaTime;
		bool flag = false;
		if (base.transform.position.y + vector.y < targetY)
		{
			flag = true;
			vector.y = targetY - base.transform.position.y;
		}
		base.transform.position += new Vector3(vector.x, vector.y, 0f);
		Attack();
		if (flag)
		{
			isFalling = false;
			velocity = Vector2.zero;
			RopeCollider.enabled = false;
			AddFallenPlatformToTilemap(base.transform.position + Vector3.down * platform.size.y / 2f, platform.size);
			if (fallLoopInstance.isValid())
			{
				fallLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				fallLoopInstance.release();
			}
			SoundManager.PlayOneShotAttached(impactEvent, base.transform);
			return true;
		}
		return false;
	}

	private void Attack()
	{
		Collider2D[] array = AttackUtility.OverlapBoxAll(base.transform.position + Vector3.down * platform.size.y, platform.size, ~(int)LayerUtility.PlayerLayer);
		if (array.IsNullOrEmpty())
		{
			return;
		}
		Collider2D[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].TryGetComponent<IDamageable>(out var component) && !damaged.Contains(component) && !(component is Enemy { IsDead: not false }))
			{
				HitInfo hitInfo = new HitInfo(this);
				if (component.TryTakeDamage(hitInfo))
				{
					damaged.Add(component);
				}
			}
		}
	}

	private void OnValidate()
	{
		SetRope();
	}

	private void OnDrawGizmosSelected()
	{
		positionInt = base.transform.position.ToVector2Int();
		base.transform.position = positionInt.ToVector3();
		DrawFallenPlatform();
	}

	private void SetRope()
	{
		Vector2 vector = new Vector2(0f, ropeSize.y / 2f);
		RopeCollider.size = ropeSize;
		rope.transform.localPosition = vector;
	}

	private void DrawFallenPlatform()
	{
		RaycastHit2D fallenHit = GetFallenHit();
		Vector2 obj = (((object)fallenHit.collider == null) ? new Vector2(base.transform.position.x, (float)Area.AreaBox.LowerLeft.y - platform.size.y / 2f) : fallenHit.centroid);
		Gizmos.color = new Color(1f, 0.75f, 0.5f, 0.5f);
		Gizmos.DrawCube(obj, platform.size);
	}

	private RaycastHit2D GetFallenHit()
	{
		Vector3 vector = base.transform.position + Vector3.down * (platform.size.y * 1.5f + 0.01f);
		Vector2 size = new Vector2(platform.size.x - 0.02f, platform.size.y);
		List<RaycastHit2D> list = new List<RaycastHit2D>();
		Physics2D.BoxCast(contactFilter: new ContactFilter2D
		{
			useLayerMask = true,
			layerMask = PlatformLayer
		}, origin: vector, size: size, angle: 0f, direction: Physics2D.gravity.normalized, results: list);
		BreakableWall component;
		return list.FirstOrDefault((RaycastHit2D hit) => !hit.transform.TryGetComponent<BreakableWall>(out component));
	}

	private void FallImmediately()
	{
		RaycastHit2D fallenHit = GetFallenHit();
		if ((object)fallenHit.collider == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		base.transform.position = fallenHit.centroid + Vector2.up * platform.size.y / 2f;
		DisableDamageables(AttackUtility.OverlapBoxAll(fallenHit.centroid, platform.size, ~(int)LayerUtility.PlayerLayer));
		RopeCollider.enabled = false;
		AddFallenPlatformToTilemap(fallenHit.centroid, platform.size);
	}

	public static void DisableDamageables(Collider2D[] colls)
	{
		foreach (Collider2D collider2D in colls)
		{
			BreakableObject component2;
			if (collider2D.TryGetComponent<Creature>(out var component))
			{
				FieldController.RemoveEnemy(component);
				component.gameObject.SetActive(value: false);
			}
			else if (collider2D.TryGetComponent<BreakableObject>(out component2))
			{
				component2.gameObject.SetActive(value: false);
			}
		}
	}

	public static void AddFallenPlatformToTilemap(Vector2 position, Vector2 size)
	{
		position = new Vector2(position.x, Mathf.RoundToInt(position.y));
		Vector2 vector = position - size / 2f;
		Vector2 vector2 = position + new Vector2(size.x, 0f - size.y) / 2f;
		List<Vector2> list = new List<Vector2>();
		for (float num = vector.x - 1f; num <= vector2.x; num += 1f)
		{
			list.Add(new Vector2(num, vector.y));
		}
		FieldController.ModifyPathGraphs(list, setGround: false, updateCreatures: false);
		Vector2 vector3 = position + new Vector2(0f - size.x, size.y) / 2f;
		Vector2 vector4 = position + new Vector2(size.x, size.y) / 2f;
		List<Vector2> list2 = new List<Vector2>();
		for (float num2 = vector3.x - 1f; num2 <= vector4.x; num2 += 1f)
		{
			list2.Add(new Vector2(num2, vector3.y));
		}
		FieldController.ModifyPathGraphs(list2, setGround: true);
	}

	public void OnStartRide()
	{
	}

	public void OnEndRide()
	{
	}
}
