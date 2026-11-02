using System;
using Cysharp.Threading.Tasks;
using SPR.Save;
using UnityEngine;

namespace SPR.Cutscene;

public class SpecialTalkTrigger : CutsceneTrigger, IStateSavable
{
	public enum State
	{
		None,
		Triggered
	}

	[Header("대화 설정")]
	[SerializeField]
	private string id;

	[SerializeField]
	private float duration;

	[SerializeField]
	private float waitDuration;

	[SerializeField]
	private float fadeDuration;

	public string ID => base.name;

	private void Awake()
	{
		base.OnTriggered += Show;
	}

	public void LoadState(int state)
	{
		switch ((State)state)
		{
		case State.None:
			base.gameObject.SetActive(value: true);
			break;
		case State.Triggered:
			base.gameObject.SetActive(value: false);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public void ChangeState(int state)
	{
		LoadState(state);
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void Show()
	{
		Game.Instance.UIManager.TalkCanvas.ShowSpecialTalk(id, duration, fadeDuration).Forget();
		ChangeState(1);
	}
}
