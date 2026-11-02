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

public class HeatFlame : MonoBehaviour, IPoolableObject, IAttackSource
{
	private enum State
	{
		None,
		Moving,
		Finished
	}

	public struct FlameParameter
	{
		public Vector2 FirstEndPos;

		public readonly Enemy TargetEnemy;

		public readonly int DataIndex;

		public readonly float Multiplier;

		public readonly bool ShouldAttack;

		public int Duration;

		private readonly AttackAttribute additionalAttribute;

		public AttackAttribute AttackAttribute => AttackAttribute.Default | AttackAttribute.BlockStuck | additionalAttribute;

		public FlameParameter(Enemy enemy, int dataIndex, float multiplier = 0f, int duration = 0, AttackAttribute additionalAttribute = AttackAttribute.None, Vector2 firstEndPos = default(Vector2), bool shouldAttack = true)
		{
			TargetEnemy = enemy;
			FirstEndPos = (TargetEnemy.IsNull() ? firstEndPos : TargetEnemy.VisualCenter);
			DataIndex = dataIndex;
			Multiplier = multiplier;
			Duration = duration;
			ShouldAttack = shouldAttack;
			this.additionalAttribute = additionalAttribute;
		}
	}

	[Serializable]
	public struct CurveParameter
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

	public const string SkillDataID = "flame";

	private const string EmptyDataID = "empty";

	[SerializeField]
	protected ParticleSystem particle;

	[Space]
	[SerializeField]
	private List<CurveParameter> curveParameters;

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

	private TrailRenderer trailRenderer;

	public const int DefaultDurationFrame = 15;

	private State state;

	private FlameParameter parameter;

	private readonly Vector2[] points = new Vector2[4];

	private int timerMax;

	private int timerCurrent;

	private Timer finishTimer;

	private List<IDamageable> damageables;

	private List<Vector2> gizmoPoints = new List<Vector2>();

	private static int preParameterIndex = -1;

	public string Name => "Fever Flame";

