using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class EnemyWave : MonoBehaviour
{
	public enum StartOption
	{
		KillCount,
		Timer
	}

	public enum WaveState
	{
		Ready,
		Check,
		Play,
		Finish
	}

	[Serializable]
	private struct TimerEnemyHolder
	{
		[SerializeField]
		private EnemyPlaceHolder holder;

		[SerializeField]
		private int timerFrame;

		public EnemyPlaceHolder Holder => holder;

		public int TimerFrame => timerFrame;
	}

	[Header("시작 조건")]
	[SerializeField]
	private StartOption startOption;

	[SerializeField]
	private int targetWaveIndex;

	[SerializeField]
	private int timerFrame;

	[SerializeField]
	private int killCount;

	[Header("추가 타이머")]
	[SerializeField]
	private List<TimerEnemyHolder> timerEnemyHolders;

	private readonly List<EnemyPlaceHolder> holders = new List<EnemyPlaceHolder>();

	private readonly List<Enemy> enemies = new List<Enemy>();

	private Dictionary<Enemy, int> enemyToTimerFrames;

	private EnemyWaveTrigger waveTrigger;

	private FrameTimer frameTimer;

	private int myWaveNum;

	private int currentKillCount;

	private AreaBox areaBox;

	private ImahenTurretBase[] turrets;

	private readonly HashSet<FieldID> fallWaveFieldIDs = new HashSet<FieldID>
	{
		new FieldID(SectorName.SolinLab, 11)
	};

	public int AliveEnemyCount
	{
		get
		{
			int num = 0;
			foreach (Enemy enemy3 in enemies)
			{
				if (!enemy3.IsDead)
				{
					num++;
				}
			}
			if (enemyToTimerFrames.IsNullOrEmpty())
			{
				return num;
			}
			foreach (var (enemy2, _) in enemyToTimerFrames)
			{
				if (!enemy2.IsDead && enemy2.gameObject.activeSelf)
				{
					num++;
				}
			}
			return num;
		}
	}

	public WaveState CurrentState { get; private set; }

	private EnemyWaveTrigger WaveTrigger => waveTrigger ?? (waveTrigger = base.transform.parent.GetComponent<EnemyWaveTrigger>());

	public void Initialize(bool ready, int num)
	{
		myWaveNum = num;
		CurrentState = ((!ready) ? WaveState.Finish : WaveState.Ready);
		turrets = GetComponentsInChildren<ImahenTurretBase>();
		OnInitialize();
	}

	protected virtual void OnInitialize()
	{
		holders.AddRange(base.transform.GetComponentsInChildren<EnemyPlaceHolder>());
		if (CurrentState == WaveState.Ready)
		{
			if (!timerEnemyHolders.IsNullOrEmpty())
			{
				enemyToTimerFrames = new Dictionary<Enemy, int>(timerEnemyHolders.Count);
				foreach (TimerEnemyHolder pair in timerEnemyHolders)
				{
					holders.Remove(pair.Holder);
					pair.Holder.AddInstantiateCallback(delegate(Enemy x)
					{
						enemyToTimerFrames.Add(x, pair.TimerFrame);
						x.gameObject.SetActive(value: false);
					});
				}
			}
			holders.ForEach(delegate(EnemyPlaceHolder holder)
			{
				holder.AddInstantiateCallback(delegate(Enemy x)
				{
					enemies.Add(x);
					x.gameObject.SetActive(value: false);
				});
			});
			ImahenTurretBase[] array = turrets;
			foreach (ImahenTurretBase imahenTurretBase in array)
			{
				enemies.Add(imahenTurretBase);
				imahenTurretBase.gameObject.SetActive(value: false);
			}
		}
		else
		{
			holders.ForEach(delegate(EnemyPlaceHolder holder)
			{
				holder.gameObject.SetActive(value: false);
			});
			ImahenTurretBase[] array = turrets;
			for (int num = 0; num < array.Length; num++)
			{
				array[num].gameObject.SetActive(value: false);
			}
		}
	}

	public void ActivateWave()
	{
		CurrentState = WaveState.Play;
		OnActivate();
	}

	public void SetStartPosition(AreaBox area)
	{
		areaBox = area;
	}

	protected virtual void OnActivate()
	{
		if (fallWaveFieldIDs.Contains(Game.FieldID))
		{
			foreach (Enemy enemy in enemies)
			{
				if (!(enemy is Creature))
				{
					if (enemy is ImahenTurretBase)
					{
						enemy.gameObject.SetActive(value: true);
					}
				}
				else
				{
					ActivateEnemyWithFalling(enemy);
				}
			}
			if (enemyToTimerFrames.IsNullOrEmpty())
			{
				return;
			}
			{
				foreach (KeyValuePair<Enemy, int> pair in enemyToTimerFrames)
				{
					UniTaskExtension.WaitForFixedUpdatesOrCancel(pair.Value).GetAwaiter().OnCompleted(delegate
					{
						ActivateEnemyWithFalling(pair.Key);
					});
				}
				return;
			}
		}
		foreach (Enemy enemy2 in enemies)
		{
			if (!(enemy2 is Creature))
			{
				if (enemy2 is ImahenTurretBase)
				{
					enemy2.gameObject.SetActive(value: true);
				}
			}
			else
			{
				ActivateEnemy(enemy2);
			}
		}
		if (enemyToTimerFrames.IsNullOrEmpty())
		{
			return;
		}
		foreach (KeyValuePair<Enemy, int> pair2 in enemyToTimerFrames)
		{
			UniTaskExtension.WaitForFixedUpdatesOrCancel(pair2.Value).GetAwaiter().OnCompleted(delegate
			{
				ActivateEnemy(pair2.Key);
			});
		}
	}

	private void ActivateEnemy(Enemy enemy)
	{
		if (enemy is InfectedTreeGuard { IsMain: false })
		{
			return;
		}
		if (enemy.HasAnimation("falling"))
		{
			Vector2 vector = (Vector2)base.transform.position + areaBox.Center;
			Vector2 vector2 = (Vector2)base.transform.position + (Vector2)areaBox.LowerLeft;
			Vector2 vector3 = (Vector2)base.transform.position + (Vector2)areaBox.UpperRight;
			float y = vector.y + (float)CryptoRandom.Range(0, Mathf.RoundToInt((float)areaBox.Size.y / 4f));
			enemy.SetAnimation("falling");
			enemy.transform.position = ((enemy.SpawnPosition.x < vector.x) ? new Vector3(vector2.x, y) : new Vector3(vector3.x, y));
			enemy.LookAt(vector);
			enemy.gameObject.SetActive(value: true);
			if (enemy is Creature creature)
			{
				creature.AddAnimationEvent(SpineObject.AnimState.Complete, creature.OnLandingComplete);
			}
			if (enemy is EarthWarrior earthWarrior)
			{
				earthWarrior.WaveSpawning = true;
			}
			float num = Vector3.Distance(enemy.SpawnPosition, enemy.transform.position);
			enemy.transform.DOJump(enemy.SpawnPosition, 5f, 1, num / 60f).SetEase(Ease.Linear).OnComplete(delegate
			{
				if (!enemy.IsDead)
				{
					enemy.SetAnimation("landing", loop: false);
				}
			});
		}
		else
		{
			ActivateEnemyWithWalk(enemy);
		}
	}

	private void ActivateEnemyWithWalk(Enemy enemy)
	{
		Vector2 vector = (Vector2)base.transform.position + areaBox.Center;
		Vector2 vector2 = (Vector2)base.transform.position + (Vector2)areaBox.LowerLeft;
		Vector2 vector3 = (Vector2)base.transform.position + (Vector2)areaBox.UpperRight;
		float y = enemy.SpawnPosition.y;
		enemy.transform.position = ((enemy.SpawnPosition.x < vector.x) ? new Vector3(vector2.x, y) : new Vector3(vector3.x, y));
		enemy.LookAt(vector);
		enemy.StateMachine.ChangeState(new MoveToState(enemy.CastObject<Creature>(), enemy.SpawnPosition));
		enemy.gameObject.SetActive(value: true);
	}

	private void ActivateEnemyWithFalling(Enemy enemy)
	{
		Vector3 position = enemy.transform.position;
		enemy.transform.position = new Vector3(position.x, areaBox.UpperRight.y);
		enemy.gameObject.SetActive(value: true);
		enemy.StateMachine.ChangeState(new CreatureFallingState((Creature)enemy, enemy.SpawnPosition));
	}

	private void Update()
	{
		if (CurrentState != WaveState.Play)
		{
			return;
		}
		int num = enemies.Count((Enemy x) => x.Health <= 0);
		if (!enemyToTimerFrames.IsNullOrEmpty())
		{
			num += enemyToTimerFrames.Keys.Count((Enemy x) => x.Health <= 0);
		}
		if (num != currentKillCount)
		{
			WaveTrigger.UpdateKillCount(num - currentKillCount);
			currentKillCount = num;
		}
		if (!enemies.Any((Enemy x) => x.Health > 0) && (enemyToTimerFrames.IsNullOrEmpty() || !enemyToTimerFrames.Keys.Any((Enemy x) => x.Health > 0)))
		{
			CurrentState = WaveState.Finish;
		}
	}

	private void FixedUpdate()
	{
		if (CurrentState == WaveState.Check && startOption == StartOption.Timer)
		{
			frameTimer.Tick();
			if (frameTimer.IsDone)
			{
				WaveTrigger.ActivateWave(myWaveNum);
			}
		}
	}

	public void OnAnyWaveActivated(int waveNum)
	{
		if (CurrentState == WaveState.Ready && startOption == StartOption.Timer && waveNum == targetWaveIndex)
		{
			frameTimer = new FrameTimer(timerFrame);
			CurrentState = WaveState.Check;
		}
	}

	public void OnEnemyKilled(int totalKillCount)
	{
		if (CurrentState == WaveState.Ready && startOption == StartOption.KillCount && totalKillCount >= killCount)
		{
			WaveTrigger.ActivateWave(myWaveNum);
		}
	}

	public Enemy GetRandomEnemy()
	{
		List<Enemy> list = new List<Enemy>();
		foreach (Enemy enemy3 in enemies)
		{
			if (!enemy3.IsDead && enemy3.gameObject.activeSelf)
			{
				list.Add(enemy3);
			}
		}
		if (enemyToTimerFrames.IsNullOrEmpty())
		{
			if (list.Count != 0)
			{
				return list[CryptoRandom.Range(0, list.Count)];
			}
			return null;
		}
		foreach (var (enemy2, _) in enemyToTimerFrames)
		{
			if (!enemy2.IsDead && enemy2.gameObject.activeSelf)
			{
				list.Add(enemy2);
			}
		}
		if (list.Count != 0)
		{
			return list[CryptoRandom.Range(0, list.Count)];
		}
		return null;
	}

	public T GetRandomEnemy<T>() where T : Enemy
	{
		foreach (Enemy enemy3 in enemies)
		{
			if (enemy3 is T result && !enemy3.IsDead)
			{
				GameObject gameObject = enemy3.gameObject;
				if ((object)gameObject != null && gameObject.activeSelf)
				{
					return result;
				}
			}
		}
		foreach (var (enemy2, _) in enemyToTimerFrames)
		{
			if (enemy2 is T result2 && !enemy2.IsDead)
			{
				GameObject gameObject = enemy2.gameObject;
				if ((object)gameObject != null && gameObject.activeSelf)
				{
					return result2;
				}
			}
		}
		return (T)GetRandomEnemy();
	}

	public void AddEnemy(Enemy enemy)
	{
		enemies.Add(enemy);
	}
}
