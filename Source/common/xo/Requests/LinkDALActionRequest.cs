using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace Common.XO.Requests
{
    public class LinkDALActionRequest
    {
        public LinkDALActionType? DALAction { get; set; }
    }

    //DAL action selection
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDALActionType
    {
        EndADAMode,
        StartADAMode,
        SendReset,
        GetStatus,
        GetPayment,
        GetCreditOrDebit,
        GetPIN,
        GetZIP,
        RemoveCard,
        GetIdentifier,
        GetIdentifierAndPayment,
        GetIdentifierAndHoldData,
        UseHeldData,
        DeviceUI,
        CancelPayment,
        StartPreSwipeMode,
        WaitForCardPreSwipeMode,
        EndPreSwipeMode,
        PurgeHeldCardData,
        StartManualPayment,
        MonitorMessageUpdate,
        GetTestPayment,
        GetTestStatus,
        DeviceUITest,
        GetVerifyAmount,
        SaveRollCall,
        GetSignature,
        EndSignatureMode
    }
}
