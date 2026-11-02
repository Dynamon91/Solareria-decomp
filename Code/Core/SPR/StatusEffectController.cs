using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.UI.Field;

namespace SPR;

public class StatusEffectController
{
	private readonly Character owner;

	private readonly List<StatusEffect> elementEffects = new List<StatusEffect>();

	private bool IsPlayer => owner is PlayerMover;

	private static EffectBoard PlayerEffectStatusBoard => Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard.EffectBoard;

	public StatusEffectController(Character owner)
	{
		this.owner = owner;
		if (IsPlayer)
		{
			AddressableHelper.PreloadAssetAsync("Electric Shock_loop.prefab").Forget();
		}
	}

	public void AddEffect<T>(T elementEffect) where T : StatusEffect
	{
		int num = elementEffects.FindIndex((StatusEffect x) => x.GetType() == typeof(T));
		if (num >= 0)
		{
			elementEffects[num].Update(elementEffect);
			return;
		}
		elementEffects.Add(elementEffect);
		elementEffect.OnAdd();
		Game.Instance.RuntimeData.AddHitStatusEffect(elementEffect);
		if (IsPlayer)
		{
			PlayerEffectStatusBoard.ApplyIcon(elementEffect);
		}
	}

	public void RemoveEffect<T>() where T : StatusEffect
	{
		foreach (StatusEffect elementEffect in elementEffects)
		{
			if (elementEffect is T)
			{
				elementEffect.ClearStack();
				elementEffect.OnRemove(this, forceRemove: true);
			}
		}
		elementEffects.RemoveAll((StatusEffect x) => x.GetType() == typeof(T));
		PlayerEffectStatusBoard.UpdateIcons();
	}

	public bool Contain<T>() where T : StatusEffect
	{
		return elementEffects.Find((StatusEffect x) => x.GetType() == typeof(T)) != null;
	}

	public T GetEffect<T>() where T : StatusEffect
	{
		return (T)elementEffects.Find((StatusEffect x) => x.GetType() == typeof(T));
	}

	public List<T> GetEffects<T>() where T : StatusEffect
	{
		return elementEffects.Where((StatusEffect x) => x.GetType() == typeof(T)).Cast<T>().ToList();
	}

	public int Count<T>() where T : StatusEffect
	{
		return elementEffects.Count((StatusEffect x) => x.GetType() == typeof(T));
	}

	public void Clear(bool force = false)
	{
		foreach (StatusEffect elementEffect in elementEffects)
		{
			elementEffect.ClearStack();
			if (force)
			{
				elementEffect.OnRemove(this, forceRemove: true);
			}
		}
		if (IsPlayer)
		{
			PlayerEffectStatusBoard.HideAllIcons(instant: true);
		}
	}

	public void Running()
	{
		if (elementEffects.Count == 0)
		{
			return;
		}
		foreach (StatusEffect elementEffect in elementEffects)
		{
			elementEffect.Running();
			UpdateEffectUI();
		}
		foreach (StatusEffect elementEffect2 in elementEffects)
		{
			if (elementEffect2.IsDone)
			{
				elementEffect2.OnRemove(this, forceRemove: false);
			}
		}
		if (IsPlayer)
		{
			PlayerEffectStatusBoard.UpdateIcons();
		}
		elementEffects.RemoveAll((StatusEffect e) => e.IsDone);
	}

	private void UpdateEffectUI()
	{
		_ = IsPlayer;
	}

	public void SetEffectLoop(bool active)
	{
		foreach (StatusEffect elementEffect in elementEffects)
		{
			elementEffect.SetEffectLoop(active);
		}
	}
}
