namespace SafraCoinContractsService.Core.Settings;

public class BlockChainSettings
{
    public required string RpcUrl { get; set; }
    public required string PrivateKey { get; set; }
    public required string AccountAddress { get; set; }
    public uint GasLimit { get; set; }
}
