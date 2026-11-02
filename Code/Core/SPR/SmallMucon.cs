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
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class SmallMucon : FlyingCreature, IPoolableObject
{
	public const string Address = "Small Mucon.prefab";

	[Header("퍼짐 설정")]
	[SerializeField]
	private MinMax xMinMax;

	[SerializeField]
	private MinMax yMinMax;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<SmallMucon> selfDestruct;

	[Header("References")]
	[SerializeField]
	private ParticleSystem trail;

	[SerializeField]
	private EventReference spawnEvent;

	private Vector3 chargeDirection;

	public override string Name => "SmallMucon";

	public override EnemyState DefaultState => base.PatrolState;

	public override string MoveAnimation => "idle";

	public string PrefabKey => "Small Mucon.prefab";

	public GameObject GameObject => base.gameObject;

	public void InitializeBeforeUse()
	{
		SetAnimation("idle");
		SetColorAsync(Color.white, 0f, 0).Forget();
		ResetStatus();
		UseDeathFire = false;
		HitBox.enabled = true;
		base.gameObject.SetActive(value: true);
		trail.Play(withChildren: true);
	}

	public void ReturnToPool()
	{
		Enemy.PoolManager.Return(this);
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		UseDeathFire = false;
		base.BounceAnim = "parry";
		AddAction(ActionTiming.Begin, "ChargeToPlayer", (Action<int, Parameter[]>)delegate
		{
			chargeDirection = (Enemy.Player.VisualCenter - VisualCenter).normalized;
		});
		AddAction(ActionTiming.OnFrame, "ChargeToPlayer", delegate(int _, Parameter[] p)
		{
			ChargeToPlayer(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "Destruct", Die);
		AddAction(ActionTiming.OnFrame, "ParryOverlap", ParryOverlap);
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		base.transform.position += new Vector3(velocityX * (float)(moveForward ? FacingDirection.GetSignX() : (-FacingDirection.GetSignX())), 0f, 0f) * deltaTime;
	}

	public override void OnEncounterPlayer()
	{
		base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { selfDestruct }, forceAttack: true));
	}

	public void Spread()
	{
		InitializeOnInstantiate(Mathf.Abs(Game.Instance.FieldController.Enemies.Count()) + 1, base.transform.position);
		base.Exp = 0;
		base.Money = 0;
		int num = CryptoRandom.Range((int)xMinMax.min, (int)xMinMax.max);
		int num2 = CryptoRandom.Range((int)yMinMax.min, (int)yMinMax.max);
		num = (CryptoRandom.RandomBool() ? num : (-num));
		AreaBox areaBox = Game.Instance.FieldController.MainArea.AreaBox;
		Vector2 vector = VisualCenter + new Vector2(num, num2);
		vector = new Vector2(Mathf.Clamp(vector.x, (float)areaBox.LowerLeft.x + 5f, (float)areaBox.UpperRight.x - 5f), Mathf.Clamp(vector.y, (float)areaBox.LowerLeft.y + 5f, (float)areaBox.UpperRight.y - 5f));
		Game.Instance.FieldController.AddEnemy(this);
		base.StateMachine.ChangeState(null);
		Vector2 normalized = (vector - base.Position).normalized;
		float distance = Vector2.Distance(base.Position, vector);
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.Position, normalized, distance, LayerUtility.PlatformLayer);
		Vector2 vector2 = raycastHit2D.point + -normalized * 10f;
		vector2 = new Vector2(Mathf.Clamp(vector2.x, (float)areaBox.LowerLeft.x + 5f, (float)areaBox.UpperRight.x - 5f), Mathf.Clamp(vector2.y, (float)areaBox.LowerLeft.y + 5f, (float)areaBox.UpperRight.y - 5f));
		base.transform.DOMove(raycastHit2D ? vector2 : vector, 1f).OnComplete(delegate
		{
			base.SpawnPosition = base.Position;
			base.StateMachine.ChangeState(base.PatrolState);
		});
		Enemy.SoundManager.PlayOneShotAttached(spawnEvent, base.transform);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { selfDestruct };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { selfDestruct };
	}

	protected override void OnDie()
	{
		base.OnDie();
		Enemy.PoolManager.GetObject<PoolingParticle>("SmallMucon_Explosion.prefab", VisualCenter).Play();
		ForceStopDeathFire();
		trail.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		ReturnToPoolAsync().Forget();
	}

	private void ChargeToPlayer(float speed)
	{
		base.transform.position += chargeDirection * (speed * Time.fixedDeltaTime);
		List<IDamageable> list = new List<IDamageable>();
		HitInfo hitInfo = new HitInfo(this);
		if (AttackUtility.AttackBox(VisualCenter, HitBox.size, hitInfo, (int)LayerUtility.PlatformLayer | (int)LayerUtility.PlayerLayer, list).Contains(Enemy.Player) && Enemy.Player.CanParry(hitInfo))
		{
			base.StateMachine.ChangeState(new EnemyBounceState(this, 60));
		}
		else if (list.Count > 0)
		{
			Die();
		}
	}

	private void ParryOverlap()
	{
		HitInfo hitInfo = new HitInfo(this, "Parry");
		Collider2D[] array = AttackUtility.OverlapBoxAll(VisualCenter, HitBox.size, (int)LayerUtility.MonsterLayer | (int)LayerUtility.PlatformLayer);
		foreach (Collider2D collider2D in array)
		{
			if (collider2D.TryGetComponent<Enemy>(out var component) && component != this)
			{
				component.TryTakeDamage(hitInfo);
				Die();
				UnityEngine.Object.Destroy(base.gameObject);
				break;
			}
			if (collider2D.TryGetComponent<SPR.Field.Platform>(out var _))
			{
				Die();
				UnityEngine.Object.Destroy(base.gameObject);
				break;
			}
		}
	}

	private async UniTaskVoid ReturnToPoolAsync()
	{
		await UniTask.WaitForSeconds(1f);
		ReturnToPool();
	}
}
