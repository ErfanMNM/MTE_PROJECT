
namespace DMS_App.Controllers
{
    public class DataPool_Query_Payload_Req_GetPoolPatch
    {
        public string id { get; set; } = string.Empty;
        public string timestamp {  get; set; } = string.Empty;
        public string user { get; set; } = string.Empty;
        public string poolname {  get; set; } = string.Empty;
        public e_DataPool_Query_Payload_Type type { get; set; } = e_DataPool_Query_Payload_Type.POOL_QUERY;
        public e_DataPool_Query_Payload_Action action { get; set; }
    }

    public class DataPool_Query_Payload_Res_GetPoolPatch
    {
        public string id { get; set; } = string.Empty;
        public string timestamp { get; set; } = string.Empty;
        public string poolname { get; set; } = string.Empty;
        public string poolpath {  get; set; } = string.Empty;
        public int status { get; set; } = 500;
        public string message { get; set; } = string.Empty;
    }


    public enum e_DataPool_Query_Payload_Type
    {
        POOL_QUERY,
        POOL_CREATE,
        POOL_UPDATE,
        POOL_DELETE,
        POOL_INSERT
    }

    public enum e_DataPool_Query_Payload_Action
    {
        GetPoolPath,
    }
    public class Payload_GetPoolPath { }
}

