using System;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource
    {
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            var exec = ExecuteWriteWithPolicy(
                "BeginTransaction",
                ds => ds.BeginTransaction(args),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);

            if (!exec.Success)
            {
                LogSafe("Failed to start transaction after all retries. Please check data source health.");
                throw new Exception("Transaction begin failed across all retries and failovers.");
            }

            return exec.Result ?? Current.ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            var exec = ExecuteWriteWithPolicy(
                "EndTransaction",
                ds => ds.EndTransaction(args),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);

            if (!exec.Success)
            {
                LogSafe("Failed to end transaction after all retries. Please check data source health.");
                throw new Exception("Transaction end failed across all retries.");
            }

            return exec.Result ?? Current.ErrorObject;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            var exec = ExecuteWriteWithPolicy(
                "Commit",
                ds => ds.Commit(args),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);

            if (!exec.Success)
            {
                LogSafe("Failed to commit transaction after all retries. Please check data source health.");
                throw new Exception("Transaction commit failed across all retries.");
            }

            return exec.Result ?? Current.ErrorObject;
        }
    }
}
