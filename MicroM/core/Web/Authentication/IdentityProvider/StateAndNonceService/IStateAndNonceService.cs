using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication.SSO;

public interface IStateAndNonceService
{
    StateAndNonceContext EnsureStateAndNonce(IFormCollection original, string? providedState, string? providedNonce, string? providedDeviceId);

    void StoreStateCookie(ApplicationOption app, string hmacKey, StateAndNonceData data);

    ResultWithStatus<StateAndNonceData, string> ValidateAndConsumeStateCookie(string app_id, string hmacKey, string incomingState);
}
