using Cysharp.Threading.Tasks;
using SPR.Graphic;
using SPR.UI.Store;
using SPR.UI.Tutorial;
using SPR.UI.Upgrade;
using UnityEngine;

namespace SPR.UI.Field;

public class FieldCanvas : AutoScaledCanvas
{
	public enum Pending
	{
		None,
		Show,
		Hide
	}

	[SerializeField]
	private PlayerStatusBoard playerStatusBoard;

	[SerializeField]
	private MessageBoard messageBoard;

	[SerializeField]
	private PotionBoard potionBoard;

	[SerializeField]
	private TeleportBoard teleportBoard;

	[SerializeField]
	private LevelBoard levelBoard;

	[SerializeField]
	private ItemBoard itemBoard;

	[SerializeField]
	private BossBoard bossBoard;

	[SerializeField]
	private TutorialMessageBoard tutorialMessageBoard;

	[SerializeField]
	private TutorialBoard tutorialBoard;

	[SerializeField]
	private InteractionPanel interactionPanel;

	[SerializeField]
	private StorePanel storePanel;

	[SerializeField]
	private ExchangePanel exchangePanel;

	[SerializeField]
	private UpgradeWarmhornCountPanel upgradeWarmhornCountPanel;

	[SerializeField]
	private CardEngravingPanel cardEngravingPanel;

	[SerializeField]
	private MoneyBoard moneyBoard;

	[SerializeField]
	private NarrationBoard narrationBoard;

	[SerializeField]
	private CutsceneBoard cutsceneBoard;

	[SerializeField]
	private LevelUpPanel levelUpPanel;

	[SerializeField]
	private DonationPanel donationPanel;

	[SerializeField]
	private PartUpgradePanel partUpgradePanel;

	[SerializeField]
	private SectorBoard sectorBoard;

	[SerializeField]
	private RedAlertBoard redAlertBoard;

	[SerializeField]
	private ConfirmPopup confirmPopup;

	[SerializeField]
	private PausePanel pausePanel;

	private const float ShowHideDuration = 0.6f;

	private Pending isPending;

	private bool currentState;

	public PlayerStatusBoard PlayerStatusBoard => playerStatusBoard;

	public MessageBoard MessageBoard => messageBoard;

	public PotionBoard PotionBoard => potionBoard;

	public TeleportBoard TeleportBoard => teleportBoard;

	public LevelBoard LevelBoard => levelBoard;

	public ItemBoard ItemBoard => itemBoard;

	public BossBoard BossBoard => bossBoard;

	public TutorialMessageBoard TutorialMessageBoard => tutorialMessageBoard;

	public TutorialBoard TutorialBoard => tutorialBoard;

	public InteractionPanel InteractionPanel => interactionPanel;

	public StorePanel StorePanel => storePanel;

	public ExchangePanel ExchangePanel => exchangePanel;

	public UpgradeWarmhornCountPanel UpgradeWarmhornCountPanel => upgradeWarmhornCountPanel;

	public CardEngravingPanel CardEngravingPanel => cardEngravingPanel;

	public MoneyBoard MoneyBoard => moneyBoard;

	public NarrationBoard NarrationBoard => narrationBoard;

	public CutsceneBoard CutsceneBoard => cutsceneBoard;

	public LevelUpPanel LevelUpPanel => levelUpPanel;

	public DonationPanel DonationPanel => donationPanel;

	public PartUpgradePanel PartUpgradePanel => partUpgradePanel;

	public SectorBoard SectorBoard => sectorBoard;

	public RedAlertBoard RedAlertBoard => redAlertBoard;

	public ConfirmPopup ConfirmPopup => confirmPopup;

	public PausePanel PausePanel => pausePanel;

	public bool IsPending => isPending != Pending.None;

	private static GraphicManager GraphicManager => Game.Instance.GraphicManager;

	public void Initialize()
	{
		playerStatusBoard.Initialize();
		moneyBoard.Initialize();
		messageBoard.Initialize();
		bossBoard.Initialize();
		levelBoard.Initialize();
		itemBoard.Initialize();
		tutorialMessageBoard.Initialize();
		tutorialBoard.Initialize();
		interactionPanel.Initialize();
		cardEngravingPanel.Initialize();
		narrationBoard.Initialize();
		potionBoard.Initialize(Game.Instance.Player.PlayerStat.GetStat<WarmhornCount>().MaxPotionCount);
		sectorBoard.Initialize();
	}

	public void OnSceneLoaded()
	{
		bossBoard.Initialize();
		TutorialMessageBoard.OnSceneLoaded();
		SectorBoard.OnSceneLoaded();
		TeleportBoard.OnSceneLoaded();
	}

	public async UniTask ShowAllAsync(bool instant)
	{
		if (isPending != Pending.Show)
		{
			isPending = Pending.Show;
			float duration = (instant ? 0f : 0.6f);
			UniTask uniTask = playerStatusBoard.ShowAsync(duration);
			UniTask uniTask2 = potionBoard.ShowAsync(duration);
			UniTask uniTask3 = levelBoard.ShowAsync(duration);
			UniTask uniTask4 = bossBoard.ShowAsync(duration);
			UniTask uniTask5 = TutorialMessageBoard.ShowAsync(duration);
			UniTask uniTask6 = GraphicManager.ShowVignetteAsync(duration);
			playerStatusBoard.UpdateBarSize(instant: false);
			await UniTask.WhenAll(uniTask, uniTask3, uniTask4, uniTask5, uniTask6, uniTask2);
			playerStatusBoard.PlayMaxHealthChangedEffect();
			currentState = true;
			isPending = Pending.None;
		}
	}

	public async UniTask HideAllAsync(bool instant)
	{
		if (isPending != Pending.Hide)
		{
			isPending = Pending.Hide;
			float duration = (instant ? 0f : 0.6f);
			UniTask uniTask = playerStatusBoard.HideAsync(duration);
			UniTask uniTask2 = potionBoard.HideAsync(duration);
			UniTask uniTask3 = levelBoard.HideAsync(duration);
			UniTask uniTask4 = bossBoard.HideAsync(duration);
			UniTask uniTask5 = TutorialMessageBoard.HideAsync(duration);
			UniTask uniTask6 = GraphicManager.HideVignetteAsync(duration);
			await UniTask.WhenAll(uniTask, uniTask3, uniTask4, uniTask5, uniTask6, uniTask2);
			currentState = false;
			isPending = Pending.None;
		}
	}

	public async UniTask ContinueCurrentState()
	{
		if (currentState)
		{
			await ShowAllAsync(instant: true);
		}
	}
}