	public Direction FacingDirection
	{
		get
		{
			if (!(base.transform.position.x > Game.Instance.Player.mover.VisualCenter.x))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	private TrailRenderer TrailRenderer => trailRenderer ?? (trailRenderer = GetComponentInChildren<TrailRenderer>());

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public virtual string PrefabKey => "Fever Flame.prefab";

	public GameObject GameObject => base.gameObject;

	private void OnDrawGizmosSelected()
	{
		List<Vector2> list = new List<Vector2>(4);
		list.Add(Vector2.zero);
		float f = testDegreeFromStart * (MathF.PI / 180f);
		Vector2 item = new Vector2(Mathf.Cos(f) * radiusFromStart, Mathf.Sin(f) * radiusFromStart);
		list.Add(item);
		f = testDegreeFromEnd * (MathF.PI / 180f);
		item = testEnemyPoint + new Vector2(Mathf.Cos(f) * radiusFromEnd, Mathf.Sin(f) * radiusFromEnd);
		list.Add(item);
		list.Add(testEnemyPoint);
		gizmoPoints.Clear();
		timerMax = 15;
		for (int i = 0; i <= 15; i++)
		{
			timerCurrent = i;
			float x = CubicBezierCurve(list[0].x, list[1].x, list[2].x, list[3].x);
			float y = CubicBezierCurve(list[0].y, list[1].y, list[2].y, list[3].y);
			gizmoPoints.Add(new Vector2(x, y));
		}
		Gizmos.color = Color.magenta;
		Gizmos.DrawCube(list[0], new Vector3(2f, 4f));
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

	public void Initialize(FlameParameter argParameter)
	{
		parameter = argParameter;
		if (parameter.Duration == 0)
		{
			parameter.Duration = 15;
		}
		damageables = new List<IDamageable>(1);
		Vector2 endPos = (parameter.TargetEnemy.IsNull() ? (parameter.FirstEndPos + testEnemyPoint) : parameter.FirstEndPos);
		Init(base.transform.position, endPos, parameter.Duration);
		base.gameObject.SetActive(value: true);
		particle.Play();
		state = State.Moving;
		SoundManager.PlayOneShotAttached(shootEvent, base.transform);
	}

	protected virtual CurveParameter GetCurveParameter()
	{
		for (int i = 0; i < 10; i++)
		{
			int num = CryptoRandom.Range(0, curveParameters.Count);
			if (num != preParameterIndex)
			{
				preParameterIndex = num;
				break;
			}
		}
		return curveParameters[preParameterIndex];
	}

	private void Init(Vector2 startPos, Vector2 endPos, int duration)
	{
		timerMax = duration;
		timerCurrent = 0;
		CurveParameter curveParameter = GetCurveParameter();
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

	protected virtual bool CanReturn()
	{
		finishTimer.Tick(Time.fixedDeltaTime);
		return finishTimer.IsDone;
	}

	protected virtual void OnFinishedMoving()
	{
		finishTimer = new Timer(TrailRenderer.time);
		particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	private void FixedUpdate()
	{
		if (state == State.Finished && CanReturn())
		{
			ReturnToPool();
		}
		else
		{
			if (state != State.Moving)
			{
				return;
			}
			timerCurrent++;
			if (!parameter.TargetEnemy.IsNull())
			{
				points[3] = parameter.TargetEnemy.VisualCenter;
			}
			base.transform.position = new Vector3(CubicBezierCurve(points[0].x, points[1].x, points[2].x, points[3].x), CubicBezierCurve(points[0].y, points[1].y, points[2].y, points[3].y));
			if (timerCurrent >= timerMax)
			{
				state = State.Finished;
				OnFinishedMoving();
				if (!parameter.TargetEnemy.IsNull() && parameter.TargetEnemy.Health > 0)
				{
					PlayerMover.Attack(new PlayerMover.AttackParameter(parameter.TargetEnemy, parameter.ShouldAttack ? "flame" : "empty", parameter.DataIndex, parameter.AttackAttribute, this, random: true, parameter.Multiplier), damageables);
				}
				SoundManager.PlayOneShotAttached(hitEvent, base.transform);
			}
		}
	}

	public Vector3 GetBezierPosition(int current, float max)
	{
		return new Vector3(CubicBezierCurve(points[0].x, points[1].x, points[2].x, points[3].x, current, max), CubicBezierCurve(points[0].y, points[1].y, points[2].y, points[3].y, current, max));
	}

	private float CubicBezierCurve(float a, float b, float c, float d)
	{
		return CubicBezierCurve(a, b, c, d, timerCurrent, timerMax);
	}

	public static Vector3 GetBezierPosition(List<Vector2> points, int current, float max)
	{
		return new Vector3(CubicBezierCurve(points[0].x, points[1].x, points[2].x, points[3].x, current, max), CubicBezierCurve(points[0].y, points[1].y, points[2].y, points[3].y, current, max));
	}

	private static float CubicBezierCurve(float a, float b, float c, float d, int current, float max)
	{
		float num = (float)current / max;
		return Mathf.Pow(1f - num, 3f) * a + Mathf.Pow(1f - num, 2f) * 3f * num * b + Mathf.Pow(num, 2f) * 3f * (1f - num) * c + Mathf.Pow(num, 3f) * d;
	}

	public static async UniTask ShootFlameAsync(IDamageable damageable, string flameID, float multiplier)
	{
		if (damageable is Enemy enemy)
		{
			FlameData flameData = Game.DataManager.GetFlameData(flameID);
			int num = Mathf.Max(flameData.DelayFrame - 15, 0);
			int shootDuration = Mathf.Min(flameData.DelayFrame, 15);
			if (num > 0)
			{
				await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(num);
			}
			for (int i = 0; i < flameData.Count; i++)
			{
				await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(flameData.CycleFrames[i]);
				ShootFlame(new FlameParameter(enemy, 0, multiplier, shootDuration));
			}
		}
	}

	public static async UniTask ShootFlameAsync(IDamageable damageable, string flameID)
	{
		if (damageable is Enemy enemy)
		{
			FlameData flameData = Game.DataManager.GetFlameData(flameID);
			int num = Mathf.Max(flameData.DelayFrame - 15, 0);
			int shootDuration = Mathf.Min(flameData.DelayFrame, 15);
			if (num > 0)
			{
				await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(num);
			}
			for (int i = 0; i < flameData.Count; i++)
			{
				await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(flameData.CycleFrames[i]);
				ShootFlame(new FlameParameter(enemy, 0, 0f, shootDuration));
			}
		}
	}

	public static async UniTask ShootFlameAsync(List<IDamageable> damageables, string flameID)
	{
		FlameData flameData = Game.DataManager.GetFlameData(flameID);
		int num = Mathf.Max(flameData.DelayFrame - 15, 0);
		int shootDuration = Mathf.Min(flameData.DelayFrame, 15);
		if (num > 0)
		{
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(num);
		}
		for (int i = 0; i < flameData.Count; i++)
		{
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(flameData.CycleFrames[i]);
			foreach (IDamageable damageable in damageables)
			{
				if (damageable is Enemy enemy)
				{
					ShootFlame(new FlameParameter(enemy, 0, 0f, shootDuration));
				}
			}
		}
	}

	public static void ShootFlame(FlameParameter parameter)
	{
		if (CanShoot(parameter.TargetEnemy))
		{
			Game.Instance.ObjectPoolManager.GetObject<HeatFlame>("Fever Flame.prefab", PlayerMover.VisualCenter).Initialize(parameter);
		}
	}

	public static int GetDelayFrame(string id)
	{
		return Mathf.Max(Game.DataManager.GetFlameData(id).DelayFrame - 15, 0);
	}

	protected static bool CanShoot(Enemy enemy)
	{
		if (!(enemy is DarkChasaShadow))
		{
			return !(enemy is ChasaShadow);
		}
		return false;
	}

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		OnReturnToPool();
		state = State.None;
	}

	protected virtual void OnReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
		TrailRenderer.Clear();
		finishTimer = null;
	}
}
