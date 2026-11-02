using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class Sotdae : SpineObject, IStateSavable, IDamageable
{
	private enum State
	{
		None,
		Dropped
	}

	[SerializeField]
	private float dropDuration;

	[SerializeField]
	private float jumpPower;

	[SerializeField]
	private Ease ease;

	[Header("Reward")]
	[SerializeField]
	private string rewardItemName;

	[SerializeField]
	private Vector2Int dropStartLocalPos;

	[SerializeField]
	private Vector2Int dropEndLocalPos;

	[Header("플랫폼")]
	[SerializeField]
	private List<SotdaePlatform> platforms;

	[Header("References")]
	[SerializeField]
	private ParticleSystem hit;

	[SerializeField]
	private BoxCollider2D firstAppearTrigger;

	private BoxCollider2D box2D;

	private bool isDropped;

	public string ID => "Sotdae";

	public HitType HitType => HitType.Object;

	public override string Name => "";

	public override Vector2 VisualCenter => base.transform.position;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private static PlayerMover Player => Game.Instance.Player.mover;

	private void FixedUpdate()
	{
		CheckTrigger();
		CheckReset();
	}

	public void LoadState(int state)
	{
		isDropped = state == 1;
		platforms[0].OnEnter += delegate
		{
			platforms[0].Appear();
		};
		platforms[0].Disappear();
		for (int num = 1; num < platforms.Count; num++)
		{
			platforms[num].Disappear();
			platforms[num].Box2D.enabled = false;
		}
		for (int num2 = 0; num2 < platforms.Count; num2++)
		{
			int next = Mathf.Min(num2 + 1, platforms.Count - 1);
			int current = num2;
			platforms[num2].OnEnter += delegate
			{
				Appear(current, next);
				if (current > 0)
				{
					Disappear(current - 1);
				}
			};
		}
	}

	public void ChangeState(int state)
	{
		isDropped = state == 1;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (isDropped)
		{
			return false;
		}
		if (!(hitInfo.Source is PlayerMover playerMover))
		{
			return true;
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			hit.Play(withChildren: true);
			SetAnimation("hit", loop: false);
			AddAnimation("idle");
			DropKey();
		}
		else
		{
			playerMover.PSM.ChangeState(playerMover.PSM.AttackStuckState);
		}
		return true;
	}

	private void DropKey()
	{
		Vector2 startPos = (Vector2)base.transform.position + (Vector2)dropStartLocalPos;
		Vector2Int endPos = ((Vector2)base.transform.position + (Vector2)dropEndLocalPos).ToVector2Int();
		ItemObject.DropItemAsync(rewardItemName, startPos, endPos).Forget();
		ChangeState(1);
	}

	private void Appear(int current, int next)
	{
		if (current != next)
		{
			if (Game.Instance.SaveData.PlayerData.HasEverObtainedItem("OphilinTreasureHint", out var _))
			{
				platforms[next].Appear();
			}
			platforms[next].Box2D.enabled = true;
		}
	}

	private void Disappear(int current)
	{
		current = Mathf.Max(0, current);
		if (current != 0)
		{
			platforms[current].Box2D.enabled = false;
		}
		platforms[current].Disappear();
	}

	private void CheckTrigger()
	{
		if (!Game.Instance.SaveData.PlayerData.HasEverObtainedItem("OphilinTreasureHint", out var current) || platforms[1].IsAppear)
		{
			return;
		}
		Collider2D[] array = AttackUtility.OverlapBoxAll(firstAppearTrigger.transform.position, firstAppearTrigger.size * firstAppearTrigger.transform.localScale, LayerUtility.PlayerLayer);
		if (array.Length == 0)
		{
			platforms[0].Disappear();
			return;
		}
		Collider2D[] array2 = array;
		for (current = 0; current < array2.Length; current++)
		{
			if (array2[current].TryGetComponent<PlayerMover>(out var _))
			{
				platforms[0].Appear();
			}
		}
	}

	private void CheckReset()
	{
		if (!Player.IsOnGround)
		{
			return;
		}
		for (int i = 1; i < platforms.Count; i++)
		{
			if (Player.transform.position.y > platforms[i].transform.position.y)
			{
				return;
			}
		}
		for (int j = 1; j < platforms.Count; j++)
		{
			platforms[j].Disappear();
			platforms[j].Box2D.enabled = false;
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector2 vector = (Vector2)base.transform.position + (Vector2)dropStartLocalPos;
		Vector2Int v = ((Vector2)base.transform.position + (Vector2)dropEndLocalPos).ToVector2Int();
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(vector, 1f);
		Gizmos.DrawWireSphere(v.ToVector3(), 1f);
	}
}
