using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Upgrade;

public class LevelUpPanel : UpgradePanel
{
	[Header("Key Guides")]
	[SerializeField]
	private List<GameObject> increaseControlGuides;

	[SerializeField]
	private List<GameObject> decreaseControlGuides;

	[SerializeField]
	private List<GameObject> upgradeControlGuides;

	private int targetLevel;

	private ILevelUpProvider levelUpProvider;

	protected override bool UsingOhn => true;

	private LevelUpBoard LevelUpBoard => upgradeBoard as LevelUpBoard;

	protected override string ConfirmType => "LevelUp";

	public override void Initialize(IService serviceObject, GameObject vcam, Unit arg)
	{
		base.Initialize(serviceObject, vcam, arg);
		levelUpProvider = serviceObject.ServiceGameObject.GetComponent<ILevelUpProvider>();
	}

	public override string ControllerName()
	{
		return "Level Up Panel";
	}

	protected override async UniTask OnActivateAsync()
	{
		targetLevel = ServicePanel<Unit>.PlayerStat.Level + 1;
		await upgradeBoard.InitializeAsync();
		LevelUpBoard.SetData(new LevelUpModel(targetLevel));
		UpdateControlGuide();
		ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Ingame/SFX_UI_Verita_Appear");
	}

	protected override void OnControlInput(int horizontal, int vertical)
	{
		if (vertical == 0)
		{
			targetLevel = Mathf.Clamp(targetLevel + horizontal, ServicePanel<Unit>.PlayerStat.Level, 120);
			LevelUpBoard.SetData(new LevelUpModel(targetLevel, horizontal > 0));
			UpdateControlGuide();
			ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
	}

	protected override void OnConfirmInput()
	{
		UniTask task = PlaySuccessTimelineAsync();
		ServicePanel<Unit>.InputManager.BlockControlForTask(task, "Level Up Success Timeline").Forget();
		levelUpProvider.PlayLevelUpMotionAsync().Forget();
	}

	protected override void UpdateControlGuide()
	{
		switch (upgradeBoard.CanProceedUpgrade())
		{
		case UpgradeCheckResult.Success:
			upgradeControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: true);
			});
			increaseControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: true);
			});
			decreaseControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: true);
			});
			break;
		case UpgradeCheckResult.NotEnoughOhn:
		case UpgradeCheckResult.NotEnoughMaterial:
		case UpgradeCheckResult.SameGrade:
			upgradeControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: false);
			});
			increaseControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: true);
			});
			decreaseControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: true);
			});
			break;
		default:
			upgradeControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: false);
			});
			increaseControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: false);
			});
			decreaseControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: false);
			});
			break;
		}
	}

	public override void UpdateData()
	{
		upgradeBoard.InitializeAsync().Forget();
		targetLevel = ServicePanel<Unit>.PlayerStat.Level + 1;
		LevelUpBoard.SetData(new LevelUpModel(targetLevel));
		UpdateControlGuide();
	}

	public void ProceedLevelUp()
	{
		upgradeBoard.OnProceedUpgrade();
	}
}
