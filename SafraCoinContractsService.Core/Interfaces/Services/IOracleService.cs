namespace SafraCoinContractsService.Core.Interfaces.Services;

public interface IOracleService
{
    Task SetAuthorization(string accountAddress, bool isAuthorized=true);
}
