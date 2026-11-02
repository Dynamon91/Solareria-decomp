using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class MyosoCombat : Character
{
	[SerializeField]
	private Vector2 spearThrowOffset;

	[SerializeField]
	private float spearDestinationOffset;

	private BoxCollider2D box2D;

	private EnemyPierceState pierceState;

	public override string Name => "Myoso";

	public int PauseFrame { get; set; }

	public int DefenseReduction { get; set; }

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public int Width => Mathf.CeilToInt(Box2D.size.x);

	private static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (pierceState != null)
		{
			LookAt(pierceState.Target.VisualBottom);
		}
	}

	public async UniTaskVoid Appear(EnemyPierceState state)
	{
		pierceState = state;
		base.gameObject.SetActive(value: true);
		string animationName = "attack_boss_2";
		SetAnimationInstantly(animationName, loop: false);
		EnableVFX(animationName);
		SetAnimation("acc_spin", loop: true, 1f, 1);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(205);
		base.gameObject.SetActive(value: false);
	}

	public void ThrowSpear(int fadeDuration)
	{
		if (pierceState.Target is Baran)
		{
			ThrowSpearToBaran(fadeDuration).Forget();
		}
		else
		{
			ThrowSpearAnyCreature(fadeDuration).Forget();
		}
	}

	private async UniTaskVoid ThrowSpearToBaran(int fadeDuration)
	{
		Baran baran = pierceState.Target.CastObject<Baran>();
		LookAt(baran.Position);
		Vector2 spearSpawnPos = base.transform.position + new Vector3(spearThrowOffset.x * (float)FacingDirection.GetSignX(), spearThrowOffset.y);
		Vector2 targetPos = baran.GetBoneWorldPosition("Armor_Center_CT2");
		MyosoSpear spear = PoolManager.GetObject<MyosoSpear>("Myoso_Spear.prefab", spearSpawnPos);
		Vector2 normalized = (targetPos - spearSpawnPos).normalized;
		spear.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, normalized));
		await spear.Cast(fadeDuration);
		targetPos = baran.GetBoneWorldPosition("Armor_Center_CT2");
		normalized = (targetPos - spearSpawnPos).normalized;
		spear.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, normalized));
		bool isHit = false;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(spearSpawnPos, normalized, float.PositiveInfinity, LayerUtility.PlatformLayer);
		spear.transform.DOMove(raycastHit2D.point, 0.1f).SetEase(Ease.Linear).OnUpdate(delegate
		{
			if (baran.IsAttack10Progressing && !isHit)
			{
				float num = Vector2.Distance(spearSpawnPos, spear.transform.position);
				float num2 = Vector2.Distance(spearSpawnPos, targetPos);
				if (!(num < num2))
				{
					baran.TryTakeDamage(pierceState.HitInfo);
					baran.HitSpear();
					isHit = true;
				}
			}
		})
			.OnComplete(delegate
			{
				pierceState.Spear = spear;
				pierceState.HitDirection = FacingDirection;
				spear.Hit(MyosoSpear.Target.Baran);
				if (!baran.IsAttack10Progressing)
				{
					baran.StateMachine.ChangeState(pierceState);
				}
				else
				{
					spear.ReturnToPool();
				}
			});
	}

	private async UniTaskVoid ThrowSpearAnyCreature(int fadeDuration)
	{
		Enemy enemy = pierceState.Target;
		if ((object)enemy == null)
		{
			return;
		}
		Vector3 spearSpawnPos = base.transform.position + new Vector3(spearThrowOffset.x * (float)FacingDirection.GetSignX(), spearThrowOffset.y);
		MyosoSpear spear = PoolManager.GetObject<MyosoSpear>("Myoso_Spear.prefab", spearSpawnPos);
		Vector2 normalized = (enemy.VisualCenter - (Vector2)spearSpawnPos).normalized;
		spear.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, normalized));
		await spear.Cast(fadeDuration);
		normalized = (enemy.VisualCenter - (Vector2)spearSpawnPos).normalized;
		spear.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, normalized));
		RaycastHit2D raycastHit2D = Physics2D.Raycast(spearSpawnPos, normalized, float.PositiveInfinity, LayerUtility.PlatformLayer);
		spear.transform.DOMove(raycastHit2D.point, 0.1f).SetEase(Ease.Linear).OnComplete(delegate
		{
			pierceState.Spear = spear;
			pierceState.HitDirection = FacingDirection;
			if (enemy.IsDead)
			{
				spear.ReturnToPool();
			}
			else
			{
				spear.Hit(MyosoSpear.Target.Creature);
				enemy.StateMachine.ChangeState(pierceState);
			}
		});
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(base.transform.position + new Vector3(spearThrowOffset.x * (float)FacingDirection.GetSignX(), spearThrowOffset.y), 1f);
	}
}
