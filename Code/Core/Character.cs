using FSM;
using Library;
using SPR;
using UnityEngine;

public abstract class Character : SpineObject
{
	public bool IsNull => base.gameObject.IsNull();

	public SimpleStateMachine StateMachine { get; protected set; }

	protected virtual void Awake()
	{
		StateMachine = new SimpleStateMachine();
		base.VfxController.InitializeVFXDictionary();
	}

	protected virtual void FixedUpdate()
	{
		StateMachine.FixedUpdate(Time.fixedDeltaTime);
	}

	protected Direction GetTargetDirectionX(Vector3 targetPos)
	{
		Vector3 position = base.transform.position;
		if ((targetPos - position).normalized.x.GetSign() <= 0)
		{
			return Direction.Left;
		}
		return Direction.Right;
	}

	public void LookAt(Vector3 targetPos)
	{
		Direction targetDirectionX = GetTargetDirectionX(targetPos);
		Flip(targetDirectionX);
	}

	public bool CheckWall(float distance)
	{
		Vector3 position = base.transform.position;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.right, distance, LayerMask.GetMask("Platform"));
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(position, -Vector2.right, distance, LayerMask.GetMask("Platform"));
		if (!raycastHit2D)
		{
			return raycastHit2D2;
		}
		return true;
	}
}
