using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM;
using SPR.FSM.Base;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using Spine;
using UnityEngine;

namespace SPR;

public abstract class Creature : Enemy
{
	[Header("리스폰 여부")]
	[SerializeField]
	private bool canNotRespawn;

	[Header("Other")]
	[SerializeField]
	private CreatureMass mass;

	[Header("Creature Parameters")]
	[SerializeField]
	private int detectPathWidth;

	[SerializeField]
	protected float walkFrontSpeed;

	[SerializeField]
	private float walkBackSpeed;

	[SerializeField]
	protected int attackWaitMinFrame;

	[SerializeField]
	protected int attackWaitMaxFrame;

	[SerializeField]
	protected int patrolRange;

	[SerializeField]
	protected int patrolWaitMinFrame;

	[SerializeField]
	protected int patrolWaitMaxFrame;

	[SerializeField]
	protected int alertMinFrame;

	[SerializeField]
	protected int alertMaxFrame;

	[Header("시야")]
	[SerializeField]
	protected Zone patrolSightArea;

	[Header("SFX")]
	[SerializeField]
	private SoundManager.CreatureMaterialType materialType;

	[SerializeField]
	private EventReference dieEvent;

	[Header("매복")]
	[SerializeField]
	[Tooltip("매복 시야")]
	protected Zone ambushSightArea;

	[SerializeField]
	private GenericAttackSequence<Creature> ambushAttack;

	protected CreatureCanvas CreatureCanvas;

	private const float CliffCheckDistance = 1f;

	private ContactFilter2D groundFilter;

	private string fixedDropItem;

	private HashSet<PathFinder.Node> connectedNodes = new HashSet<PathFinder.Node>();

	private List<PathFinder.Node> connectedNodeList;

	private bool waitForDeath;

	protected readonly Dictionary<AttackSequence, Func<List<AttackSequence>>> AttackPatterns = new Dictionary<AttackSequence, Func<List<AttackSequence>>>();

	private AttackSequence prevAttack;

	protected readonly Dictionary<AttackSequence, float> AttackChance = new Dictionary<AttackSequence, float>();

	protected bool UseDeathFire = true;

	protected bool DisableItemDrop;

	private BurstIcon burstIcon;

	public float SpeedScale { get; set; } = 1f;

	public override EnemyState DefaultState => PatrolState;

	public Vector2 SpawnPos => base.SpawnPosition;

	public override HitType HitType => HitType.Creature;

	public virtual bool CanMove
	{
		get
		{
			if (!(walkFrontSpeed > 0f))
			{
				return walkBackSpeed > 0f;
			}
			return true;
		}
	}

	public bool OnlyWalkFront => Mathf.Approximately(walkBackSpeed, 0f);

	private static EnemyFightData EnemyFightData => Game.Instance.SaveData.EnemyFightData;

	public PatrolState PatrolState { get; protected set; }

	public ChaseState ChaseState { get; protected set; }

	public AttackWaitState AttackWaitState { get; protected set; }

	public AttackState AttackState { get; protected set; }

	public AlertState AlertState { get; protected set; }

	public DeathState DeathState { get; protected set; }

	public AmbushState AmbushState { get; protected set; }

	public virtual string IdleAnim => "idle";

	public virtual string WalkFrontAnim => "walk";

	public virtual string WalkBackAnim => "walk_back";

	public float MoveForwardDelta => walkFrontSpeed * Time.fixedDeltaTime;

	public List<PathFinder.Node> PathDebug { get; set; } = new List<PathFinder.Node>();

	public float MoveForwardSpeed => walkFrontSpeed;

	public float MoveBackSpeed => walkBackSpeed;

	public PathFinder.Node ChaseNode { get; private set; }

	public IReadOnlyList<PathFinder.Node> PathNodesList => connectedNodeList;

	protected HashSet<PathFinder.Node> ConnectedNodes
	{
		get
		{
			return connectedNodes;
		}
		private set
		{
			connectedNodes = value;
			if (connectedNodes != null)
			{
				connectedNodeList = connectedNodes.ToList();
			}
		}
	}

	public int DetectPathWidth => detectPathWidth;

