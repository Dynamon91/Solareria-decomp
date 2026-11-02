using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.Timeline;
using UnityEngine;

namespace SPR.Field;

public class LegendaryArea : SubArea
{
	[SerializeField]
	protected bool shouldDisableZone;

	[SerializeField]
	private GameObject[] transparentWalls;

	[SerializeField]
	protected GameObject[] areaChangers;

	[Header("Timeline")]
	[SerializeField]
	protected LegendaryClear legendaryClear;

	private LegendaryBounds legendaryBounds;

	private HighCreature highCreature;

	protected LegendaryBounds LegendaryBounds => legendaryBounds ?? (legendaryBounds = GetComponent<LegendaryBounds>());

	private HighCreature HighCreature => highCreature ?? (highCreature = GetComponentInChildren<HighCreature>());

	public void OpenWall()
	{
		SetWalls(value: false);
	}

	public async UniTaskVoid PlayClearTimeline(Vector3 position)
	{
		legendaryClear.PlayTimeline(position);
		if (shouldDisableZone)
		{
			FieldController.ChangeAreaTo(FieldController.MainArea);
			GameObject[] array = areaChangers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
			await UniTask.WaitWhile(() => GraphicHelper.MainCamera.GetComponent<CinemachineBrain>().IsBlending || legendaryClear.IsPlaying);
		}
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		TryDisableArea();
		LegendaryBounds.OnEnter += OnEnterLegendary;
		LegendaryBounds.OnExit += OnExitLegendary;
		SetWalls(value: false);
	}

	protected void TryDisableArea()
	{
		Vector3 position = GetComponentInChildren<EnemyPlaceHolder>().transform.position;
		if (Game.Instance.SaveData.WorldData.IsEliminatedCreature(Game.Instance.CurrentFieldID, position) && shouldDisableZone)
		{
			GameObject[] array = areaChangers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
		}
	}

	protected virtual void OnEnterLegendary()
	{
		SetWalls(value: true);
		if ((object)HighCreature != null && !HighCreature.IsDead)
		{
			HighCreature.StateMachine.ChangeState(HighCreature.DefaultState);
		}
	}

	protected virtual void OnExitLegendary()
	{
		if ((object)HighCreature != null && !HighCreature.IsDead)
		{
			HighCreature.StateMachine.ChangeState(HighCreature.DefaultState);
		}
	}

	protected void SetWalls(bool value)
	{
		if (!transparentWalls.IsNullOrEmpty())
		{
			GameObject[] array = transparentWalls;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value);
			}
		}
	}
}
