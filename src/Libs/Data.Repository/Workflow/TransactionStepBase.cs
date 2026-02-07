using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Workflow
{
    public abstract class TransactionStepBase<TStepContext> : StepBase<TStepContext>
        where TStepContext : TransactionStepContextBase<TStepContext>, new()
    {
        public TransactionStepBase() : base()
        {
        }

        public TransactionStepBase(TStepContext stepContext) : 
            base(stepContext)
        {
        }

        protected abstract Task RunTransactionAsync(DbContext db);

        protected abstract DbContext GetDb();

        public override async Task Run()
        {
            var db = GetDb();

            // If an outer transaction already exists, just run within it to avoid nesting
            if (db.Database.CurrentTransaction != null)
            {
                await RunTransactionAsync(db);
                return;
            }

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    await RunTransactionAsync(db);
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
