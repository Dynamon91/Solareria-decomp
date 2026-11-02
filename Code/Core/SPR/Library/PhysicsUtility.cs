using System;
using System.Collections;
using System.Linq;
using System.Threading;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using JetBrains.Annotations;
using UnityEngine;

namespace SPR.Library;

public static class PhysicsUtility
{
	public static IEnumerator Move(this Rigidbody2D rb2D, Vector2 endPos, float accel, float terminalV = 999f, CancellationToken cancellationToken = default(CancellationToken))
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		Transform transform = rb2D.transform;
		Vector2 direction = (endPos - (Vector2)transform.position).normalized;
		bool deltaX = endPos.x > transform.position.x;
		bool deltaY = endPos.y > transform.position.y;
		bool isMoving = true;
		while (isMoving)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				rb2D.velocity = Vector2.zero;
				break;
			}
			Vector2 vector;
			if (accel == 0f)
			{
				vector = terminalV * direction;
			}
			else
			{
				vector = rb2D.velocity + direction * (accel * Time.fixedDeltaTime);
				if (vector.x > terminalV)
				{
					vector.x = terminalV;
				}
				else if (vector.x < 0f - terminalV)
				{
					vector.x = 0f - terminalV;
				}
				if (vector.y > terminalV)
				{
					vector.y = terminalV;
				}
				else if (vector.y < 0f - terminalV)
				{
					vector.y = 0f - terminalV;
				}
			}
			Vector2 vector2 = (Vector2)transform.position + vector * Time.fixedDeltaTime;
			if (endPos == vector2)
			{
				isMoving = false;
			}
			else if (deltaX != endPos.x > vector2.x || deltaY != endPos.y > vector2.y)
			{
				vector = (endPos - (Vector2)transform.position) / Time.fixedDeltaTime;
				isMoving = false;
			}
			rb2D.velocity = vector;
			yield return wait;
		}
		rb2D.velocity = Vector2.zero;
	}

	public static IEnumerator LocalMove(this Rigidbody2D rb2D, Vector2 localEndPos, float accel, float terminalV = 999f, CancellationToken cancellationToken = default(CancellationToken))
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		Transform transform = rb2D.transform;
		Vector2 direction = (localEndPos - (Vector2)transform.localPosition).normalized;
		bool deltaX = localEndPos.x > transform.localPosition.x;
		bool deltaY = localEndPos.y > transform.localPosition.y;
		bool isMoving = true;
		while (isMoving)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				rb2D.velocity = Vector2.zero;
				break;
			}
			Vector2 vector;
			if (accel == 0f)
			{
				vector = terminalV * direction;
			}
			else
			{
				vector = rb2D.velocity + direction * (accel * Time.fixedDeltaTime);
				if (vector.x > terminalV)
				{
					vector.x = terminalV;
				}
				else if (vector.x < 0f - terminalV)
				{
					vector.x = 0f - terminalV;
				}
				if (vector.y > terminalV)
				{
					vector.y = terminalV;
				}
				else if (vector.y < 0f - terminalV)
				{
					vector.y = 0f - terminalV;
				}
			}
			Vector2 vector2 = (Vector2)transform.localPosition + vector * Time.fixedDeltaTime;
			if (localEndPos == vector2)
			{
				isMoving = false;
			}
			else if (deltaX != localEndPos.x > vector2.x || deltaY != localEndPos.y > vector2.y)
			{
				vector = (localEndPos - (Vector2)transform.localPosition) / Time.fixedDeltaTime;
				isMoving = false;
			}
			rb2D.velocity = vector;
			yield return wait;
		}
		rb2D.velocity = Vector2.zero;
	}

	public static IEnumerator Rotate(this Rigidbody2D rb2D, float localRotateBy, float accel, float terminalSpeed = 999f, CancellationToken token = default(CancellationToken), Action<float> beforeWaitCallback = null, bool preserveVelocity = false)
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		int direction = ((localRotateBy > 0f) ? 1 : (-1));
		float rotation = rb2D.rotation;
		float endAngle = rotation + localRotateBy;
		bool delta = endAngle > rotation;
		bool isMoving = true;
		while (isMoving)
		{
			if (token.IsCancellationRequested)
			{
				rb2D.angularVelocity = 0f;
				break;
			}
			float num;
			if (accel == 0f)
			{
				num = terminalSpeed * (float)direction;
			}
			else
			{
				num = rb2D.angularVelocity + (float)direction * (accel * Time.fixedDeltaTime);
				if (num > terminalSpeed)
				{
					num = terminalSpeed;
				}
				else if (num < 0f - terminalSpeed)
				{
					num = 0f - terminalSpeed;
				}
			}
			float num2 = rb2D.rotation + num * Time.fixedDeltaTime;
			if (delta != endAngle > num2)
			{
				rb2D.angularVelocity = (endAngle - rb2D.rotation) / Time.fixedDeltaTime;
				num2 = endAngle;
				isMoving = false;
			}
			else
			{
				rb2D.angularVelocity = num;
			}
			beforeWaitCallback?.Invoke(num2);
			yield return wait;
		}
		if (!preserveVelocity)
		{
			rb2D.angularVelocity = 0f;
		}
	}

	public static IEnumerator WaitForFixedUpdatesOrCancel(int frameCount, CancellationToken token = default(CancellationToken))
	{
		for (int i = 0; i < frameCount; i++)
		{
			yield return new WaitForFixedUpdate();
			if (token.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public static Vector2 GetCollidePosition(Vector2 direction, Vector2 startPosition, Vector2 colliderSize, float maxDistance = 300f)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(startPosition, direction, maxDistance, LayerUtility.PlatformLayer);
		float num = (raycastHit2D ? raycastHit2D.distance : maxDistance);
		return startPosition + (direction * num - direction * colliderSize / 2f);
	}

	public static Vector2 GetCollidePosition(Vector2 direction, Vector2 startPosition, Vector2 colliderSize, [CanBeNull] out GameObject hitObject, float maxDistance = 300f)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(startPosition, direction, maxDistance, LayerUtility.PlatformLayer);
		hitObject = (raycastHit2D ? raycastHit2D.transform.gameObject : null);
		float num = (raycastHit2D ? raycastHit2D.distance : maxDistance);
		return startPosition + (direction * num - direction * colliderSize / 2f);
	}

	public static int GetColliderHeight(Vector2 position, float maxDistance = 10f)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.up, maxDistance, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(position, Vector2.down, maxDistance, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		float num = 0f;
		if ((bool)raycastHit2D)
		{
			num += raycastHit2D.distance;
		}
		if ((bool)raycastHit2D2)
		{
			num += raycastHit2D2.distance;
		}
		return Mathf.RoundToInt(num);
	}

	public static int GetColliderWidth(Vector2 position, float maxDistance = 10f)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.left, maxDistance, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(position, Vector2.right, maxDistance, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		float num = 0f;
		if ((bool)raycastHit2D)
		{
			num += raycastHit2D.distance;
		}
		if ((bool)raycastHit2D2)
		{
			num += raycastHit2D2.distance;
		}
		return Mathf.RoundToInt(num);
	}

	public static IEnumerator PendulumRotate(this Rigidbody2D rb2D, Rigidbody2D hangingBody, float length, float endAngle, float duration, Ease ease)
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		float targetAngle = rb2D.rotation;
		TweenerCore<float, float, FloatOptions> tween = DOTween.To(() => targetAngle, delegate(float x)
		{
			targetAngle = x;
		}, endAngle, duration).SetEase(ease);
		while (tween.IsActive())
		{
			float rotation = rb2D.rotation;
			float angularVelocity = (targetAngle - rotation) / Time.fixedDeltaTime;
			rb2D.angularVelocity = angularVelocity;
			Vector2 position = rb2D.position;
			position.x += Mathf.Sin(targetAngle * (MathF.PI / 180f)) * length;
			position.y -= Mathf.Cos(targetAngle * (MathF.PI / 180f)) * length;
			Vector2 velocity = (position - hangingBody.position) / Time.fixedDeltaTime;
			hangingBody.velocity = velocity;
			yield return wait;
		}
		rb2D.angularVelocity = 0f;
		hangingBody.velocity = Vector2.zero;
	}

	public static IEnumerator MoveWithTween(this Rigidbody2D rb2D, Vector2 endPos, float duration, Ease ease, CancellationToken token = default(CancellationToken))
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		Vector2 targetPosition = rb2D.position;
		TweenerCore<Vector2, Vector2, VectorOptions> tween = DOTween.To(() => targetPosition, delegate(Vector2 x)
		{
			targetPosition = x;
		}, endPos, duration).SetEase(ease);
		while (tween.IsActive())
		{
			if (token.IsCancellationRequested)
			{
				yield break;
			}
			Vector2 position = rb2D.position;
			Vector2 velocity = (targetPosition - position) / Time.fixedDeltaTime;
			rb2D.velocity = velocity;
			yield return wait;
		}
		rb2D.velocity = Vector2.zero;
	}

	public static IEnumerator LocalMoveYWithTween(this Rigidbody2D rb2D, float localEndPos, float duration, Ease ease, CancellationToken token = default(CancellationToken))
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		float targetPositionY = rb2D.transform.localPosition.y;
		TweenerCore<float, float, FloatOptions> tween = DOTween.To(() => targetPositionY, delegate(float x)
		{
			targetPositionY = x;
		}, localEndPos, duration).SetEase(ease);
		while (tween.IsActive())
		{
			if (token.IsCancellationRequested)
			{
				yield break;
			}
			float y = rb2D.transform.localPosition.y;
			float y2 = (targetPositionY - y) / Time.fixedDeltaTime;
			rb2D.velocity = new Vector2(0f, y2);
			yield return wait;
		}
		rb2D.velocity = Vector2.zero;
	}

	public static IEnumerator PivotMoveWithTween(this Rigidbody2D rb2D, DOGetter<Vector2> pivotGetter, Vector2 endPositionFromPivot, float duration, Ease ease)
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		Vector2 targetLocalPosition = (Vector2)rb2D.transform.position - pivotGetter();
		TweenerCore<Vector2, Vector2, VectorOptions> tween = DOTween.To(() => targetLocalPosition, delegate(Vector2 x)
		{
			targetLocalPosition = x;
		}, endPositionFromPivot, duration).SetEase(ease);
		while (tween.IsActive())
		{
			Vector2 vector = rb2D.transform.position;
			Vector2 velocity = (pivotGetter() + targetLocalPosition - vector) / Time.fixedDeltaTime;
			rb2D.velocity = velocity;
			yield return wait;
		}
		rb2D.velocity = Vector2.zero;
	}

	public static Vector2 GetParabolaInitialVelocityWithJerk(float xMax, float yMax, float tMax, float jerk, float gravity)
	{
		float y = ((yMax == 0f) ? 0f : ((yMax + 0.5f * gravity * Mathf.Pow(tMax, 2f)) / tMax));
		return new Vector2((xMax == 0f) ? 0f : ((xMax - 1f / 6f * jerk * Mathf.Pow(tMax, 3f)) / tMax), y);
	}

	public static bool IsPlayerStay(Vector2 center, Vector2 size, bool includingDefaultLayer = false)
	{
		int layerMask = (int)LayerUtility.PlayerLayer | (includingDefaultLayer ? ((int)LayerUtility.DefaultLayer) : 0);
		PlayerMover component;
		return Physics2D.OverlapBoxAll(center, size, 0f, layerMask).Any((Collider2D col) => col.TryGetComponent<PlayerMover>(out component));
	}
}
