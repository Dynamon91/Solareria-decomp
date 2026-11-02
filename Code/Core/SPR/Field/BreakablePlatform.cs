using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

[RequireComponent(typeof(EdgeCollider2D))]
public class BreakablePlatform : MonoBehaviour, IStateSavable
{
	private EdgeCollider2D edgeColl;

	private SpriteRenderer sr;

	private int currentState;

	private const int DefaultState = 0;

	private const int BrokenState = 1;

	public string ID => $"{this}_{Game.Instance.CurrentFieldID}";

	private EdgeCollider2D EdgeCollider => edgeColl ?? (edgeColl = GetComponent<EdgeCollider2D>());

	private SpriteRenderer SpriteRenderer => sr ?? (sr = GetComponent<SpriteRenderer>());

	public void LoadState(int state)
	{
		SpriteRenderer.DOKill();
		SpriteRenderer.color = Color.white;
		if (state == 0)
		{
			base.gameObject.SetActive(value: true);
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
		currentState = state;
	}

	public void ChangeState(int state)
	{
		if (state == 1)
		{
			SpriteRenderer.DOColor(Color.black, 1f).OnComplete(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		}
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void FixedUpdate()
	{
		if (currentState == 0 && EdgeCollider.bounds.Intersects(Game.Instance.Player.mover.ColliderBounds))
		{
			ChangeState(1);
		}
	}

	private void OnDrawGizmosSelected()
	{
		base.transform.position = base.transform.position.RoundToInt();
	}
}
