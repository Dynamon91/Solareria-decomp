using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class PlayerDropItem : MonoBehaviour, IInteractable
{
	[SerializeField]
	private Vector2Int interactCanvasPosition = new Vector2Int(0, 3);

	[SerializeField]
	private ParticleSystem idleParticle;

	[SerializeField]
	private ParticleSystem interactParticle;

	[SerializeField]
	private ParticleSystem playerParticle;

	[SerializeField]
	private EventReference coinEvent;

	private BoxCollider2D coll;

	private FieldInteractCanvas interactCanvas;

	public string Name => "Player Drop Item";

	public bool IsNull => this.IsNull();

	public int InteractableOrder => 1;

	private BoxCollider2D BoxCollider => coll ?? (coll = GetComponent<BoxCollider2D>());

	private static PlayerData.DropData DropData => Game.Instance.SaveData.PlayerData.CurrentDropData;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
		idleParticle.Play();
	}

	public void Interact()
	{
		BoxCollider.enabled = false;
		int money = DropData.Money;
		PlayerStat.AddMoney(money);
		PlayerStat.AddExperience(DropData.Exp);
		Game.Instance.Player.SetSoulDropPosition(default(SoulDropInfo).None);
		Game.Instance.Player.SetSpecialDropInfo(value: false);
		Game.Instance.SaveData.PlayerData.ClearDropData();
		interactParticle.Play();
		playerParticle.transform.SetParent(Game.Instance.Player.transform);
		playerParticle.transform.localPosition = Vector3.zero;
		playerParticle.Play();
		idleParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		UniTask.WaitWhile(() => playerParticle.isEmitting).GetAwaiter().OnCompleted(delegate
		{
			playerParticle.transform.SetParent(base.transform);
		});
		PlaySFXAsync(money).Forget();
	}

	private async UniTask PlaySFXAsync(int money)
	{
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/Object/SFX_Object_GetSoul");
		if (money > 0)
		{
			await UniTask.WaitForSeconds(Game.Instance.UIManager.FieldCanvas.MoneyBoard.MaintainDuration, ignoreTimeScale: true);
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Ingame/SFX_Coin_Long");
		}
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}
}
