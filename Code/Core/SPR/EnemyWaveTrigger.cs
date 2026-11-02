using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using UnityEngine;

namespace SPR;

[RequireComponent(typeof(PolygonCollider2D))]
public class EnemyWaveTrigger : DoorTrigger, IStateSavable, IVcamConfiner
{
	public enum CompleteOption
	{
		Default,
		Timer,
		Enemy
	}

	private enum State
	{
		Ready,
		Trapped,
		Clear
	}

	[SerializeField]
	private CinemachineVirtualCamera vcam;

	[SerializeField]
	private AreaBox areaBox;

	[SerializeField]
	private Vector2 soulDropPosition;

	[SerializeField]
	private SoulDropInfo soulDropInfo;

	[SerializeField]
	private CompleteOption completeOption;

	[SerializeField]
	private int timerStartWave;

	[SerializeField]
	private int timerFrame;

	[SerializeField]
	private EnemyPlaceHolder specificHolder;

	[SerializeField]
	[ReadOnlyInspector]
	private List<EnemyWave> waves;

	[SerializeField]
	private SoundManager.BGMType bgmType;

	private Enemy specificEnemy;

	private FrameTimer frameTimer;

	private State currentState;

	private int killCount;

	private bool isTimerStarted;

	private PolygonCollider2D polygonColl;

	private CinemachineBrain cineBrain;

	private Area area;

	private const float BgmDelay = 1.6f;

	private CinemachineImpulseSource impulseSource;

	protected override Vector3 TriggerSize => areaBox.Size.ToVector3();

	protected override Vector3 TriggerCenter => base.transform.position + (Vector3)areaBox.Center;

	private Vector2 PlayerDropPosition => (Vector2)TriggerCenter + soulDropPosition;

	private PolygonCollider2D PolygonCollider => polygonColl ?? (polygonColl = GetComponent<PolygonCollider2D>());

	private CinemachineBrain CineBrain => cineBrain ?? (cineBrain = GraphicHelper.MainCamera.GetComponent<CinemachineBrain>());

	private Area MyArea => area ?? (area = GetComponentInParent<Area>());

	public AreaBox AreaBox => areaBox;

	public bool IsDone => currentState == State.Clear;

	public string ID => $"{base.name}_{Game.Instance.CurrentFieldID}_{base.transform.position}";

	public int CurrentWaveIndex { get; private set; }

	public EnemyWave CurrentWave => waves[CurrentWaveIndex - 1];

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private CinemachineImpulseSource ImpulseSource => impulseSource ?? (impulseSource = GetComponent<CinemachineImpulseSource>());

	public bool ConfineLookAround => true;

