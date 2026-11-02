using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR.Debugging;

public class DebugCollider : MonoBehaviour, IPoolableObject
{
	private Collider2D targetColl;

	public string PrefabKey => "Debug/DebugCollider.prefab";

	public GameObject GameObject => base.gameObject;

	public async UniTask ShowAsync(Collider2D coll)
	{
		targetColl = coll;
		base.gameObject.SetActive(value: true);
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
		ReturnToPool();
	}

	public async UniTask ShowBoxAsync(Vector2 center, Vector2 size, float angle)
	{
		Vector3 eulerAngles = base.transform.eulerAngles;
		eulerAngles.z = angle;
		base.transform.eulerAngles = eulerAngles;
		base.transform.position = center;
		BoxCollider2D boxColl = base.gameObject.AddComponent<BoxCollider2D>();
		boxColl.size = size;
		targetColl = boxColl;
		base.gameObject.SetActive(value: true);
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
		ReturnToPool();
		Object.Destroy(boxColl);
	}

	private void OnDrawGizmos()
	{
		List<List<Vector3>> list = new List<List<Vector3>>();
		Transform transform = targetColl.transform;
		Vector3 position = base.transform.position;
		Vector3 scale = transform.lossyScale;
		Collider2D collider2D = targetColl;
		if (!(collider2D is PolygonCollider2D polygonCollider2D))
		{
			if (!(collider2D is BoxCollider2D { offset: var offset } boxCollider2D))
			{
				if (collider2D is CircleCollider2D circleCollider2D)
				{
					Vector3 center = position + new Vector3(circleCollider2D.offset.x * scale.x, circleCollider2D.offset.y * scale.y);
					Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
					Gizmos.DrawSphere(center, circleCollider2D.radius);
				}
				return;
			}
			List<Vector3> list2 = new List<Vector3>();
			Vector2 vector = boxCollider2D.size / 2f;
			list2.Add(new Vector2((0f - vector.x + offset.x) * scale.x, (0f - vector.y + offset.y) * scale.y).RotateVector(base.transform.eulerAngles.z));
			list2.Add(new Vector2((0f - vector.x + offset.x) * scale.x, (vector.y + offset.y) * scale.y).RotateVector(base.transform.eulerAngles.z));
			list2.Add(new Vector2((vector.x + offset.x) * scale.x, (vector.y + offset.y) * scale.y).RotateVector(base.transform.eulerAngles.z));
			list2.Add(new Vector2((vector.x + offset.x) * scale.x, (0f - vector.y + offset.y) * scale.y).RotateVector(base.transform.eulerAngles.z));
			list.Add(list2);
		}
		else
		{
			Vector2 offset2 = polygonCollider2D.offset;
			List<Vector3> item = polygonCollider2D.points.Select(delegate(Vector2 point)
			{
				Vector3 vector5 = point + offset2;
				return new Vector3(vector5.x * scale.x, vector5.y * scale.y);
			}).ToList();
			list.Add(item);
		}
		if (list[0].Count == 4)
		{
			Vector3 vector2 = list[0][0];
			Vector2 vector3 = new Vector2(vector2.x, vector2.y);
			Vector2 vector4 = new Vector2(vector2.x, vector2.y);
			foreach (Vector3 item2 in list[0])
			{
				vector3.x = Mathf.Min(vector3.x, item2.x);
				vector3.y = Mathf.Min(vector3.y, item2.y);
				vector4.x = Mathf.Max(vector4.x, item2.x);
				vector4.y = Mathf.Max(vector4.y, item2.y);
			}
			Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
			Gizmos.DrawCube((Vector2)position + (vector3 + vector4) / 2f, vector4 - vector3);
			return;
		}
		Gizmos.color = Color.red;
		foreach (List<Vector3> item3 in list)
		{
			for (int num = 0; num < item3.Count - 1; num++)
			{
				Gizmos.DrawLine(item3[num] + position, item3[num + 1] + position);
			}
			Gizmos.DrawLine(item3[item3.Count - 1] + position, item3[0] + position);
		}
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
