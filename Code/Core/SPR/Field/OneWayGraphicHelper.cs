using System;
using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;
using UnityEngine.U2D;

namespace SPR.Field;

public class OneWayGraphicHelper : ScriptableObject
{
	[Serializable]
	public abstract class PlatformFeature
	{
		[SerializeField]
		private SpriteShape mainProfile;

		[SerializeField]
		private bool shouldWiggle;

		[SerializeField]
		protected bool bothSide;

		[SerializeField]
		private int startLength;

		[SerializeField]
		private int endLength;

		[SerializeField]
		private int middleLength;

		public SpriteShape MainProfile => mainProfile;

		public int StartLength => startLength;

		public int EndLength => endLength;

		public int MiddleLength => middleLength;

		public virtual void GenerateShape(Spline spline, List<SplinePoint> splinePoints, bool branchToRight)
		{
			for (int i = 0; i < splinePoints.Count; i++)
			{
				spline.InsertPointAt(i, splinePoints[i].Position);
				spline.SetTangentMode(i, ShapeTangentMode.Continuous);
				float directionAngle = GetDirectionAngle(branchToRight, splinePoints[i].Slope);
				int num = (shouldWiggle ? CryptoRandom.Range(-20, 20) : 0);
				Vector2 vector = (directionAngle + (float)num).ToDirectionVector();
				spline.SetRightTangent(i, vector);
				spline.SetLeftTangent(i, -vector);
			}
		}

		public virtual void AdjustHeight(List<SplinePoint> splinePoints, Spline spline)
		{
		}

		public abstract void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints);

		public virtual void ApplySprite(Spline spline, List<SplinePoint> splinePoints)
		{
			for (int i = 0; i < splinePoints.Count; i++)
			{
				if (i == 0)
				{
					spline.SetSpriteIndex(i, 0);
				}
				else if (i == splinePoints.Count - 2)
				{
					spline.SetSpriteIndex(i, 4);
				}
				else if (i == splinePoints.Count - 1)
				{
					spline.SetSpriteIndex(i, 0);
				}
				else
				{
					spline.SetSpriteIndex(i, i % 3 + 1);
				}
			}
		}

