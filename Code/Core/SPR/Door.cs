using System.Collections.Generic;
using SPR.Field;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class Door : MonoBehaviour, IPoolableObject, IImpassable, IUnclimbable
{
	[SerializeField]
	private bool openOnAwake;

	private Animator animator;

	private BoxCollider2D boxCollider;

	private bool isOpen;

	private static readonly int openAnimParam = Animator.StringToHash("IsOpen");

	private readonly List<Vector2> groundRange = new List<Vector2>();

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private BoxCollider2D BoxCollider => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	protected bool OpenOnAwake => openOnAwake;

	public DoorTrigger Trigger { get; set; }

	public Vector2 CollSize => BoxCollider.size;

	public string PrefabKey => "TrapDoor.prefab";

	public GameObject GameObject => base.gameObject;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void Awake()
	{
		BoxCollider.enabled = !openOnAwake;
		isOpen = openOnAwake;
		OnAwake();
		SetGroundRange();
	}

	private void SetGroundRange()
	{
		Vector2 vector = BoxCollider.bounds.center;
		Vector2 size = BoxCollider.size;
		Vector2 vector2 = vector - size / 2f;
		Vector2 vector3 = vector + new Vector2(size.x, 0f - size.y) / 2f;
		groundRange.Clear();
		for (int i = Mathf.FloorToInt(vector2.x); i <= Mathf.CeilToInt(vector3.x); i++)
		{
			groundRange.Add(new Vector2(i, vector2.y));
		}
	}

	public void SetDoorOpen(bool open)
	{
		if (isOpen != open)
		{
			isOpen = open;
			BoxCollider.enabled = !open;
			PlayAnimation(open);
			Game.Instance.FieldController.ModifyPathGraphs(groundRange, open);
		}
	}

	public void SetColliderSize(Vector2 size)
	{
		BoxCollider.size = size;
		SetGroundRange();
	}

	protected virtual void PlayAnimation(bool open)
	{
		Animator.SetBool(openAnimParam, open);
	}

	protected virtual void OnAwake()
	{
		PlayAnimation(openOnAwake);
	}
}
