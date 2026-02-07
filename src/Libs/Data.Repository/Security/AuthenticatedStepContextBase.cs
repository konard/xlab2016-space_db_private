using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Security
{
    public abstract class AuthenticatedStepContextBase<TStepContext, TAuthInfo> : StepContextBase<TStepContext>
        where TStepContext : AuthenticatedStepContextBase<TStepContext, TAuthInfo>
        where TAuthInfo : AuthInfoBase
    {
        public TAuthInfo AuthInfo { get; set; }

        public override void AssignInput(TStepContext other)
        {
            AuthInfo = other?.AuthInfo;
        }
    }
}