		public static float GetDirectionAngle(bool rightBranch, int slope)
		{
			if (!rightBranch)
			{
				slope *= -1;
			}
			return slope switch
			{
				1 => 26.56505f, 
				-1 => -26.56505f, 
				_ => 0f, 
			};
		}
	}

	[Serializable]
	public class EarthBranchFeature : PlatformFeature
	{
		[Header("Props")]
		[SerializeField]
		private OneWayProp propPrefab;

		[SerializeField]
		private int propProbability;

		public override void AdjustHeight(List<SplinePoint> splinePoints, Spline spline)
		{
			float num = 2f;
			int num2 = splinePoints.Count - 1;
			while (num2 > 2 && num > 1f)
			{
				if (num2 == splinePoints.Count - 1)
				{
					spline.SetPosition(num2, splinePoints[num2].Position + Vector2.down);
				}
				spline.SetHeight(num2, num);
				num = Mathf.Max(num - 0.25f, 1f);
				num2--;
			}
		}

		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
			for (int i = 1; i < splinePoints.Count - 2; i++)
			{
				if (CryptoRandom.Range(0, 100) <= propProbability)
				{
					SplinePoint splinePoint = splinePoints[i];
					Vector2 vector = (Vector2)platform.transform.position + splinePoint.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
					UnityEngine.Object.Instantiate(propPrefab, vector, Quaternion.identity, platform.transform).Initialize(splinePoint.Slope);
				}
			}
		}
	}

	[Serializable]
	public class FloatFeature : PlatformFeature
	{
		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
		}
	}

	[Serializable]
	public class BridgeFeature : PlatformFeature
	{
		[Header("Props")]
		[SerializeField]
		private OneWayPropSupport supportPrefab;

		[SerializeField]
		private List<OneWayProp> propPrefabs;

		[SerializeField]
		private int propProbability;

		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
			CreateSupportAt(platform, splinePoints[1], OneWayPropSupport.SupportType.Left);
			CreateSupportAt(platform, splinePoints[splinePoints.Count - 2], OneWayPropSupport.SupportType.Right);
			if (propPrefabs.Count <= 0)
			{
				return;
			}
			foreach (OneWayProp propPrefab in propPrefabs)
			{
				for (int i = 1; i < splinePoints.Count - 2; i++)
				{
					if (CryptoRandom.Range(0, 100) <= propProbability)
					{
						SplinePoint splinePoint = splinePoints[i];
						Vector2 vector = (Vector2)platform.transform.position + splinePoint.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
						UnityEngine.Object.Instantiate(propPrefab, vector, Quaternion.identity, platform.transform).Initialize(splinePoint.Slope);
					}
				}
			}
		}

		private void CreateSupportAt(OneWayPlatform platform, SplinePoint point, OneWayPropSupport.SupportType type)
		{
			Vector2 vector = (Vector2)platform.transform.position + point.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
			OneWayPropSupport oneWayPropSupport = UnityEngine.Object.Instantiate(supportPrefab, vector, Quaternion.identity, platform.transform);
			oneWayPropSupport.SetType(type);
			oneWayPropSupport.Initialize(point.Slope);
		}
	}

	[Serializable]
	public class FillBridgeFeature : PlatformFeature
	{
		[Header("Supports")]
		[SerializeField]
		private int supportDistance;

		[SerializeField]
		private int margin;

		[Header("Props")]
		[SerializeField]
		private OneWayPropSupport supportPrefab;

		[SerializeField]
		private OneWayPropBottom bottomDecorationPrefab;

		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
			if (platform.Points.Count > 1)
			{
				DebugLog.GeneralLogError("FillBridge는 1개의 Point만 가질 수 있습니다.");
				return;
			}
			int length = platform.Points[0].Length;
			int num = Mathf.FloorToInt(((float)length - (float)(2 * margin)) / (float)supportDistance) + 1;
			for (int i = 0; i < num; i++)
			{
				SplinePoint point = new SplinePoint(new Vector2(i * supportDistance + margin, 0f), platform.Points[0].Slope);
				OneWayPropSupport.SupportType type = ((i == num - 1) ? OneWayPropSupport.SupportType.Middle : OneWayPropSupport.SupportType.Left);
				CreateSupportAt(platform, point, type);
			}
			if (!bottomDecorationPrefab.IsNull())
			{
				CreateBottomDecoration(platform, length);
			}
		}

		private void CreateSupportAt(OneWayPlatform platform, SplinePoint point, OneWayPropSupport.SupportType type)
		{
			Vector2 vector = (Vector2)platform.transform.position + point.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
			OneWayPropSupport oneWayPropSupport = UnityEngine.Object.Instantiate(supportPrefab, vector, Quaternion.identity, platform.transform);
			oneWayPropSupport.SetType(type);
			oneWayPropSupport.Initialize(point.Slope);
		}

		private void CreateBottomDecoration(OneWayPlatform platform, int length)
		{
			Vector2 vector = (Vector2)platform.transform.position + new Vector2((float)length / 2f, 0f);
			RaycastHit2D raycastHit2D = Physics2D.Raycast(vector, Vector2.down, 100f, LayerUtility.PlatformLayer);
			if (raycastHit2D.collider.IsNull())
			{
				DebugLog.GeneralLogWarning("FillBridgeFeature의 바닥 데코레이션을 둘 플랫폼이 너무 멀거나 없습니다.");
				return;
			}
			Vector2 vector2 = vector + new Vector2(0f, 0f - raycastHit2D.distance);
			UnityEngine.Object.Instantiate(bottomDecorationPrefab, vector2, Quaternion.identity, platform.transform).InitializeWithLength(length);
		}
	}

	[Serializable]
	public class HangingFeature : PlatformFeature
	{
		[Header("Props")]
		[SerializeField]
		private List<OneWayProp> propPrefabs;

		[SerializeField]
		private int propProbability;

		[SerializeField]
		private OneWayPropRope ropePrefab;

		[SerializeField]
		private bool hangingBothSide;

		public override void ApplySprite(Spline spline, List<SplinePoint> splinePoints)
		{
			for (int i = 0; i < splinePoints.Count - 1; i++)
			{
				if (i == 0)
				{
					spline.SetSpriteIndex(i, 0);
				}
				else if (i == splinePoints.Count - 2)
				{
					spline.SetSpriteIndex(i, 4);
				}
				else
				{
					spline.SetSpriteIndex(i, (i - 1) % 3 + 1);
				}
			}
		}

		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
			if (platform.Points.Count > 1)
			{
				Debug.LogError("HangingWood는 1개의 Point만 가질 수 있습니다.");
				return;
			}
			if (propPrefabs.Count > 0)
			{
				foreach (OneWayProp propPrefab in propPrefabs)
				{
					for (int i = 1; i < splinePoints.Count - 2; i++)
					{
						if (CryptoRandom.Range(0, 100) <= propProbability)
						{
							SplinePoint splinePoint = splinePoints[i];
							Vector2 vector = (Vector2)platform.transform.position + splinePoint.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
							UnityEngine.Object.Instantiate(propPrefab, vector, Quaternion.identity, platform.transform).Initialize(splinePoint.Slope);
						}
					}
				}
			}
			if (hangingBothSide)
			{
				Vector2 vector2 = platform.transform.position;
				OneWayPlatform.Point point = platform.Points[0];
				{
					foreach (Vector2 item in new List<Vector2>
					{
						vector2 + (float)point.Length * 0.25f * (platform.BranchToRight ? new Vector2(1f, 0f) : new Vector2(-1f, 0f)),
						vector2 + (float)point.Length * 0.75f * (platform.BranchToRight ? new Vector2(1f, 0f) : new Vector2(-1f, 0f))
					})
					{
						UnityEngine.Object.Instantiate(ropePrefab, item, Quaternion.identity, platform.transform).Initialize(splinePoints[0].Slope);
					}
					return;
				}
			}
			OneWayPlatform.Point point2 = platform.Points[0];
			Vector2 vector3 = (Vector2)platform.transform.position + (float)point2.Length / 2f * (platform.BranchToRight ? new Vector2(1f, 0f) : new Vector2(-1f, 0f));
			UnityEngine.Object.Instantiate(ropePrefab, vector3, Quaternion.identity, platform.transform).Initialize(splinePoints[0].Slope);
		}
	}

	[Serializable]
	public class BranchFeature : PlatformFeature
	{
		[Header("Props")]
		[SerializeField]
		private OneWayProp rootPropPrefab;

		[SerializeField]
		private List<OneWayProp> propPrefabs;

		[SerializeField]
		private int propProbability;

		public override void ApplySprite(Spline spline, List<SplinePoint> splinePoints)
		{
			for (int i = 0; i < splinePoints.Count - 1; i++)
			{
				if (i == 0)
				{
					spline.SetSpriteIndex(i, 0);
				}
				else
				{
					spline.SetSpriteIndex(i, (i - 1) % 3 + 1);
				}
			}
		}

		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
			if (!rootPropPrefab.IsNull())
			{
				SplinePoint splinePoint = splinePoints[splinePoints.Count - 1];
				Vector2 vector = (Vector2)platform.transform.position + splinePoint.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
				UnityEngine.Object.Instantiate(rootPropPrefab, vector, Quaternion.identity, platform.transform).Initialize(splinePoint.Slope);
				if (bothSide)
				{
					SplinePoint splinePoint2 = splinePoints[0];
					vector = (Vector2)platform.transform.position + splinePoint2.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
					OneWayProp oneWayProp = UnityEngine.Object.Instantiate(rootPropPrefab, vector, Quaternion.identity, platform.transform);
					oneWayProp.Initialize(splinePoint2.Slope);
					Vector3 localScale = oneWayProp.transform.localScale;
					localScale.x *= -1f;
					oneWayProp.transform.localScale = localScale;
				}
			}
			for (int i = 1; i < splinePoints.Count - 2; i++)
			{
				if (CryptoRandom.Range(0, 100) <= propProbability)
				{
					OneWayProp original = propPrefabs[CryptoRandom.Range(0, propPrefabs.Count)];
					SplinePoint splinePoint3 = splinePoints[i];
					Vector2 vector2 = (Vector2)platform.transform.position + splinePoint3.Position * (platform.BranchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
					UnityEngine.Object.Instantiate(original, vector2, Quaternion.identity, platform.transform).Initialize(splinePoint3.Slope);
				}
			}
		}
	}

	[Serializable]
	public class StairWayFeature : PlatformFeature
	{
		public override void GenerateShape(Spline spline, List<SplinePoint> splinePoints, bool branchToRight)
		{
			Vector2 vector = splinePoints[splinePoints.Count - 1].Position.ToVector2Int() * (branchToRight ? new Vector2(1f, 1f) : new Vector2(-1f, 1f));
			Vector2 item = ((0f < vector.x) ? Vector2.zero : vector) + new Vector2(0.5f, 0f);
			Vector2 item2 = ((0f < vector.x) ? vector : Vector2.zero) + new Vector2(-0.5f, -0.5f);
			List<Vector2> list = new List<Vector2>
			{
				item,
				item2,
				new Vector2(item2.x, Mathf.Min(item.y, item2.y) - 1f),
				new Vector2(item.x, Mathf.Min(item.y, item2.y) - 1f)
			};
			for (int i = 0; i < list.Count; i++)
			{
				spline.InsertPointAt(i, list[i]);
				spline.SetTangentMode(i, ShapeTangentMode.Linear);
			}
		}

		public override void CreateBranches(OneWayPlatform platform, List<SplinePoint> splinePoints)
		{
		}

		public override void ApplySprite(Spline spline, List<SplinePoint> splinePoints)
		{
		}
	}

	public struct SplinePoint
	{
		public readonly Vector2 Position;

		public readonly int Slope;

		public SplinePoint(Vector2 position, int slope)
		{
			Position = position;
			Slope = slope;
		}
	}

	public enum PlatformType
	{
		StairWay,
		TreeBranch,
		WoodBridge,
		DarkWoodBridge,
		BurnedWoodBridge,
		CastleBridge,
		WindBridge,
		MoldBridge,
		SolinLabBridge,
		StairWayBridge,
		WoodBridgeOneSide,
		WoodBridgeBothSide,
		BurnedBranchOneSide,
		BurnedBranchBothSide,
		CastleBridgeOneSide,
		CastleBridgeBothSide,
		MoldBridgeOneSide,
		MoldBridgeBothSide
	}

	[SerializeField]
	private BridgeFeature woodBridge;

	[SerializeField]
	private BridgeFeature darkWoodBridge;

	[SerializeField]
	private BridgeFeature burnedWoodBridge;

	[SerializeField]
	private BridgeFeature castleBridge;

	[SerializeField]
	private BridgeFeature windBridge;

	[SerializeField]
	private BridgeFeature moldBridge;

	[SerializeField]
	private FloatFeature solinLabBridge;

	[SerializeField]
	private HangingFeature hangingWood;

	[SerializeField]
	private HangingFeature hangingDarkWood;

	[SerializeField]
	private HangingFeature hangingBurnedWood;

	[SerializeField]
	private HangingFeature hangingCastleBridge;

	[SerializeField]
	private EarthBranchFeature earthBranch;

	[SerializeField]
	private BranchFeature woodBridgeOneSide;

	[SerializeField]
	private BranchFeature woodBridgeBothSide;

	[SerializeField]
	private BranchFeature fireBranchFlatOne;

	[SerializeField]
	private BranchFeature fireBranchFlatBoth;

	[SerializeField]
	private BranchFeature castleBridgeOneSide;

	[SerializeField]
	private BranchFeature castleBridgeBothSide;

	[SerializeField]
	private BranchFeature moldBridgeOneSide;

	[SerializeField]
	private BranchFeature moldBridgeBothSide;

	[SerializeField]
	private StairWayFeature stairWay;

	[SerializeField]
	private FillBridgeFeature stairWayBridge;

	[HideInInspector]
	public PlatformType cachedType;

	private static OneWayGraphicHelper instance;

	public static OneWayGraphicHelper Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Resources.Load<OneWayGraphicHelper>("OneWayGraphicHelper");
			}
			return instance;
		}
	}

	public PlatformFeature GetFeature(PlatformType platformType)
	{
		return platformType switch
		{
			PlatformType.StairWay => stairWay, 
			PlatformType.TreeBranch => earthBranch, 
			PlatformType.WoodBridge => woodBridge, 
			PlatformType.DarkWoodBridge => darkWoodBridge, 
			PlatformType.BurnedWoodBridge => burnedWoodBridge, 
			PlatformType.CastleBridge => castleBridge, 
			PlatformType.WindBridge => windBridge, 
			PlatformType.MoldBridge => moldBridge, 
			PlatformType.SolinLabBridge => solinLabBridge, 
			PlatformType.StairWayBridge => stairWayBridge, 
			PlatformType.WoodBridgeOneSide => woodBridgeOneSide, 
			PlatformType.WoodBridgeBothSide => woodBridgeBothSide, 
			PlatformType.BurnedBranchOneSide => fireBranchFlatOne, 
			PlatformType.BurnedBranchBothSide => fireBranchFlatBoth, 
			PlatformType.CastleBridgeOneSide => castleBridgeOneSide, 
			PlatformType.CastleBridgeBothSide => castleBridgeBothSide, 
			PlatformType.MoldBridgeOneSide => moldBridgeOneSide, 
			PlatformType.MoldBridgeBothSide => moldBridgeBothSide, 
			_ => throw new ArgumentOutOfRangeException("platformType", platformType, null), 
		};
	}
}
