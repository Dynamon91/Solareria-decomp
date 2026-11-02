using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR;

public class PlayerOverheatWave : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private EventReference loopEvent;

	[SerializeField]
	private EventReference endEvent;

	private BoxCollider2D coll;

	private ParticleSystem particle;

	private static List<IDamageable> damageables;

	private ContactFilter2D filter;

	private float maxDistance;

	private float movedDistance;

	private float speed;

	private int durationFrame;

	private int cycleFrame;

	private float burnDamage;

	private float burnFireDamage;

	private bool shouldDestroy;

	private HashSet<PathFinder.Node> connectedNodes;

	private PathFinder.Node leftEndNode;

	private PathFinder.Node rightEndNode;

	private float moveLeftEnd;

	private float moveRightEnd;

	private EventInstance loopInstance;

	private const string SkillDataId = "overheat_shockwave";

	private const string BurnDataID = "overheat_shockwave_burn";

	public string Name => "Player_Overheat_Wave";

	public string PrefabKey => "Player Overheat Wave.prefab";

	public GameObject GameObject => base.gameObject;

	public Direction FacingDirection => Direction.None;

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	private ParticleSystem SlashParticle => particle ?? (particle = base.transform.GetChild(0).GetComponent<ParticleSystem>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void Awake()
	{
		filter = new ContactFilter2D
		{
			useTriggers = true,
			useLayerMask = true,
			layerMask = ((int)LayerUtility.MonsterLayer | (int)LayerUtility.ObjectLayer | (int)LayerUtility.DefaultLayer)
		};
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		damageables = null;
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(int direction, float distance, Vector2 size)
	{
		(float, float) damageCoefficient = Game.Instance.Player.PlayerStat.GetDamageCoefficient("overheat_shockwave_burn", 0);
		burnDamage = damageCoefficient.Item1;
		burnFireDamage = damageCoefficient.Item2;
		PlayerBurnData playerBurnData = Game.DataManager.GetPlayerBurnData("overheat_shockwave_burn");
		durationFrame = playerBurnData.Duration;
		cycleFrame = playerBurnData.CycleFrame;
		Coll.size = size;
		Coll.offset = new Vector2(0f, size.y / 2f);
		Vector3 localScale = base.transform.localScale;
		localScale.x = direction;
		base.transform.localScale = localScale;
		if (damageables == null)
		{
			damageables = new List<IDamageable>();
		}
		maxDistance = distance;
		movedDistance = 0f;
		speed = 0f;
		shouldDestroy = false;
		SetConnectedPath();
		base.gameObject.SetActive(value: true);
	}

	private void SetConnectedPath()
	{
		Vector3 position = base.transform.position;
		PathFinder.Node currentNode = PathFinder.FindNode(position);
		if (SPR.Field.Platform.IgnoreBake.Contains(SceneManager.GetActiveScene().name))
		{
			bool num = base.transform.localScale.x > 0f;
			Vector2 collidePosition = PhysicsUtility.GetCollidePosition(num ? Vector2.right : Vector2.left, base.transform.position + Vector3.up * 2f, coll.size, maxDistance);
			int xMin = Mathf.CeilToInt(num ? (position.x - maxDistance) : collidePosition.x);
			int xMax = Mathf.FloorToInt(num ? collidePosition.x : (position.x + maxDistance));
			Game.Instance.FieldController.Platform.PathFinder.BakeGraph(xMin, xMax, Mathf.CeilToInt(position.y - maxDistance), Mathf.FloorToInt(position.y + maxDistance));
			PathFinder.UpdateConnectedPath();
			currentNode = PathFinder.FindNode(position);
		}
		connectedNodes = PathFinder.ConnectedNodeList.Find((HashSet<PathFinder.Node> x) => x.Contains(currentNode));
		if (connectedNodes != null)
		{
			float minX = connectedNodes.Min((PathFinder.Node n) => n.Position.x);
			float maxX = connectedNodes.Max((PathFinder.Node n) => n.Position.x);
			leftEndNode = connectedNodes.First((PathFinder.Node n) => Mathf.Approximately(minX, n.Position.x));
			rightEndNode = connectedNodes.First((PathFinder.Node n) => Mathf.Approximately(maxX, n.Position.x));
			moveLeftEnd = Mathf.Max(minX, position.x - maxDistance);
			moveRightEnd = Mathf.Min(maxX, position.x + maxDistance);
		}
	}

	public async UniTask ShootAsync(float speed)
	{
		this.speed = speed;
		loopInstance = SoundManager.PlayOneShotAttached(loopEvent, base.transform);
		await UniTask.CompletedTask;
	}

	private void FixedUpdate()
	{
		if (speed <= 0f || shouldDestroy)
		{
			return;
		}
		Vector3 position = base.transform.position;
		Move();
		if (!shouldDestroy && Mathf.Approximately(base.transform.position.x, position.x))
		{
			SetShouldDestroy();
		}
		AttackUtility.OverlapCollider(Coll, filter, out var results);
		foreach (Collider2D item in results)
		{
			if (item.TryGetComponent<IDamageable>(out var component))
			{
				OnContactDamageable(component);
			}
		}
	}

	private void LateUpdate()
	{
		if (shouldDestroy && !SlashParticle.isPlaying)
		{
			ReturnToPool();
		}
	}

	private void Move()
	{
		int sign = base.transform.localScale.x.GetSign();
		PathFinder.Node node = PathFinder.FindNode(base.transform.position);
		if (node == null)
		{
			DebugLog.GeneralLogError($"{Game.FieldID}, {base.transform.position} 노드를 찾을 수 없어 발화를 취소합니다.");
			SetShouldDestroy();
			return;
		}
		PathFinder.Node node2 = node.Shift(sign, Mathf.RoundToInt(speed * Time.fixedDeltaTime), (sign < 0) ? leftEndNode : rightEndNode);
		if (node2 == null)
		{
			SetShouldDestroy();
			return;
		}
		Vector2 position = node2.Position;
		position.x = Mathf.Clamp(position.x, moveLeftEnd, moveRightEnd);
		base.transform.position = Vector2.MoveTowards(base.transform.position, position, speed * Time.fixedDeltaTime);
		movedDistance += speed * Time.fixedDeltaTime;
		if (movedDistance >= maxDistance)
		{
			SetShouldDestroy();
		}
	}

	private void OnContactDamageable(IDamageable damageable)
	{
		if (!Game.Instance.Player.mover.Attack(new PlayerMover.AttackParameter(damageable, "overheat_shockwave", 0, AttackAttribute.Default | AttackAttribute.BlockStuck, this), damageables))
		{
			return;
		}
		if (!(damageable is Enemy enemy))
		{
			if (damageable is DynamicHitBox dynamicHitBox)
			{
				dynamicHitBox.Enemy.StartBurning(durationFrame, cycleFrame, burnDamage, burnFireDamage);
			}
		}
		else
		{
			enemy.StartBurning(durationFrame, cycleFrame, burnDamage, burnFireDamage);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(Coll.bounds.center, Coll.size);
	}

	private void SetShouldDestroy()
	{
		shouldDestroy = true;
		if (loopInstance.isValid())
		{
			loopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			loopInstance.release();
		}
		SoundManager.PlayOneShotAttached(endEvent, base.transform);
	}
}
