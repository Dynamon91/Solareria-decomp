using System;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public abstract class FlyingCreature : Creature
{
	private struct AirMoveInfo
	{
		public readonly bool Right;

		public readonly bool Left;

		public readonly bool Up;

		public readonly bool Down;

		public AirMoveInfo(RaycastHit2D right, RaycastHit2D left, RaycastHit2D up, RaycastHit2D down)
		{
			Right = right.collider == null;
			Left = left.collider == null;
			Up = up.collider == null;
			Down = down.collider == null;
		}
	}

	[Header("비행 벽감지")]
	[SerializeField]
	private Vector2 checkWallBoxSize;

	public virtual string MoveAnimation => "";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.PatrolState = new FlyCreaturePatrolState(this, patrolWaitMinFrame, patrolWaitMaxFrame);
	}

	public override Vector2 GetPatrolDestination()
	{
		float f = (float)CryptoRandom.Range(0, 360) * (MathF.PI / 180f);
		float num = UnityEngine.Random.Range(0f, patrolRange);
		return new Vector2(base.SpawnPos.x + Mathf.Cos(f) * num, base.SpawnPos.y + Mathf.Sin(f) * num);
	}

	public bool MoveAirTo(float speed, Vector3 targetPos, bool moveForward)
	{
		Vector2 vector = base.transform.position;
		Vector2 normalized = ((Vector2)targetPos - vector).normalized;
		Flip(normalized.x);
		Vector2 visualCenter = VisualCenter;
		Vector2 vector2 = new Vector2(normalized.x.GetSign(), normalized.y.GetSign());
		float x = VisualCenter.x + checkWallBoxSize.x / 2f * vector2.x;
		float y = VisualCenter.y + checkWallBoxSize.y / 2f * vector2.y;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2(x, visualCenter.y), new Vector2(vector2.x, 0f), 0.1f, LayerUtility.PlatformLayer);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(new Vector2(visualCenter.x, y), new Vector2(0f, vector2.y), 0.1f, LayerUtility.PlatformLayer);
		Vector2 vector3 = normalized * (speed * Time.fixedDeltaTime);
		if ((bool)raycastHit2D)
		{
			vector3 = new Vector2(vector3.x * raycastHit2D.normal.x, vector3.y * vector2.y);
		}
		if ((bool)raycastHit2D2)
		{
			vector3 = new Vector2(vector3.x * vector2.x, vector3.y * raycastHit2D2.normal.y);
		}
		Vector2 vector4 = vector;
		bool result = false;
		if (moveForward)
		{
			vector4 += vector3;
		}
		else
		{
			vector4 -= vector3;
		}
		if (Mathf.Approximately(targetPos.x, vector4.x) || (targetPos.x - vector.x).GetSign() * (targetPos.x - vector4.x).GetSign() < 0)
		{
			vector4.x = targetPos.x;
			result = true;
		}
		base.transform.position = vector4;
		return result;
	}

	public void MoveAirTo(Vector2 velocity)
	{
		Vector2 vector = base.transform.position;
		float fixedDeltaTime = Time.fixedDeltaTime;
		AirMoveInfo airMoveInfo = CanGoAir();
		if (velocity.x > 0f && !airMoveInfo.Right)
		{
			velocity.x = 0f;
		}
		if (velocity.x < 0f && !airMoveInfo.Left)
		{
			velocity.x = 0f;
		}
		if (velocity.y > 0f && !airMoveInfo.Up)
		{
			velocity.y = 0f;
		}
		if (velocity.y < 0f && !airMoveInfo.Down)
		{
			velocity.y = 0f;
		}
		vector += velocity * fixedDeltaTime;
		base.transform.position = vector;
	}

	public bool IsOverlappedWall(Vector3 targetPos)
	{
		Vector2 visualCenter = VisualCenter;
		Vector2 normalized = ((Vector2)targetPos - (Vector2)base.transform.position).normalized;
		Vector2 vector = new Vector2(normalized.x.GetSign(), normalized.y.GetSign());
		float x = VisualCenter.x + checkWallBoxSize.x / 2f * vector.x;
		float y = VisualCenter.y + checkWallBoxSize.y / 2f * vector.y;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2(x, visualCenter.y), new Vector2(vector.x, 0f), 0.1f, LayerUtility.PlatformLayer);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(new Vector2(visualCenter.x, y), new Vector2(0f, vector.y), 0.1f, LayerUtility.PlatformLayer);
		if (!raycastHit2D)
		{
			return raycastHit2D2;
		}
		return true;
	}

	public virtual void OnEncounterPlayer()
	{
	}

	private AirMoveInfo CanGoAir()
	{
		Vector2 visualCenter = VisualCenter;
		RaycastHit2D right = Physics2D.Raycast(visualCenter, Vector2.right, checkWallBoxSize.x / 2f, LayerUtility.PlatformLayer);
		RaycastHit2D left = Physics2D.Raycast(visualCenter, Vector2.left, checkWallBoxSize.x / 2f, LayerUtility.PlatformLayer);
		RaycastHit2D up = Physics2D.Raycast(visualCenter, Vector2.up, checkWallBoxSize.y / 2f, LayerUtility.PlatformLayer);
		RaycastHit2D down = Physics2D.Raycast(visualCenter, Vector2.down, checkWallBoxSize.y / 2f, LayerUtility.PlatformLayer);
		return new AirMoveInfo(right, left, up, down);
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(base.SpawnPosition, patrolRange);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(VisualCenter, checkWallBoxSize);
	}
}