	protected float MoveLeftEnd { get; private set; }

	protected float MoveRightEnd { get; private set; }

	protected virtual bool UseDeathFireVFX => true;

	public bool IsAmbushCreature { get; set; }

	protected virtual Vector2 BurstIconPosition => VisualCenter + Vector2.up * Bounds.size.y / 2f;

	protected override void Awake()
	{
		base.Awake();
		if (!(this is HighCreature))
		{
			CreateCreatureCanvas().Forget();
		}
		PatrolState = new PatrolState(this, patrolWaitMinFrame, patrolWaitMaxFrame);
		ChaseState = new ChaseState(this);
		AttackWaitState = new AttackWaitState(this, attackWaitMinFrame, attackWaitMaxFrame);
		AlertState = new AlertState(this, alertMinFrame, alertMaxFrame);
		DeathState = new DeathState(this, OnEnterDeathState);
		AmbushState = new AmbushState(this);
		groundFilter = new ContactFilter2D
		{
			layerMask = ((int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer),
			useLayerMask = true,
			useTriggers = true
		};
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.green;
		Vector2 wallRayStartPosition = GetWallRayStartPosition(front: true);
		Gizmos.DrawLine(to: wallRayStartPosition + Vector2.down * 1f, from: wallRayStartPosition);
		Vector2 wallRayStartPosition2 = GetWallRayStartPosition(front: false);
		Gizmos.DrawLine(to: wallRayStartPosition2 + Vector2.down * 1f, from: wallRayStartPosition2);
		Gizmos.color = Color.yellow;
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
		Gizmos.color = Color.cyan;
		Zone zoneOf2 = ambushSightArea.GetZoneOf(this);
		Gizmos.DrawWireCube(zoneOf2.Center, zoneOf2.Size);
		foreach (PathFinder.Node connectedNode in ConnectedNodes)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawCube(connectedNode.Position, new Vector3(0.5f, 0.5f));
		}
	}

