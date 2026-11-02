using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR;

public class LeafaStoreCutsceneProducer : CutsceneProducer
{
	[Header("References")]
	[SerializeField]
	private GameObject foodObject;

	[SerializeField]
	private CinemachineVirtualCamera vcam;

	private SpriteRenderer foodRenderer;

	private Leafa leafa;

	private readonly List<SpriteRenderer> foodRenderers = new List<SpriteRenderer>();

	private List<StoreItemData> itemDataList;

	private Sequence foodSequence;

	private bool hasMoreFood;

	private bool canFinishLoop;

	private float endBottomPosY;

	private const float EndCenterX = 2.5f;

	private const float TopLocalPosY = 4.75f;

	private const float EndLocalPosY = 1f;

	private const float Gap = 2.75f;

	private const int InitialSortingOrder = 15;

	private static readonly Vector2 StartLocalPos = new Vector2(2f, -0.2f);

	private static readonly Vector2 TopLocalPos = new Vector2(1.75f, 5f);

	private SpriteRenderer FoodRenderer => foodRenderer ?? (foodRenderer = foodObject.GetComponent<SpriteRenderer>());

	private Leafa Leafa => leafa ?? (leafa = base.transform.parent.GetComponent<Leafa>());

	private void Awake()
	{
		foodRenderers.Add(FoodRenderer);
		endBottomPosY = 1f;
	}

	protected override void SetProducerState(int state)
	{
		foodObject.SetActive(value: false);
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		FoodRenderer.sortingOrder = 15;
		for (int i = 0; i < itemDataList.Count; i++)
		{
			if (foodRenderers.Count <= i)
			{
				GameObject gameObject = Object.Instantiate(foodObject, foodObject.transform.parent);
				foodRenderers.Add(gameObject.GetComponent<SpriteRenderer>());
			}
			string iD = itemDataList[i].ID;
			string key = ((Game.DataManager.GetItemData(iD).ItemType == ItemType.Food) ? Address.FoodObjectSprite(iD) : Address.ItemSprite(iD));
			AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(key);
			await task;
			SpriteRenderer spriteRenderer = foodRenderers[i];
			spriteRenderer.sortingOrder = 15;
			spriteRenderer.transform.localPosition = StartLocalPos;
			foodRenderers[i].sprite = task.Result;
		}
		await BlendFieldCamToCutsceneStartCamAsync(vcam);
	}

	protected override void OnEndTimeline(int state)
	{
		SynchronizeEndVcamAsync(vcam).Forget();
	}

	protected override void OnSkipTimeline(int state)
	{
		foodSequence?.Kill();
		foodRenderers.ForEach(delegate(SpriteRenderer r)
		{
			r.gameObject.SetActive(value: false);
		});
	}

	public override bool ShouldLoop(int index)
	{
		return !canFinishLoop;
	}

	public override void EnterLoop(int index)
	{
		canFinishLoop = false;
		float interval = 0.74f;
		float num = 0.11f;
		float interval2 = 0.4f;
		float num2 = 0.55f;
		float duration = 0.25f;
		float num3 = 0f;
		List<Vector2> list = new List<Vector2>(itemDataList.Count - 1);
		List<int> sortingOrderList = new List<int>(itemDataList.Count - 1);
		sortingOrderList.Add(25);
		list.Add(new Vector2(2.5f, 1f));
		for (int i = 1; i < itemDataList.Count; i++)
		{
			int num4 = ((i % 2 != 0) ? 1 : (-1));
			float x = 2.5f + (float)num4 * (num3 + 2.75f);
			float y = endBottomPosY;
			list.Add(new Vector2(x, y));
			if (i % 2 == 0)
			{
				num3 += 2.75f;
			}
			sortingOrderList.Add(sortingOrderList[Mathf.Max(0, i - 2)] - num4);
		}
		foodSequence = DOTween.Sequence();
		for (int j = 0; j < itemDataList.Count; j++)
		{
			SpriteRenderer targetRenderer = foodRenderers[j];
			Vector2 vector = list[j];
			foodSequence.AppendInterval(interval);
			Sequence sequence = DOTween.Sequence();
			sequence.Append(targetRenderer.transform.DOPivotLocalMoveX(TopLocalPos.x, num2 + num).SetEase(Ease.InOutSine).OnStart(delegate
			{
				targetRenderer.gameObject.SetActive(value: true);
			})).Append(targetRenderer.transform.DOPivotLocalMoveX(vector.x, duration)).SetEase(Ease.Linear);
			Sequence sequence2 = DOTween.Sequence();
			int orderIndex = j;
			sequence2.Append(targetRenderer.transform.DOPivotLocalMoveY(4.75f, num2).SetEase(Ease.InOutCirc)).AppendCallback(delegate
			{
				targetRenderer.sortingOrder = sortingOrderList[orderIndex];
			}).AppendInterval(num)
				.Append(targetRenderer.transform.DOPivotLocalMoveY(vector.y, duration).SetEase(Ease.InOutQuad));
			foodSequence.Append(sequence).Join(sequence2);
			if (j + 1 == itemDataList.Count)
			{
				if (hasMoreFood)
				{
					foodSequence.AppendInterval(interval);
					foodSequence.AppendCallback(delegate
					{
						CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f).GetAwaiter().OnCompleted(delegate
						{
							canFinishLoop = true;
						});
					});
				}
				else
				{
					foodSequence.AppendCallback(delegate
					{
						canFinishLoop = true;
					});
				}
			}
			foodSequence.AppendInterval(interval2);
		}
		foodSequence.Play();
	}

	public void FadeScreen(bool isOut)
	{
		if (isOut)
		{
			CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f).Forget();
		}
		else
		{
			CutsceneProducer.UIManager.FadeCanvas.FadeIn(1f).Forget();
		}
	}

	public void DeactivateFoods()
	{
		foodRenderers.ForEach(delegate(SpriteRenderer r)
		{
			r.gameObject.SetActive(value: false);
		});
	}

	public void PlayPlayerEatAnimation()
	{
		CutsceneProducer.Player.mover.AddAnimation("eat_start", loop: false);
		CutsceneProducer.Player.mover.AddAnimation("eat_loop");
	}

	public void ReturnPlayerAnimation()
	{
		if (!CutsceneProducer.FieldCanvas.StorePanel.HasStock())
		{
			CutsceneProducer.Player.mover.SetAnimation("idle_back");
		}
		else
		{
			PlayerLookBack(value: false);
		}
	}

	public void PrepareInformations(List<StoreItemData> itemDatas)
	{
		itemDataList = itemDatas.Where((StoreItemData _, int i) => i < 3).ToList();
		hasMoreFood = itemDatas.Count > 3;
	}
}
