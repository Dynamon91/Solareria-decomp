using Cysharp.Threading.Tasks;

namespace SPR;

public interface ILevelUpProvider
{
	UniTask PlayLevelUpMotionAsync();
}
