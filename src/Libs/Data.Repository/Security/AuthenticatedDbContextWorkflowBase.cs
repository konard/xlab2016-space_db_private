using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Security
{
    public class AuthenticatedDbContextWorkflowBase<TRepository, T, TQuery, TKey, TState, TDto, TDetailedDto, TStepContext, TAuthInfo> :
        DbContextWorkflowBase<TRepository, T, TQuery, TKey, TState, TDto, TDetailedDto, TStepContext>,
        IAuthenticatedWorkflow<TAuthInfo>
        where TRepository : IRepository<T, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TQuery : IPaginable
        where TStepContext : AuthenticatedStepContextBase<TStepContext, TAuthInfo>
        where TAuthInfo : AuthInfoBase
    {
        private readonly IAuthDataService<TAuthInfo> authDataService;

        public TAuthInfo AuthInfo { get; set; }

        public AuthenticatedDbContextWorkflowBase(TRepository repository,
            IAuthDataService<TAuthInfo> authDataService)
            : base(repository)
        {
            this.authDataService = authDataService;
        }

        public async Task Authorize()
        {
            AuthInfo = await authDataService.AuthorizeAsync();
        }

        public override async Task RunAsync(TState oldState, TState newState, TStepContext stepContext)
        {
            if (AuthInfo == null)
                await Authorize();

            if (stepContext != null)
                stepContext.AuthInfo = AuthInfo;
        }
    }
}
