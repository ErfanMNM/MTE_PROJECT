using DMS_Project.AppControls;
using DMS_Project.Production;

namespace DMS_Project.Infrastructure
{
    public static class Global_Variable
    {
        // App State
        public static e_AppState AppState { get; set; } = e_AppState.Initiating;

        // Current Production Order
        public static POInfo CurrentPO { get; set; } = new POInfo();

        // Production State Machine
        public static e_Production_State ProductionState { get; set; } = e_Production_State.NoSelectedPO;

        // Production Instance (singleton) - khởi tạo khi cần
        private static Production.Production? _production;
        public static Production.Production Production
        {
            get
            {
                _production ??= new Production.Production();
                return _production;
            }
        }

        // DataPool Instance (singleton) - khởi tạo khi cần
        private static DataPool.DataPool? _dataPool;
        public static DataPool.DataPool DataPool
        {
            get
            {
                _dataPool ??= new DataPool.DataPool();
                return _dataPool;
            }
        }
    }
}
