namespace SafraCoinContractsService.Core.ValueObjects;

public class SmartContractVO
{
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string Abi { get; private set; }
    public string ByteCode { get; private set; }
    public string RawCodeHash {get; private set; }

    public SmartContractVO(string name, string address, string abi, string byteCode, string rawCodeHash)
    {
        Name = name;
        Address = address;
        Abi = abi;
        ByteCode = byteCode;
        RawCodeHash = rawCodeHash;
    }
}