	public override void Activate()
	{
		if (base.gameObject.activeSelf)
		{
			SetConnectedPath();
			if (!IsAmbushCreature)
			{
				base.StateMachine.ChangeState(DefaultState);
			}
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		waitForDeath = false;
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	public virtual List<AttackSequence> GetAttackSequences()
	{
		ChaseNode = null;
		if (prevAttack != null && AttackChance.ContainsKey(prevAttack))
		{
			AttackChance[prevAttack] = Mathf.Max(0f, AttackChance[prevAttack] - (float)PlayerControlParameter.Instance.creatureRepeatPenalty);
		}
		InitiateChance();
		AttackSequence key;
		foreach (KeyValuePair<AttackSequence, Func<List<AttackSequence>>> attackPattern in AttackPatterns)
		{
			attackPattern.Deconstruct(out key, out var _);
			AttackSequence attackSequence = key;
			int sign = (Enemy.Player.GetPositionFromGroud().x - VisualBottom.x).GetSign();
			PathFinder.Node node = PathFinder.FindNode(Enemy.Player.GetPositionFromGroud());
			PathFinder.Node node2 = PathFinder.FindNode(base.Position);
			if (node != null && ConnectedNodes.Contains(node))
			{
				if (!attackSequence.GetSightZone().GetZoneOf(node.Position + visualCenterPivot, sign).Contains(Enemy.Player.ColliderBounds))
				{
					AttackChance[attackSequence] = 0f;
				}
				continue;
			}
			if (attackSequence.CheckPlatform)
			{
				AttackChance[attackSequence] = 0f;
				continue;
			}
			ChaseNode = null;
			node2 = node2.Shift(sign, Mathf.RoundToInt(Mathf.Abs(Enemy.Player.Position.x - base.Position.x)));
			if (attackSequence.GetSightZone().GetZoneOf(node2.Position + visualCenterPivot, sign).Contains(Enemy.Player.ColliderBounds))
			{
				ChaseNode = node2;
				break;
			}
			if (ChaseNode == null)
			{
				AttackChance[attackSequence] = 0f;
			}
		}
		if ((float)Mathf.FloorToInt(AttackChance.Sum((KeyValuePair<AttackSequence, float> x) => x.Value)) <= 0f)
		{
			return null;
		}
		int num = CryptoRandom.Range(1, Mathf.FloorToInt(AttackChance.Sum((KeyValuePair<AttackSequence, float> x) => x.Value)));
		float num2 = 0f;
		foreach (KeyValuePair<AttackSequence, float> item in AttackChance)
		{
			item.Deconstruct(out key, out var value2);
			AttackSequence key2 = key;
			float num3 = value2;
			num2 += num3;
			if ((float)num <= num2)
			{
				prevAttack = key2;
				return AttackPatterns[key2]?.Invoke();
			}
		}
		return null;
	}

	protected virtual void InitiateChance()
	{
		float value = 100f / (float)AttackPatterns.Count;
		foreach (var (key, _) in AttackPatterns)
		{
			AttackChance.TryAdd(key, value);
		}
	}

	public virtual void ResetChance()
	{
		float value = 100f / (float)AttackPatterns.Count;
		foreach (var (key, _) in AttackPatterns)
		{
			AttackChance[key] = value;
		}
	}

	public void SetConnectedPath()
	{
		if (this is Mucon)
		{
			return;
		}
		base.SpawnPosition = base.Position;
		PathFinder.Node currentNode = PathFinder.FindNode(base.transform.position);
		ConnectedNodes = PathFinder.ConnectedNodeList.Find((HashSet<PathFinder.Node> x) => x.Contains(currentNode));
		if (ConnectedNodes != null)
		{
			MoveLeftEnd = ConnectedNodes.Min((PathFinder.Node n) => n.Position.x) - (float)detectPathWidth / 2f;
			MoveRightEnd = ConnectedNodes.Max((PathFinder.Node n) => n.Position.x) - (float)detectPathWidth / 2f;
		}
	}

	public bool CanReach(Vector2 target)
	{
		PathFinder.Node item = PathFinder.FindNode(target);
		return ConnectedNodes.Contains(item);
	}

	public virtual bool ShouldChangeToAttackState()
	{
		return IsPlayerInAnyAttackBound();
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		int sign = (moveForward ? FacingDirection.GetSignX() : (-FacingDirection.GetSignX()));
		PathFinder.Node node = PathFinder.FindNode(base.transform.position);
		PathFinder.Node node2 = node.Shift(sign, Mathf.RoundToInt(velocityX * deltaTime));
		PathFinder.Node node3 = node.Shift(sign, detectPathWidth / 2, allowNull: true);
		if (node2 != null && node3 != null)
		{
			Vector2 position = node2.Position;
			position.x = Mathf.Clamp(position.x, MoveLeftEnd, MoveRightEnd);
			base.transform.position = Vector2.MoveTowards(base.transform.position, position, velocityX * deltaTime);
		}
	}

	public void Move(Vector2 targetPos)
	{
		base.transform.position = Vector3.MoveTowards(base.transform.position, targetPos, walkFrontSpeed * Time.fixedDeltaTime);
	}

	public virtual bool IsPlayerInAnyAttackBound()
	{
		return base.AllAttackSequences.Any(base.IsPlayerInAttackBound);
	}

	public virtual bool IsPlayerInSightBound()
	{
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		if (!IsPlayerInBound(zoneOf.Center, zoneOf.Size))
		{
			return false;
		}
		Vector2 normalized = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		float distance = Mathf.Min((Enemy.Player.VisualCenter - VisualCenter).magnitude, patrolSightArea.Size.x / 2f);
		return (object)Physics2D.Raycast(VisualCenter, normalized, distance, LayerUtility.PlatformLayer).collider == null;
	}

	public void SetPatrolSight(Zone zone)
	{
		patrolSightArea = zone;
	}

	protected override bool IsPlayerInBound(Vector2 center, Vector2 size, bool checkPlatform = false)
	{
		if (checkPlatform)
		{
			if (base.IsPlayerInBound(center, size, checkPlatform: true))
			{
				return ConnectedNodes.Contains(PathFinder.FindNode(Enemy.Player.GetPositionFromGroud()));
			}
			return false;
		}
		return base.IsPlayerInBound(center, size);
	}

	public virtual Vector2 GetPatrolDestination()
	{
		if (ConnectedNodes == null)
		{
			return base.transform.position;
		}
		PathFinder.Node node = PathFinder.FindNode(base.SpawnPosition);
		PathFinder.Node node2 = node.ShiftEnd(-1).Shift(1, detectPathWidth / 2);
		PathFinder.Node node3 = node.ShiftEnd(1).Shift(-1, detectPathWidth / 2);
		int count = CryptoRandom.Range(0, patrolRange);
		int num = ((!CryptoRandom.RandomBool()) ? 1 : (-1));
		Vector2 position = node.Shift(num, count).Position;
		switch (num)
		{
		case -1:
			if (position.x < node2.Position.x)
			{
				return node2.Position;
			}
			break;
		case 1:
			if (position.x > node3.Position.x)
			{
				return node3.Position;
			}
			break;
		}
		return position;
	}

	public void ChangeVisualBottomPositionTo(Vector2 targetPos)
	{
		Vector2 vector = targetPos - VisualBottom;
		base.transform.position += (Vector3)vector;
	}

	public void Resurrect()
	{
		ResetStatus();
		base.Exp = 0;
		base.Money = 0;
		SetColorAsync(Color.white, 0f, 0).Forget();
		Game.Instance.FieldController.AddEnemy(this);
		base.StateMachine.ChangeState(DefaultState);
		HitBox.enabled = true;
	}

	public void TryAmbushAttack()
	{
		Zone zoneOf = ambushSightArea.GetZoneOf(this);
		if (new Bounds(zoneOf.Center, zoneOf.Size).Intersects(Enemy.Player.ColliderBounds))
		{
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { ambushAttack }, forceAttack: true));
		}
	}