	public void LoadState(int state)
	{
		currentState = ((state != 0) ? State.Clear : State.Ready);
		waves.Clear();
		waves.AddRange(base.transform.GetComponentsInChildren<EnemyWave>());
		int num = 1;
		foreach (EnemyWave wave in waves)
		{
			wave.Initialize(currentState == State.Ready, num++);
		}
		switch (currentState)
		{
		case State.Ready:
		{
			vcam.Follow = Game.Instance.Player.transform;
			if (!soulDropInfo.IsValid)
			{
				MyArea.SetSoulDropPosition(active: false);
			}
			Door[] componentsInChildren = GetComponentsInChildren<Door>(includeInactive: true);
			if (!componentsInChildren.IsNullOrEmpty())
			{
				InitializeDoors(componentsInChildren, open: true);
			}
			else
			{
				InstantiateDoor(openOnAwake: true, "Earthford Branch Door.prefab");
				List<Vector3> doorSizes = GetDoorSizes();
				for (int num2 = 0; num2 < ConnectedDoors.Count; num2++)
				{
					Door door = ConnectedDoors[num2];
					Vector3 vector = doorSizes[num2];
					Vector2 collSize = door.CollSize;
					collSize.y = vector.y;
					door.SetColliderSize(collSize);
				}
			}
			IsOpen = true;
			killCount = 0;
			if (completeOption == CompleteOption.Enemy)
			{
				specificHolder.AddInstantiateCallback(delegate(Enemy x)
				{
					specificEnemy = x;
				});
			}
			break;
		}
		case State.Clear:
			GetComponentsInChildren<SpineDoor>(includeInactive: true).ForEach(delegate(SpineDoor spineDoor)
			{
				spineDoor.OpenLoop();
			});
			break;
		}
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public override void InteractEnter()
	{
		if (currentState == State.Ready)
		{
			EnterAsync().Forget();
			SoundManager.PlayEnemyWaveBGM(bgmType);
			if (soulDropInfo.IsValid)
			{
				Game.Instance.Player.SetSoulDropPosition(soulDropInfo);
				Game.Instance.Player.SetSpecialDropInfo(value: true);
			}
			else
			{
				MyArea.SetSoulDropPosition(active: true, PlayerDropPosition);
			}
		}
	}

	public override void InteractExit()
	{
		if (!Game.Instance.Player.isDead && currentState != State.Trapped)
		{
			if (soulDropInfo.IsValid)
			{
				Game.Instance.Player.SetSoulDropPosition(default(SoulDropInfo).None);
				Game.Instance.Player.SetSpecialDropInfo(value: false);
			}
			else
			{
				MyArea.SetSoulDropPosition(active: false);
			}
		}
	}

	public void ActivateWave(int waveNum)
	{
		CurrentWaveIndex = waveNum;
		if (waveNum > 0)
		{
			waves[waveNum - 1].SetStartPosition(AreaBox);
			waves[waveNum - 1].ActivateWave();
		}
		foreach (EnemyWave wave in waves)
		{
			wave.OnAnyWaveActivated(waveNum);
		}
		if (completeOption == CompleteOption.Timer && waveNum == timerStartWave)
		{
			frameTimer = new FrameTimer(timerFrame);
			isTimerStarted = true;
		}
		SoundManager.SetEnemyWaveParameter(CurrentWaveIndex, waves.Count);
	}

	public void UpdateKillCount(int delta)
	{
		killCount += delta;
		foreach (EnemyWave wave in waves)
		{
			wave.OnEnemyKilled(killCount);
		}
		if (completeOption == CompleteOption.Enemy && !IsOpen && specificEnemy.Health <= 0)
		{
			ClearOpenDoor();
		}
	}

	private void Update()
	{
		if (currentState == State.Trapped && !waves.Any((EnemyWave x) => x.CurrentState != EnemyWave.WaveState.Finish))
		{
			currentState = State.Clear;
			ChangeState(1);
			if (completeOption == CompleteOption.Default)
			{
				ClearOpenDoor();
			}
		}
	}

	private void FixedUpdate()
	{
		if (currentState == State.Trapped && completeOption == CompleteOption.Timer && isTimerStarted && !frameTimer.IsDone)
		{
			frameTimer.Tick();
			if (frameTimer.IsDone)
			{
				ClearOpenDoor();
			}
		}
	}

	private void ClearOpenDoor()
	{
		OpenDoors();
		SetActiveVirtualCamera(active: false);
		if (soulDropInfo.IsValid)
		{
			Game.Instance.Player.SetSoulDropPosition(default(SoulDropInfo).None);
			Game.Instance.Player.SetSpecialDropInfo(value: false);
		}
		else
		{
			MyArea.SetSoulDropPosition(active: false);
		}
		if (bgmType != SoundManager.BGMType.Library)
		{
			SoundManager.StopBGM();
			UniTask.WaitForSeconds(1.6f).GetAwaiter().OnCompleted(delegate
			{
				SoundManager.PlayBGM(MyArea.SoundKey);
			});
		}
	}

	private void OpenDoors()
	{
		ConnectedDoors.ForEach(delegate(Door door)
		{
			door.SetDoorOpen(open: true);
		});
		IsOpen = true;
	}

	private void SetActiveVirtualCamera(bool active)
	{
		if (active)
		{
			vcam.Priority = CineBrain.ActiveVirtualCamera.Priority + 1;
		}
		vcam.gameObject.SetActive(active);
	}

	private async UniTaskVoid EnterAsync()
	{
		currentState = State.Trapped;
		killCount = 0;
		IsOpen = false;
		ConnectedDoors.ForEach(delegate(Door door)
		{
			door.SetDoorOpen(open: false);
		});
		SetActiveVirtualCamera(active: true);
		await UniTask.WaitWhile(() => CineBrain.IsBlending);
		ImpulseSource?.GenerateImpulse();
		if (ConnectedDoors[0] is SpineDoor spineDoor)
		{
			await UniTaskExtension.WaitForSeconds(spineDoor.CloseAnimDuration);
		}
		ActivateWave(0);
		UpdateKillCount(0);
	}

	protected override void OnDrawGizmos()
	{
		base.OnDrawGizmos();
		Gizmos.color = Color.yellow;
		Gizmos.DrawCube(PlayerDropPosition, Vector2.one);
	}
}
