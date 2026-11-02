using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Upgrade;

public abstract class UpgradeBoardBase : MonoBehaviour
{
	public abstract UniTask InitializeAsync();

	protected abstract void UpdateUI();

	public abstract UpgradeCheckResult CanProceedUpgrade();

	public abstract void OnProceedUpgrade();

	public abstract void OnBlockUpgrade(UpgradeCheckResult ucResult);

	protected abstract void ShowRequirements(bool value);
}