	protected virtual void OnEnterDeathState()
	{
		RemoveAnimationEvent(AnimState.Complete, OnLandingComplete);
	}

	protected override void OnChangeHealth(int delta)
	{
		if (!waitForDeath)
		{
			CreatureCanvas.UpdateHealthBar(base.Health, base.MaxHealth, delta);
			CreatureCanvas.UpdateStunPoint(base.CurrentStun);
		}
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		base.OnDamaged(hitInfo);
		VisualKnockBack(hitInfo.GetDirection(VisualBottom));
		Game.Instance.SoundManager.PlayOneShotAttached("event:/Creature/SFX_Creature_Hit", base.transform).setParameterByName("Creature_Material", (float)materialType);
	}

	protected sealed override void Die()
	{
		if (!waitForDeath)
		{
			WaitForPlayerState().Forget();
		}
	}

	protected virtual void OnDie()
	{
		if (canNotRespawn)
		{
			Game.Instance.SaveData.WorldData.AddEliminatedCreature(Game.Instance.CurrentFieldID, base.PlaceholderPosition);
		}
		if (IsAmbushCreature)
		{
			Game.Instance.SaveData.WorldData.AddEliminatedAmbushCreature(Game.Instance.CurrentFieldID, base.PlaceholderPosition);
		}
		DropItem();
	}

	protected void AddAttack(AttackSequence key, Func<List<AttackSequence>> value)
	{
		AttackPatterns.TryAdd(key, value);
	}

	protected void RemoveAttack(AttackSequence key)
	{
		AttackPatterns.Remove(key);
		AttackChance.Remove(key);
	}

	private async UniTaskVoid WaitForPlayerState()
	{
		base.StateMachine.ChangeState(null);
		waitForDeath = true;
		base.Die();
		if (UseDeathFireVFX)
		{
			PlayDeathBurn();
			DeathFire().Forget();
		}
		base.StateMachine.ChangeState(DeathState);
		SetColorAsync(Color.black, 0.9f, 60).Forget();
		if (this is HighCreature)
		{
			Game.Instance.UIManager.FieldCanvas.BossBoard.HideAsync(0.6f, deactivate: true).Forget();
			Enemy.SoundManager.StopLegendaryCreatureBGM(GetComponentInParent<Area>().SoundKey);
		}
		else
		{
			CreatureCanvas.SetActive(value: false);
		}
		Game.Instance.RuntimeData.AddDieCreature(Game.Instance.CurrentFieldID, base.PlaceholderPosition);
		EnemyFightData.AddCreatureDeath(Name);
		Game.Instance.UIManager.MenuCanvas.MemoryPanel.UpdateMemoryCreature(Name);
		Enemy.SoundManager.PlayOneShotAttached(dieEvent, base.transform);
		SpreadMoney();
		OnDie();
		CorpseRotate();
		await UniTask.WaitUntil(() => !Enemy.Player.IsSpecialAttackTaskValid());
		HitBox.enabled = false;
		waitForDeath = false;
	}

