using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace Data.Repository.Helpers
{
    public static class TransactionHelper
    {
        public static TransactionScope CreateScope(TimeSpan? timeout = null)
        {
            return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = timeout ?? new TimeSpan(0, 0, 0, 30)
            }, TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}
