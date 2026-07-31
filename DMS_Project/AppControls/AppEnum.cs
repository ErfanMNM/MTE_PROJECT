
namespace DMS_Project.AppControls
{
    public enum e_AppState
    {
        Initiating,
        Ready
    }

    public class ProductionStateMachine
    {
        public e_AppState AppState { get; set; } = e_AppState.Initiating;
        public void SetReady() => AppState = e_AppState.Ready;
    }

}
