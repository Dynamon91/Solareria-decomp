using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.Serialization;

namespace SPR;

public class GaiaCurveBlade : MonoBehaviour, IPoolableObject, IAttackSource
{
	[Serializable]
	private struct CurveParameter
	{
		[SerializeField]
		private float degreeFromStart;

		[SerializeField]
		private float degreeFromEnd;

		[FormerlySerializedAs("distanceFromStart")]
		public float radiusFromStart;

		[FormerlySerializedAs("distanceFromEnd")]
		public float radiusFromEnd;

		public float RadianFromStart => MathF.PI / 180f * degreeFromStart;

		public float RadianFromEnd => MathF.PI / 180f * degreeFromEnd;
	}

	[SerializeField]
	private string address;

	[Space]
	[SerializeField]
	private List<CurveParameter> curveParameters;

	[SerializeField]
	private int durationFrame;

	[SerializeField]
	private EventReference shootEvent;

	[SerializeField]
	private EventReference hitEvent;

	[Space]
	[Header("Test - 기즈모 용도")]
	[SerializeField]
	private float testDegreeFromStart;

	[SerializeField]
	private float testDegreeFromEnd;

	[FormerlySerializedAs("distanceFromStart")]
	[SerializeField]
	private float radiusFromStart;

	[FormerlySerializedAs("distanceFromEnd")]
	[SerializeField]
	private float radiusFromEnd;

	[SerializeField]
	private Vector2 testEnemyPoint;

	private Enemy targetEnemy;

	private readonly Vector2[] points = new Vector2[4];

	private int timerMax;

	private int timerCurrent;

	private List<Vector2> gizmoPoints = new List<Vector2>();

	private float angle;

	private BoxCollider2D box2D;

	private Gaia gaia;

	public string Name => "GaiaBlade";

	public Direction FacingDirection { get; set; }

	public Vector2 Point => testEnemyPoint;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	private void OnDrawGizmosSelected()
	{
		List<Vector2> list = new List<Vector2>(4);
		list.Add(base.transform.position);
		float f = testDegreeFromStart * (MathF.PI / 180f);
		Vector2 item = new Vector2(Mathf.Cos(f) * radiusFromStart, Mathf.Sin(f) * radiusFromStart);
		list.Add(item);
		f = testDegreeFromEnd * (MathF.PI / 180f);
		item = testEnemyPoint + new Vector2(Mathf.Cos(f) * radiusFromEnd, Mathf.Sin(f) * radiusFromEnd);
		list.Add(item);
		list.Add(testEnemyPoint);
		gizmoPoints.Clear();
		timerMax = durationFrame;
		for (int i = 0; i <= durationFrame; i++)
		{
			timerCurrent = i;
			float x = CubicBezierCurve(timerCurrent, list[0].x, list[1].x, list[2].x, list[3].x);
			float y = CubicBezierCurve(timerCurrent, list[0].y, list[1].y, list[2].y, list[3].y);
			gizmoPoints.Add(new Vector2(x, y));
		}
		Gizmos.color = Color.magenta;
		Gizmos.DrawCube(base.transform.position, Vector3.one);
		for (int j = 1; j < list.Count; j++)
		{
			Gizmos.DrawSphere(list[j], 0.5f);
		}
		DrawLine(Vector3.zero);
		void DrawLine(Vector3 center)
		{
			Gizmos.DrawLine(center, gizmoPoints[0]);
			List<Vector2> list2 = gizmoPoints;
			Gizmos.DrawLine(center, list2[list2.Count - 1]);
			for (int k = 0; k < gizmoPoints.Count - 1; k++)
			{
				Gizmos.DrawLine(gizmoPoints[k], gizmoPoints[k + 1]);
			}
		}
	}

	public void Initialize(Gaia gaia, Vector2 destination, int duration)
	{
		if ((object)this.gaia == null)
		{
			this.gaia = gaia;
		}
		Init(base.transform.position, destination);
		base.gameObject.SetActive(value: true);
		SoundManager.PlayOneShotAttached(shootEvent, base.transform);
		timerMax = duration;
	}

