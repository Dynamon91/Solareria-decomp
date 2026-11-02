using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Tutorial;

public class TutorialTrigger : MonoBehaviour, IStateSavable
{
	[SerializeField]
	private ActionName actionName;

	[SerializeField]
	private bool isPopup;

	[SerializeField]
	private BreakableTree chargeAttackTarget;

	private Vector2 center;

	private Vector2 size;

	private BoxCollider2D coll;

	private const int DefaultState = 0;

	private const int CompleteState = 1;

	private int currentState;

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static TutorialMessageBoard TutorialMessageBoard => Game.Instance.UIManager.FieldCanvas.TutorialMessageBoard;

	private static TutorialBoard TutorialBoard => Game.Instance.UIManager.FieldCanvas.TutorialBoard;

	private static TutorialCanvas TutorialCanvas => Game.Instance.UIManager.TutorialCanvas;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public string ID => $"TutorialTrigger_{actionName}";

	public void LoadState(int state)
	{
		currentState = state;
		base.gameObject.SetActive(state == 0);
	}

	public void ChangeState(int state)
	{
		currentState = state;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public void Hide()
	{
		TutorialBoard.SetActiveAsync(value: false).Forget();
	}

	private void Start()
	{
		center = (Vector2)base.transform.position + Coll.offset;
		size = Coll.size;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!other.TryGetComponent<PlayerMover>(out var _) || (actionName == ActionName.Attack && !Game.Instance.Player.mover.PSM.BasicAttackInputItem.Active) || (actionName == ActionName.ChargeAttack && (PlayerStat.HeatMaxLevel <= 0 || chargeAttackTarget.IsNull() || chargeAttackTarget.IsBroken)) || actionName == ActionName.Groggy || (actionName == ActionName.Ignition && !PlayerHeat.CanActivateSkill(HeatSkillType.Ignition)))
		{
			return;
		}
		if (ActionNameUtil.ActionNamesOnlyShowMessage.Contains(actionName))
		{
			ShowOnlyTutorialMessage();
		}
		else if (WanderingData.ShouldShowTutorial(actionName))
		{
			ShowTutorialMessage();
			ChangeState(1);
			if (isPopup)
			{
				TutorialCanvas.SetData(actionName);
				base.gameObject.SetActive(value: false);
				InputManager.StackControlTarget(TutorialCanvas).Forget();
			}
			else
			{
				TutorialBoard.SetData(actionName);
				TutorialBoard.SetActiveAsync(value: true).Forget();
			}
		}
	}

	private void OnTriggerStay2D(Collider2D other)
	{
		if (!other.TryGetComponent<PlayerMover>(out var component))
		{
			return;
		}
		if (actionName == ActionName.HeavyAttackCharge)
		{
			if (component.PSM.ChargeAttackInputItem.IsSuccessCharging)
			{
				ChangeState(1);
				TutorialBoard.SetData(ActionName.HeavyAttackUseJumper);
			}
		}
		else if (actionName == ActionName.Groggy)
		{
			ChangeState(1);
			base.gameObject.SetActive(value: false);
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other.TryGetComponent<PlayerMover>(out var _))
		{
			TutorialBoard.SetActiveAsync(value: false).Forget();
		}
	}

	private void ShowTutorialMessage()
	{
		if (currentState != 1)
		{
			ActionName actionName = this.actionName;
			if (actionName == ActionName.Move || actionName == ActionName.Jump)
			{
				TutorialMessageBoard.AddMessage(this.actionName);
			}
		}
	}

	private void ShowOnlyTutorialMessage()
	{
		if (WanderingData.ShouldShowNewTutorialMessage(actionName) && currentState != 1)
		{
			TutorialMessageBoard.AddMessage(actionName);
			base.gameObject.SetActive(value: false);
		}
	}
}
