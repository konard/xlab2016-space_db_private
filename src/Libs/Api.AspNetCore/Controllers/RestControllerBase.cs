using Api.AspNetCore.Exceptions;
using Api.AspNetCore.Models;
using Api.AspNetCore.Models.Scope;
using Api.AspNetCore.Models.Validation;
using Api.AspNetCore.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Api.AspNetCore.Controllers
{
    public class RestControllerBase : ControllerBase
    {
        protected readonly IAuthorizeService authorizeService;

        public RestControllerBase()
        {
        }

        public RestControllerBase(IAuthorizeService authorizeService)
        {
            this.authorizeService = authorizeService;
        }

        protected virtual async Task<ActionResult<TResponse>> RestMethod<TRequest, TResponse>(TRequest request,
            Func<TRequest, Task<ValidationResult>> validate,
            Func<TRequest, Task<TResponse>> operate,
            Func<RequestContext<TRequest>, Task<TResponse>> operate2 = null,
            Func<bool> overrideAuthCore = null)
        {
            return await RestMethodCore(request, false, validate, operate, operate2);
        }

        protected virtual async Task<ActionResult<TResponse>> RestMethodAsAnonymous<TRequest, TResponse>(TRequest request,
            Func<TRequest, Task<ValidationResult>> validate,
            Func<TRequest, Task<TResponse>> operate,
            Func<RequestContext<TRequest>, Task<TResponse>> operate2 = null)
        {
            return await RestMethodCore(request, true, validate, operate, operate2);
        }

        protected virtual async Task<ActionResult<TResponse>> RestMethod<TResponse>(
            Func<IAuthorizationData, Task<TResponse>> operate = null, 
            Func<bool> overrideAuthCore = null)
        {
            var authorize = await authorizeService.IsAuthorized();

            if (authorize == null)
                return Unauthorized();

            if (overrideAuthCore?.Invoke() != true)
            {
                if (!await AuthorizeCore())
                    return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                return await operate(authorize);
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
        }

        protected virtual async Task<bool> AuthorizeCore()
        {
            return true;
        }

        private async Task<ActionResult<TResponse>> RestMethodCore<TRequest, TResponse>(TRequest request,
            bool isAnonymous,
            Func<TRequest, Task<ValidationResult>> validate,
            Func<TRequest, Task<TResponse>> operate,
            Func<RequestContext<TRequest>, Task<TResponse>> operate2 = null,
            Func<bool> overrideAuthCore = null)
        {
            IAuthorizationData authorize = null;

            if (authorizeService != null && !isAnonymous)
            {
                authorize = await authorizeService.IsAuthorized();

                if (authorize == null)
                    return Unauthorized();

                if (overrideAuthCore?.Invoke() != true)
                {
                    if (!await AuthorizeCore())
                        return Unauthorized();
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (validate != null)
            {
                var validationResult = await validate(request);

                if (validationResult.Errors.Count > 0)
                    return BadRequest(new ValidationFailedResult(validationResult));
            }

            try
            {
                if (operate != null)
                    return await operate(request);
                else if (operate2 != null)
                    return await operate2(new RequestContext<TRequest>
                    {
                        Request = request,
                        Authorization = authorize
                    });

                throw new InvalidOperationException("No body operation");
            }
            catch (BadRequestException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