	private Vector2 GetWallRayStartPosition(bool front)
	{
		int num = (front ? 1 : (-1));
		return VisualCenter + new Vector2((float)(num * FacingDirection.GetSignX() * detectPathWidth) / 2f, 0f);
	}

	public override void ActivateBurstIcon(bool value, bool isClicked = false)
	{
		if (value)
		{
			if (!isClicked)
			{
				if ((object)burstIcon == null)
				{
					burstIcon = Game.Instance.ObjectPoolManager.GetObject<BurstIcon>("Burst Icon.prefab", BurstIconPosition);
				}
				burstIcon.Highlight();
			}
			else if (!burstIcon.IsNull())
			{
				burstIcon.Disable().Forget();
				burstIcon = null;
			}
		}
		else
		{
			burstIcon?.ReturnToPool();
			burstIcon = null;
		}
	}

	public virtual void PlayHealTween(bool loop, Type subscriberType = null)
	{
		if ((object)subscriberType == null)
		{
			subscriberType = GetType();
		}
		CreatureCanvas.PlayHealTween(loop, subscriberType);
	}

	public virtual void StopHealTween(Type subscriberType)
	{
		CreatureCanvas.StopHealTween(subscriberType);
	}

	protected override void ResetStatus()
	{
		base.ResetStatus();
		CreatureCanvas.Initialize(Bounds.size.y, base.MaxStunCount);
	}

	public async UniTask KnockBackAsync(int frame, Vector2 direction, bool isDash = false, int power = 0)
	{
		float tMax = (float)frame * Time.fixedDeltaTime;
		if (power == 0)
		{
			PowerByMass powerByMass = PlayerControlParameter.Instance.creatureMassKnockBackPowers.FirstOrDefault((PowerByMass powerByMass2) => powerByMass2.Mass == mass);
			power = (isDash ? powerByMass.DashKnockBackPower : powerByMass.KnockBackPower);
		}
		if (power != 0)
		{
			float gravity = Mathf.Abs(Physics2D.gravity.y) * PlayerControlParameter.Instance.OriginGravity;
			Vector2 vector = direction * power;
			Vector2 velocity = PhysicsUtility.GetParabolaInitialVelocityWithJerk(vector.x, vector.y, tMax, -200f, gravity);
			ApplyVelocity(velocity);
			int dir = velocity.x.GetSign();
			for (int i = 1; i <= frame; i++)
			{
				await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
				float x = velocity.x;
				float num = (float)i * Time.fixedDeltaTime * -200f;
				float num2 = x - (float)dir * num * Time.fixedDeltaTime;
				x = ((num2.GetSign() != dir) ? 0f : num2);
				velocity.x = x;
				ApplyVelocity(velocity);
			}
		}
		void ApplyVelocity(Vector2 vector3)
		{
			Vector2 vector2 = vector3 * Time.fixedDeltaTime;
			Vector2 vector4 = base.transform.position;
			vector4 += vector2;
			Vector2 vector5 = vector4 + vector3;
			Vector2 position = ConnectedNodes.First().Position;
			Vector2 position2 = ConnectedNodes.Last().Position;
			if (vector5.x > position.x && vector5.x < position2.x)
			{
				base.transform.position = vector4;
			}
		}
	}

	public virtual void OnLandingComplete(TrackEntry track)
	{
		if (!IsDead && track.Animation.Name == "landing")
		{
			base.enabled = true;
			SetConnectedPath();
			base.StateMachine.ChangeState(DefaultState);
		}
	}

