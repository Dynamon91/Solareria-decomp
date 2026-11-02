using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class RoyalGuardArea : LegendaryArea
{
	[Header("지형 관련 설정")]
	[SerializeField]
	private float waterDragonSinkSeconds;

	[SerializeField]
	private float waterDrainingSeconds;

	[Header("References")]
	[SerializeField]
	private GameObject water;

	[SerializeField]
	private GameObject[] waterSprites;

	[SerializeField]
	private Water[] waterZone;

	[SerializeField]
	private AreaChanger waterDragonAreaChanger;

	[SerializeField]
	private AreaChanger corridorAreaChanger;

	[SerializeField]
	private SubArea corridorArea;

	[SerializeField]
	private BoxCollider2D transparentWall;

	public GameObject Water => water;

	public Vector3 WaterOriginPos { get; private set; }

	public BoxCollider2D TransparentWall => transparentWall;

	public async UniTaskVoid DisableWaterZone()
	{
		NullControllable nullControllable = new NullControllable("Water Dragon Death");
		await Game.Instance.InputManager.StackControlTarget(nullControllable);
		TweenerCore<Vector3, Vector3, VectorOptions> tween = water.transform.DOScaleY(0f, waterDrainingSeconds).SetEase(Ease.Linear).OnComplete(delegate
		{
			water.gameObject.SetActive(value: false);
		});
		GameObject[] array = waterSprites;
		foreach (GameObject sprite in array)
		{
			sprite.transform.DOMoveY(sprite.transform.position.y - sprite.GetComponent<SpriteRenderer>().bounds.size.y, waterDrainingSeconds).SetEase(Ease.Linear);
			sprite.transform.DOScaleY(0f, waterDrainingSeconds).SetEase(Ease.Linear).OnComplete(delegate
			{
				sprite.gameObject.SetActive(value: false);
			});
		}
		await tween.WaitAsync();
		waterZone.ForEach(delegate(Water a)
		{
			a.gameObject.SetActive(value: false);
		});
		waterDragonAreaChanger.gameObject.SetActive(value: false);
		corridorAreaChanger.gameObject.SetActive(value: true);
		await Game.Instance.InputManager.RemoveControlTarget(nullControllable);
		FieldController.ChangeAreaTo(corridorArea);
		RoyalGuard componentInChildren = GetComponentInChildren<RoyalGuard>();
		Game.Instance.Player.PlayerStat.AddStatAppliedExperience(componentInChildren.EnemyStatData.Exp);
	}

	public void HandleWaterOverlap(List<IDamageable> list)
	{
		AttackObject component = water.GetComponent<AttackObject>();
		BoxCollider2D component2 = water.GetComponent<BoxCollider2D>();
		AttackUtility.AttackBox(water.transform.position, component2.size, new HitInfo(component), LayerUtility.PlayerLayer, list);
	}

	protected override void OnInitialize()
	{
		if (IsRoyalGuardDead())
		{
			water.gameObject.SetActive(value: false);
			waterSprites.ForEach(delegate(GameObject s)
			{
				s.gameObject.SetActive(value: false);
			});
			waterZone.ForEach(delegate(Water a)
			{
				a.gameObject.SetActive(value: false);
			});
			waterDragonAreaChanger.gameObject.SetActive(value: false);
			corridorAreaChanger.gameObject.SetActive(value: true);
			base.gameObject.SetActive(value: false);
		}
		else
		{
			waterDragonAreaChanger.gameObject.SetActive(value: true);
			corridorAreaChanger.gameObject.SetActive(value: false);
			base.OnInitialize();
		}
		WaterOriginPos = water.transform.position;
	}

	private bool IsRoyalGuardDead()
	{
		EnemyPlaceHolder componentInChildren = GetComponentInChildren<EnemyPlaceHolder>();
		if (!Game.Instance.RuntimeData.IsAlreadyDieCreature(Game.Instance.CurrentFieldID, componentInChildren.transform.position))
		{
			return Game.Instance.SaveData.WorldData.IsEliminatedCreature(Game.Instance.CurrentFieldID, componentInChildren.transform.position);
		}
		return true;
	}
}
