using Rewired;

namespace SPR.UI;

public interface IControllerEventUpdate
{
	void OnControllerChanged(ControllerStatusChangedEventArgs args);
}
