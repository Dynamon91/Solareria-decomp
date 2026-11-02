using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SPR.UI.Title;

public class TitleManager : Manager
{
	[SerializeField]
	private TitlePanel titlePanel;

	[SerializeField]
	private SaveSlotPanel saveSlotPanel;

	[SerializeField]
	private TitleLanguagePanel languagePanel;

	[SerializeField]
	private DifficultyPanel difficultyPanel;

	[SerializeField]
	private TextMeshProUGUI versionText;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public TitlePanel TitlePanel => titlePanel;

	public SaveSlotPanel SaveSlotPanel => saveSlotPanel;

	public TitleLanguagePanel LanguagePanel => languagePanel;

	public DifficultyPanel DifficultyPanel => difficultyPanel;

	private void Awake()
	{
		Game.Instance.SetManager(this);
		string text = "ver." + Application.version;
		versionText.text = text;
		titlePanel.Initialize();
		saveSlotPanel.Initialize();
		difficultyPanel.Initialize();
	}

	protected override void OnInitialize()
	{
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		SoundManager.PlayBGM("Title");
		SoundManager.PlayAMB("Title", withDefault: false);
		return default(UniTask);
	}

	private void OnDestroy()
	{
		Game.Instance.RemoveManager(this);
	}
}
