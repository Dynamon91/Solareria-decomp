using System.Collections.Generic;
using System.Linq;

namespace SPR.UI.Trait;

public class TraitStatusCardBoard : TraitCardBoard
{
	private readonly Dictionary<int, int> diffs = new Dictionary<int, int>();

	protected override int FirstID => 1;

	protected override bool UsingActivationEffect => true;

	private TraitStatusCardElement CurrentStatus => (TraitStatusCardElement)base.Current;

	public IReadOnlyDictionary<int, int> Diffs => diffs;

	public int DiffSum => diffs.Sum((KeyValuePair<int, int> kvp) => kvp.Value);

	public bool HasNoDiff
	{
		get
		{
			if (diffs.Count != 0)
			{
				return diffs.All((KeyValuePair<int, int> kvp) => kvp.Value == 0);
			}
			return true;
		}
	}

	public override void OnActivate(bool focusCurrent)
	{
		base.OnActivate(focusCurrent);
		diffs.Clear();
	}

	public TraitResult TryAddDiff(int diff)
	{
		int currentID = base.CurrentID;
		int currentDiff;
		TraitResult traitResult = CurrentStatus.TryAddDiff(diff, DiffSum, out currentDiff);
		if (traitResult != TraitResult.Success)
		{
			DebugLog.GeneralLogWarning($"TryAddDiff({currentID}) : {traitResult}");
			return traitResult;
		}
		if (!diffs.TryAdd(currentID, currentDiff))
		{
			diffs[currentID] = currentDiff;
		}
		if (currentDiff == 0)
		{
			diffs.Remove(currentID);
		}
		return TraitResult.Success;
	}

	public void ConfirmDiffs()
	{
		diffs.Clear();
		base.Cards.ForEach(delegate(TraitCardElement card)
		{
			((TraitStatusCardElement)card).ApplyLevel();
		});
	}

	public void ResetDiffs()
	{
		diffs.Clear();
		base.Cards.ForEach(delegate(TraitCardElement card)
		{
			card.RefreshData();
		});
	}

	public void ResetTraits()
	{
		diffs.Clear();
		base.Cards.ForEach(delegate(TraitCardElement card)
		{
			((TraitStatusCardElement)card).ResetLevel();
		});
	}
}
