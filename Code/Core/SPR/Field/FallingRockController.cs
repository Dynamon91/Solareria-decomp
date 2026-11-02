using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LapinLibrary;
using Library;
using UnityEngine;

namespace SPR.Field;

public class FallingRockController : MonoBehaviour
{
	[Serializable]
	public struct Wave
	{
		[SerializeField]
		private List<Vector2> localPositions;

		[SerializeField]
		private Color gizmoColor;

		public Color GizmoColor => gizmoColor;

		public IReadOnlyList<Vector2> LocalPositions => localPositions.OrderBy((Vector2 pos) => pos.y).ToList();

		public void OnValidate()
		{
			for (int i = 0; i < localPositions.Count; i++)
			{
				Vector2 vector = localPositions[i];
				localPositions[i] = new Vector2(vector.x, Mathf.Max(0f, vector.y));
			}
		}
	}

	[SerializeField]
	private List<Wave> waves;

	[SerializeField]
	private int waveCycleFrame;

	[Header("Rock")]
	[SerializeField]
	private float moveSpeed;

	private Area myArea;

	private CancellationTokenSource tokenSource;

	private int currentFrame;

	private int waveIndex;

	private void OnValidate()
	{
		if (!waves.IsNullOrEmpty())
		{
			waves.ForEach(delegate(Wave wave)
			{
				wave.OnValidate();
			});
		}
	}

	private void Start()
	{
		waveIndex = 0;
		myArea = GetComponentInParent<Area>();
		float num = (float)waveCycleFrame * Time.fixedDeltaTime * moveSpeed;
		Vector3 position = base.transform.position;
		Vector3 pos = position;
		while (pos.y > (float)myArea.AreaBox.LowerLeft.y)
		{
			for (int num2 = waves.Count - 1; num2 >= 0; num2--)
			{
				Wave wave = waves[num2];
				position += Vector3.down * num;
				for (int i = 0; i < wave.LocalPositions.Count; i++)
				{
					Vector2 vector = wave.LocalPositions[i];
					pos = position + (Vector3)vector;
					Game.Instance.ObjectPoolManager.GetObject<FallingRock>("Falling Rock.prefab", pos, base.transform).Initialize(moveSpeed, myArea);
				}
			}
		}
	}

	private void FixedUpdate()
	{
		if (currentFrame == 0)
		{
			CreateRocks(waveIndex);
			waveIndex++;
			waveIndex %= waves.Count;
		}
		currentFrame++;
		currentFrame %= waveCycleFrame;
	}

	private void CreateRocks(int waveIndex)
	{
		CancellationToken cancellationToken = TaskUtil.RefreshToken(ref tokenSource);
		IReadOnlyList<Vector2> localPositions = waves[waveIndex].LocalPositions;
		for (int i = 0; i < localPositions.Count; i++)
		{
			Vector2 vector = localPositions[i];
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			Vector3 pos = base.transform.position + (Vector3)vector;
			Game.Instance.ObjectPoolManager.GetObject<FallingRock>("Falling Rock.prefab", pos, base.transform).Initialize(moveSpeed, myArea);
		}
	}

	private void OnDisable()
	{
		tokenSource?.Cancel();
	}
}