	private void Init(Vector2 startPos, Vector2 endPos)
	{
		timerCurrent = 0;
		CurveParameter curveParameter = curveParameters[UnityEngine.Random.Range(0, curveParameters.Count)];
		points[0] = startPos;
		float radianFromStart = curveParameter.RadianFromStart;
		float num = curveParameter.radiusFromStart;
		Vector2 vector = new Vector2(Mathf.Cos(radianFromStart) * num, Mathf.Sin(radianFromStart) * num);
		vector.x *= (endPos.x - startPos.x).GetSign();
		points[1] = startPos + vector;
		float radianFromEnd = curveParameter.RadianFromEnd;
		num = curveParameter.radiusFromEnd;
		vector = new Vector2(Mathf.Cos(radianFromEnd) * num, Mathf.Sin(radianFromEnd) * num);
		vector.x *= (endPos.x - startPos.x).GetSign();
		points[2] = endPos + vector;
		points[3] = endPos;
	}

	public async UniTaskVoid Move(AnimationCurve animationCurve = null)
	{
		while (timerCurrent <= timerMax)
		{
			timerCurrent++;
			base.transform.position = new Vector3(CubicBezierCurve(timerCurrent, points[0].x, points[1].x, points[2].x, points[3].x, animationCurve), CubicBezierCurve(timerCurrent, points[0].y, points[1].y, points[2].y, points[3].y, animationCurve));
			Vector3 vector = new Vector3(CubicBezierCurve(timerCurrent - 1, points[0].x, points[1].x, points[2].x, points[3].x, animationCurve), CubicBezierCurve(timerCurrent - 1, points[0].y, points[1].y, points[2].y, points[3].y, animationCurve));
			if (timerCurrent < timerMax)
			{
				Vector3 normalized = (base.transform.position - vector).normalized;
				angle = Vector2.SignedAngle(Vector2.up, normalized);
				base.transform.rotation = Quaternion.Euler(0f, 0f, angle);
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		base.transform.position -= new Vector3(0f, UnityEngine.Random.Range(0f, 0.5f));
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(20);
		ReturnToPool();
	}

	public async UniTaskVoid AttackMove(float width, AnimationCurve animationCurve = null)
	{
		List<IDamageable> list = new List<IDamageable>();
		while (timerCurrent <= timerMax)
		{
			if ((float)timerCurrent / (float)timerMax >= 0.5f)
			{
				HandleOverlap(list, width);
			}
			timerCurrent++;
			base.transform.position = new Vector3(CubicBezierCurve(timerCurrent, points[0].x, points[1].x, points[2].x, points[3].x, animationCurve), CubicBezierCurve(timerCurrent, points[0].y, points[1].y, points[2].y, points[3].y, animationCurve));
			Vector3 vector = new Vector3(CubicBezierCurve(timerCurrent - 1, points[0].x, points[1].x, points[2].x, points[3].x, animationCurve), CubicBezierCurve(timerCurrent - 1, points[0].y, points[1].y, points[2].y, points[3].y, animationCurve));
			if (timerCurrent < timerMax)
			{
				Vector3 normalized = (base.transform.position - vector).normalized;
				angle = Vector2.SignedAngle(Vector2.up, normalized);
				base.transform.rotation = Quaternion.Euler(0f, 0f, angle);
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		Game.Instance.ObjectPoolManager.GetObject<PoolingParticle>("vfx_Gaia_Blade_ThrustAttack.prefab", base.transform.position).Play();
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(20);
		ReturnToPool();
	}

	private float CubicBezierCurve(int time, float a, float b, float c, float d, AnimationCurve ease = null)
	{
		float num = (float)time / (float)timerMax;
		if (ease != null)
		{
			num = ease.Evaluate(num);
		}
		return Mathf.Pow(1f - num, 3f) * a + Mathf.Pow(1f - num, 2f) * 3f * num * b + Mathf.Pow(num, 2f) * 3f * (1f - num) * c + Mathf.Pow(num, 3f) * d;
	}

	public void HandleOverlap(List<IDamageable> list)
	{
		foreach (IDamageable item in AttackUtility.AttackBox(base.transform.position, Box2D.size * base.transform.localScale, angle, new HitInfo(this), LayerUtility.PlayerLayer, list))
		{
			if (gaia.CanMarkPlayer(item))
			{
				gaia.MarkToPlayer().Forget();
			}
		}
	}

	public void HandleOverlap(List<IDamageable> list, float width)
	{
		foreach (IDamageable item in AttackUtility.AttackBox(base.transform.position, new Vector2(width, Box2D.size.y) * base.transform.localScale, new HitInfo(this), LayerUtility.PlayerLayer, list))
		{
			if (gaia.CanMarkPlayer(item))
			{
				gaia.MarkToPlayer().Forget();
			}
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