	public void SetDiableItemDrop(bool value)
	{
		DisableItemDrop = value;
	}

	protected virtual async UniTaskVoid CreateCreatureCanvas()
	{
		CreatureCanvas = await AddressableHelper.InstantiateAsync<CreatureCanvas>("Creature Canvas.prefab", base.transform);
		CreatureCanvas.Initialize(Bounds.size.y, base.MaxStunCount);
	}

	private async UniTaskVoid DeathFire()
	{
		PlayDeathFire();
		await UniTask.WaitUntil(() => IsAnimComplete(0, "death"), PlayerLoopTiming.Update, DeathFireTokenSource.Token);
		await UniTask.WaitForSeconds(1f, ignoreTimeScale: false, PlayerLoopTiming.Update, DeathFireTokenSource.Token);
		await StopDeathFireAsync();
	}

	public override void AddStunCount(int value)
	{
		base.AddStunCount(value);
		if (this is HighCreature)
		{
			Game.Instance.UIManager.FieldCanvas.BossBoard.UpdateStunPoint(base.CurrentStun);
		}
		else
		{
			CreatureCanvas.UpdateStunPoint(base.CurrentStun);
		}
	}

	public override void ResetStunCount()
	{
		base.ResetStunCount();
		CreatureCanvas.ResetStunPoint();
	}

	private void PlayDeathBurn()
	{
		if (UseDeathFire)
		{
			PoolableTimeLimitObject poolableTimeLimitObject = Enemy.PoolManager.GetObject<PoolableTimeLimitObject>("Death Burn.prefab", VisualCenter);
			poolableTimeLimitObject.Initialize("Death Burn.prefab", 1.5f);
			ParticleSystem.ShapeModule shape = poolableTimeLimitObject.GetComponent<ParticleSystem>().shape;
			shape.meshRenderer = MeshRenderer;
		}
	}

	public void SetFixedDropItemName(string itemName)
	{
		fixedDropItem = itemName;
	}

	private void DropItem()
	{
		if (DisableItemDrop)
		{
			return;
		}
		string randomResult = fixedDropItem;
		if (randomResult.IsNullOrEmpty())
		{
			EnemyDropData enemyDropDataByClearCount = DataManager.Instance.GetEnemyDropDataByClearCount(Game.Instance.SaveData.ClearCount, Name);
			if (!CryptoRandom.PercentSuccess(enemyDropDataByClearCount.Rate))
			{
				return;
			}
			randomResult = Util.GetRandomResult(enemyDropDataByClearCount);
		}
		if (!string.IsNullOrEmpty(randomResult))
		{
			if (DataManager.Instance.GetItemData(randomResult) == null)
			{
				DebugLog.GeneralLogError(randomResult + "에 해당하는 아이템 데이터가 존재하지 않습니다!");
				return;
			}
			Vector2 visualCenter = VisualCenter;
			Vector2Int dropItemPosition = GetDropItemPosition();
			ItemObject.DropItemAsync(randomResult, visualCenter, dropItemPosition).Forget();
		}
	}

	private Vector2Int GetDropItemPosition()
	{
		float x = VisualBottom.x + (float)((!CryptoRandom.RandomBool()) ? 1 : (-1)) * base.HitBoxSize.x / 2f;
		List<RaycastHit2D> list = new List<RaycastHit2D>();
		Physics2D.BoxCast(new Vector2(x, VisualBottom.y + 1f), Vector2.one, 0f, Vector2.down, groundFilter, list);
		float y = ((list.Count > 0) ? (list.First().point.y + 1f) : (-999f));
		return new Vector2(x, y).ToVector2Int();
	}

	protected virtual void CorpseRotate()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(VisualCenter, Vector2.down, float.PositiveInfinity, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		if (!(Vector2.up == raycastHit2D.normal))
		{
			float num = Vector2.Angle(Vector2.up, raycastHit2D.normal);
			num = ((raycastHit2D.normal.x.GetSign() == -1) ? num : (0f - num));
			base.transform.DORotate(new Vector3(0f, 0f, num), GetAnimDuration("death")).SetEase(Ease.Linear);
		}
	}
}
