using System;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class MyosoSupportController : MonoBehaviour
{
	[Header("타이밍 트리거")]
	[SerializeField]
	private MyosoSupportTrigger[] triggers;

	[Header("조력 설정")]
	[SerializeField]
	private int coolTime;

	[SerializeField]
	private int pauseFrame;

	[SerializeField]
	private int dotCycleFrame;

	[SerializeField]
	private float myosoHeightOffset;

	[Header("References")]
	[SerializeField]
	private MyosoCombat myoso;

	private EnemyWaveTrigger enemyWaveTrigger;

	private int currentWave;

	private int frameCount;

	private int currentTiming;

	private FrameTimer timer;

	private EnemyWaveTrigger EnemyWaveTrigger => enemyWaveTrigger ?? GetComponent<EnemyWaveTrigger>();

	private void FixedUpdate()
	{
		if (EnemyWaveTrigger.CurrentWaveIndex > 0)
		{
			if (currentWave != EnemyWaveTrigger.CurrentWaveIndex)
			{
				currentWave = EnemyWaveTrigger.CurrentWaveIndex;
				frameCount = 0;
				currentTiming = 0;
			}
			if (currentWave - 1 < triggers.Length && currentTiming < triggers[currentWave - 1].Timing.Length && frameCount >= triggers[currentWave - 1].Timing[currentTiming].Timing)
			{
				MyosoAppear(triggers[currentWave - 1].Timing[currentTiming].TargetType);
				currentTiming++;
			}
			frameCount++;
		}
	}

	private void MyosoAppear(TargetType type)
	{
		Enemy enemy = null;
		EnemyWave enemyWave = EnemyWaveTrigger.CurrentWave;
		if (enemyWave.AliveEnemyCount > 0)
		{
			enemy = type switch
			{
				TargetType.Random => enemyWave.GetRandomEnemy(), 
				TargetType.WindAssassin => enemyWave.GetRandomEnemy<WindSpiritAssassin>(), 
				TargetType.WindKnight => enemyWave.GetRandomEnemy<WindSpiritKnight>(), 
				TargetType.InfectedTreeGuard => enemyWave.GetRandomEnemy<InfectedTreeGuard>(), 
				_ => throw new ArgumentOutOfRangeException("type", type, null), 
			};
			if ((object)enemy != null)
			{
				AreaBox areaBox = GetComponent<EnemyWaveTrigger>().AreaBox;
				float num = (float)areaBox.Size.x / 4f;
				Vector2 vector = (Vector2)base.transform.position + areaBox.Center;
				vector.y += myosoHeightOffset;
				Vector2 vector2 = new Vector2(base.transform.position.x + (float)areaBox.LowerLeft.x + num, vector.y);
				Vector2 vector3 = new Vector2(base.transform.position.x + (float)areaBox.UpperRight.x - num, vector.y);
				Vector2 position = ((enemy.Position.x > vector.x) ? vector2 : vector3);
				myoso.Position = position;
				myoso.Appear(new CreaturePierceState(enemy, new HitInfo(myoso, "Creature"), new HitInfo(myoso, "Creature_DOT"), pauseFrame, dotCycleFrame)).Forget();
			}
		}
	}
}
