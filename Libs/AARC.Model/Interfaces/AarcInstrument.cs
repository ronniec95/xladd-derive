namespace AARC.Model.Interfaces
{
    public interface IAarcInstrument : IAarcTicker
    {
        AarcContractType ContractType { get; }
        string LocalSymbol { get; }
        //string Ticker { get; set; }
    }
}
