using System.Collections.Generic;
using FMODUnity;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR.Field;

public class Money : MonoBehaviour, IPoolableObject
{
	private enum State
	{
		Spread,
		Falling,
		Grounded,
		Collecting
	}

	private enum Amount
	{
		One = 1,
		Five = 5,
		Twenty = 20,
		Fifty = 50,
		Hundred = 100,
		FiveHundred = 500,
		Thousand = 1000
	}

	[SerializeField]
	private List<Sprite> moneySprites;

	[SerializeField]
	private EventReference collectEvent;

	[SerializeField]
	private float groundedToCollectDelay = 0.35f;

	private int amount;

	private const float Gravity = 6f;

	private const float SpreadSpeed = 32f;

	public const float CollectingDistance = 10f;

	private const float CollectingSpeed = 53f;

	private Rigidbody2D rb2D;

	private SpriteRenderer sr;

	private BoxCollider2D bc;

	private State state;

	private float groundedEnterTime;

	private Vector2 GroundBox = new Vector2(1f, 0.1f);

	private Rigidbody2D Rb2D => rb2D ?? (rb2D = GetComponent<Rigidbody2D>());

	private SpriteRenderer Sr => sr ?? (sr = GetComponent<SpriteRenderer>());

	private BoxCollider2D Collider => bc ?? (bc = GetComponent<BoxCollider2D>());

	private Vector2 GroundCheckPosition => (Vector2)base.transform.position + Vector2.down * (base.transform.localScale.y * Collider.size.y / 2f);

	private static PlayerController Player => Game.Instance.Player;

	public string PrefabKey => "Money.prefab";

	public GameObject GameObject => base.gameObject;

	public void Initialize(int value)
	{
		state = State.Spread;
		groundedEnterTime = -999f;
		Rb2D.velocity = Vector2.zero;
		base.gameObject.layer = LayerUtility.OhnLayerIndex;
		amount = value;
		SpriteRenderer spriteRenderer = Sr;
		Sprite sprite = ((value > 20) ? moneySprites[2] : (value switch
		{
			1 => moneySprites[0], 
			5 => moneySprites[1], 
			20 => moneySprites[2], 
			_ => null, 
		}));
		spriteRenderer.sprite = sprite;
	}

	private void FixedUpdate()
	{
		switch (state)
		{
		case State.Spread:
		{
			Vector2 vector = new Vector2(Random.Range(-1f, 1f), Random.Range(2.5f, 4f));
			Rb2D.velocity = vector.normalized * 32f;
			state = State.Falling;
			break;
		}
		case State.Falling:
			Falling();
			break;
		case State.Grounded:
			if (!(Time.time - groundedEnterTime < groundedToCollectDelay))
			{
				CheckPlayerToBeCollected();
			}
			break;
		case State.Collecting:
			Collecting();
			break;
		}
	}

	private void Falling()
	{
		Rb2D.velocity += Physics2D.gravity * (6f * Time.fixedDeltaTime);
		if ((object)Physics2D.OverlapBox(GroundCheckPosition, GroundBox, 0f, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer) != null)
		{
			Rb2D.velocity = Vector2.zero;
			groundedEnterTime = Time.time;
			state = State.Grounded;
		}
	}

	private void CheckPlayerToBeCollected()
	{
		if (!Player.IsNull())
		{
			float num = Vector2.Distance(Player.transform.position, base.transform.position);
			float distance = Player.PlayerStat.GetStat<CollectingRange>().Distance;
			if (!(num > distance))
			{
				state = State.Collecting;
				base.gameObject.layer = LayerUtility.ObjectLayerIndex;
			}
		}
	}

	private void Collecting()
	{
		Vector2 vector = Player.mover.VisualCenter - (Vector2)base.transform.position;
		if (vector.magnitude < 0.01f)
		{
			CollectNow();
		}
		else
		{
			Rb2D.velocity = vector.normalized * 53f;
		}
	}

	private void CollectNow()
	{
		Game.Instance.SoundManager.PlayOneShot(collectEvent);
		Game.Instance.Player.PlayerStat.AddMoney(amount);
		ReturnToPool();
	}

	private void OnCollisionEnter2D(Collision2D coll)
	{
		if (state == State.Collecting && (int)LayerUtility.PlayerLayer == ((int)LayerUtility.PlayerLayer | (1 << coll.gameObject.layer)) && coll.gameObject.TryGetComponent<PlayerMover>(out var _))
		{
			CollectNow();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(GroundCheckPosition, GroundBox);
	}

	public void InitializeBeforeUse()
	{
		groundedEnterTime = -999f;
		Rb2D.velocity = Vector2.zero;
		base.gameObject.layer = LayerUtility.OhnLayerIndex;
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public static List<int> GetRandomAmounts(int total)
	{
		if (total < 1)
		{
			return new List<int>();
		}
		List<Amount> list = new List<Amount>();
		int num = total;
		while (num > 0)
		{
			Amount amount = FindBiggestAmountOf(num);
			list.Add(amount);
			num = (int)(num - amount);
		}
		Amount amount2 = list[0];
		if (amount2 != Amount.One)
		{
			list.RemoveAt(0);
			list.AddRange(Disassemble(amount2));
		}
		return list.ConvertAll((Amount result) => (int)result);
	}

	private static Amount FindBiggestAmountOf(int total)
	{
		if (total >= 100)
		{
			if (total < 1000)
			{
				if (total >= 500)
				{
					return Amount.FiveHundred;
				}
				return Amount.Hundred;
			}
			return Amount.Thousand;
		}
		if (total >= 20)
		{
			if (total >= 50)
			{
				return Amount.Fifty;
			}
			return Amount.Twenty;
		}
		if (total >= 5)
		{
			return Amount.Five;
		}
		return Amount.One;
	}

	private static List<Amount> Disassemble(Amount amount)
	{
		return amount switch
		{
			Amount.Thousand => new List<Amount>
			{
				Amount.FiveHundred,
				Amount.FiveHundred
			}, 
			Amount.FiveHundred => new List<Amount>
			{
				Amount.Hundred,
				Amount.Hundred,
				Amount.Hundred,
				Amount.Hundred,
				Amount.Hundred
			}, 
			Amount.Hundred => new List<Amount>
			{
				Amount.Fifty,
				Amount.Fifty
			}, 
			Amount.Fifty => new List<Amount>
			{
				Amount.Twenty,
				Amount.Twenty,
				Amount.Five,
				Amount.Five
			}, 
			Amount.Twenty => new List<Amount>
			{
				Amount.Five,
				Amount.Five,
				Amount.Five,
				Amount.Five
			}, 
			Amount.Five => new List<Amount>
			{
				Amount.One,
				Amount.One,
				Amount.One,
				Amount.One,
				Amount.One
			}, 
			_ => new List<Amount> { Amount.One }, 
		};
	}
}
