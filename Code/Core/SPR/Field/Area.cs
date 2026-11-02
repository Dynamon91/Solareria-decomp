using System.Collections.Generic;
using Cinemachine;
using Library;
using SPR.Cutscene;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class Area : MonoBehaviour, IVcamConfiner
{
	[SerializeField]
	protected AreaBox areaBox;

	[SerializeField]
	protected CinemachineVirtualCamera vcam;

	[SerializeField]
	protected string soundKey;

	[SerializeField]
	private bool usePlayerDropPosition;

	[SerializeField]
	private Vector2 playerDropPosition;

	private PolygonCollider2D confiner;

	private CutsceneProducer cutsceneProducer;

	private float xDamping;

	private float yDamping;

	private float deadZoneWidth;

	private bool hasChangedComponent;

	protected List<IStateSavable> stateSavables = new List<IStateSavable>();

	private bool useSpecialDropPosition;

	private Vector2 specialDropPosition;

	protected FieldController FieldController;

	public AreaBox AreaBox => areaBox;

	public CinemachineVirtualCamera Vcam => vcam;

	public virtual PolygonCollider2D ConfineCollider => confiner ?? (confiner = GetComponent<PolygonCollider2D>());

	public virtual bool ConfineLookAround => true;

	public Vector2 PlayerDropPosition
	{
		get
		{
			if (!usePlayerDropPosition)
			{
				if (!useSpecialDropPosition)
				{
					return Game.Instance.Player.mover.LastGroundPosition + Vector2.up;
				}
				return specialDropPosition;
			}
			return areaBox.Center + playerDropPosition;
		}
	}

	public string SoundKey => soundKey;

	protected virtual bool PlayBGMCondition
	{
		get
		{
			if (Game.FieldID.SectorName != SectorName.Library || soundKey.Equals("BossVerita"))
			{
				return true;
			}
			if (VeritaCutsceneProducer.IsVeritaInfected)
			{
				return Game.Instance.SaveData.EnemyFightData.IsBossCleared(BossName.Verita);
			}
			return true;
		}
	}

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsInArea(Vector2 pos)
	{
		return areaBox.IsInArea(pos);
	}

	public void Initialize(FieldController fieldController)
	{
		FieldController = fieldController;
		vcam.gameObject.SetActive(value: false);
		vcam.Follow = Game.Instance.Player.transform;
		CinemachineFramingTransposer cinemachineComponent = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
		xDamping = cinemachineComponent.m_XDamping;
		yDamping = cinemachineComponent.m_YDamping;
		deadZoneWidth = cinemachineComponent.m_DeadZoneWidth;
		stateSavables.Clear();
		stateSavables.AddRange(GetComponentsInChildren<IStateSavable>());
		stateSavables.ForEach(delegate(IStateSavable s)
		{
			s.InitializeState();
		});
		useSpecialDropPosition = false;
		OnInitialize();
	}

	protected virtual void OnInitialize()
	{
	}

	public virtual void OnAreaEnter()
	{
		vcam.gameObject.SetActive(value: true);
		(string, string) tuple = SoundManager.SelectSoundKey(soundKey);
		if (PlayBGMCondition)
		{
			SoundManager.PlayBGM(tuple.Item1);
		}
		else
		{
			SoundManager.StopBGM();
		}
		SoundManager.PlayAMB(tuple.Item2);
		if (Game.Instance.CurrentFieldID.SectorName == SectorName.Earthford)
		{
			int sceneIndex = Game.Instance.CurrentFieldID.SceneIndex;
			if (sceneIndex >= 90 && sceneIndex != 93)
			{
				SoundManager.SetFadeBGMVolume(0.7f);
			}
			else
			{
				SoundManager.SetFadeBGMVolume(1f);
			}
		}
	}

	public virtual void OnAreaExit()
	{
		vcam.gameObject.SetActive(value: false);
		SetVirtualCameraForMovingSight(active: false);
	}

	public bool TryGetCutsceneProducer(out CutsceneProducer producer)
	{
		if (!cutsceneProducer.IsNull())
		{
			producer = cutsceneProducer;
			return true;
		}
		cutsceneProducer = GetComponentInChildren<CutsceneProducer>();
		producer = cutsceneProducer;
		return !cutsceneProducer.IsNull();
	}

	public bool TryGetStateSavableObject<T>(out T target) where T : IStateSavable
	{
		target = default(T);
		foreach (IStateSavable stateSavable in stateSavables)
		{
			if (stateSavable is T val)
			{
				target = val;
				return true;
			}
		}
		return false;
	}

	protected virtual void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(areaBox.Center, areaBox.Size.ToVector2());
		DrawPlayerDropPosition();
	}

	protected void DrawPlayerDropPosition()
	{
		if (usePlayerDropPosition)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawCube(PlayerDropPosition, Vector2.one);
		}
	}

	public void SetVirtualCameraForMovingSight(bool active)
	{
		if ((active || hasChangedComponent) && vcam.GetCinemachineComponent(CinemachineCore.Stage.Body) is CinemachineFramingTransposer cinemachineFramingTransposer)
		{
			if (active)
			{
				cinemachineFramingTransposer.m_XDamping = 0.1f;
				cinemachineFramingTransposer.m_YDamping = 0.1f;
				cinemachineFramingTransposer.m_DeadZoneWidth = 0f;
			}
			else
			{
				cinemachineFramingTransposer.m_XDamping = xDamping;
				cinemachineFramingTransposer.m_YDamping = yDamping;
				cinemachineFramingTransposer.m_DeadZoneWidth = deadZoneWidth;
			}
			hasChangedComponent = active;
		}
	}

	public float GetClampCameraX(CinemachineVirtualCamera target, float margin = 0f)
	{
		Camera main = Camera.main;
		float num = target.m_Lens.OrthographicSize * (float)main.pixelWidth / (float)main.pixelHeight;
		float num2 = (float)AreaBox.LowerLeft.x + num + margin;
		float num3 = (float)AreaBox.UpperRight.x - num - margin;
		if (!(num2 >= num3))
		{
			return Mathf.Clamp(target.transform.position.x, num2, num3);
		}
		return AreaBox.Center.x;
	}

	public float GetClampCameraY(CinemachineVirtualCamera target)
	{
		float orthographicSize = target.m_Lens.OrthographicSize;
		float num = (float)AreaBox.LowerLeft.y + orthographicSize;
		float num2 = (float)AreaBox.UpperRight.y - orthographicSize;
		if (!(num >= num2))
		{
			return Mathf.Clamp(target.transform.position.y, num, num2);
		}
		return AreaBox.Center.y;
	}

	public Vector3 ClampCamera(CinemachineVirtualCamera target)
	{
		return new Vector3(GetClampCameraX(target), GetClampCameraY(target), target.transform.position.z);
	}

	public void SetSoulDropPosition(bool active, Vector2 pos = default(Vector2))
	{
		useSpecialDropPosition = active;
		specialDropPosition = pos;
	}
}
