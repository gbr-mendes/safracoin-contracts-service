namespace SafraCoinContractsService.Core.Settings;

public class BlockChainSettings
{
    public required string TokenName { get; set; }
    public required string TokenSymbol { get; set; }
    public required string RpcUrl { get; set; }
    public required string PrivateKey { get; set; }
    public required string AccountAddress { get; set; }
    public uint GasLimit { get; set; }
}
