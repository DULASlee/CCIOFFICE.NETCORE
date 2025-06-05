// VOL.Core/Enums/LogEvent.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace VOL.Core.Enums
{
    public enum LogEvent // Renamed from LoggerType
    {
        System = 0,
        Info,
        Success,
        Error,
        Authorzie, // Note: Possible typo in original, keeping as is unless specified to change
        Global,
        Login,
        Exception,
        ApiException,
        HandleError,
        OnActionExecuted,
        GetUserInfo,
        Edit,
        Search,
        Add,
        Del,
        AppHome,
        ApiLogin,
        ApiPINLogin,
        ApiRegister,
        ApiModifyPwd,
        ApiSendPIN,
        ApiAuthorize,
        Ask,
        JoinMeeting,
        JoinUs,
        EditUserInfo,
        Sell,
        Buy,
        ReportPrice,
        Reply,
        TechData,
        TechSecondData,
        DelPublicQuestion,
        DelexpertQuestion,
        CreateTokenError,
        IPhoneTest,
        SDKSuccess,
        SDKSendError,
        ExpertAuthority,
        ParEmpty,
        NoToken,
        ReplaceToeken, // Note: Possible typo in original, keeping as is
        KafkaException,
        // Adding some values from standard LoggerType that might be missing or for clarity
        // These were used in previous steps.
        Update, // Often used for update operations
        Select, // Often used for select/query operations
        Insert, // Often used for insert operations
        Delete  // Often used for delete operations
        // Consider if the existing 'Error' and 'Exception' are sufficient or if more specific error events are needed.
        // The list seems quite extensive already.
    }
}
