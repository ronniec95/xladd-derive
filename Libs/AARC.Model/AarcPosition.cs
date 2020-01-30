using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AARC.Model.Interfaces;

namespace AARC.Model
{
    public class AarcOptionPositon : IAarcPosition
    {
        private readonly AarcPosition _position;
        public AarcOptionPositon(AarcPosition position)
        {
            Option = position.Contract as Option;
            _position = position;
        }
        public Option Option { get; private set; }
        public string LocalSymbol => _position.LocalSymbol;
        public string Account => _position.Account;
        public double Size => _position.Size;
        [DisplayName("Avg Price")]
        public double AveragePrice => _position.AveragePrice;
    }

    public interface IAarcPosition
    {
        string LocalSymbol { get; }
        string Account { get; }
        double Size { get; }
        double AveragePrice { get; }
    }

    public class AarcPosition : IAarcPosition
    {
        [Browsable(false)]
        public IAarcInstrument Contract { get; }

        public AarcPosition()
        { }

        public AarcPosition(double size, IAarcInstrument ins, double averagePrice = 0)
        {
            Account = "NOTSET";
            Size = size;
            Contract = ins;
            AveragePrice = averagePrice;
        }

        public AarcPosition(string account, double quantity, IAarcInstrument instrument, double averagePrice = 0)
        {
            Account = account;
            Size = quantity;
            Contract = instrument;
            AveragePrice = averagePrice;
        }

        // Position Id=0 [,Type:OPT,Symbol:AAPL,C,108,20160916 ] ,Position: 5, 91.7817
        public string LocalSymbol { get { return Contract?.LocalSymbol; } }
        public string Account { get; set; }
        public double Size { get; set; }
        public double AveragePrice { get; set; }

    }

    public class AarcPortfolioPosition : AarcPosition
    {
        public AarcPortfolioPosition(string account, double quantity, IAarcInstrument instrument, double averagePrice, double marketPrice, double marketValue, double unrealisedPnl, double realisedPnl)
            : base(account, quantity, instrument, averagePrice)
        {
            MarketPrice = marketPrice;
            MarketValue = marketValue;
            UnrealisedPnl = unrealisedPnl;
            RealisedPnl = realisedPnl;
        }

        [DisplayName("Mkt Price")]
        public double MarketPrice { get; set; }
        [DisplayName("Mkt Value")]
        public double MarketValue { get; set; }
        [DisplayName("Unrealised PnL")]
        public double UnrealisedPnl { get; set; }
        [DisplayName("Realised PnL")]
        public double RealisedPnl { get; set; }
    }

    [Table("AarcBrokerPosition")]
    public class AarcBrokerPosition
    {
        public AarcBrokerPosition()
        {
        }

        public AarcBrokerPosition(AarcPortfolioPosition pos, DateTime date)
        {
            Account = pos.Account;
            PositionType = pos.Contract.ContractType.ToString();
            LocalSymbol = pos.LocalSymbol;
            Size = pos.Size;
            RealisedPnL = pos.RealisedPnl;
            UnrealisedPnL = pos.UnrealisedPnl;
            AveragePrice = pos.AveragePrice;
            MktPrice = pos.MarketPrice;
            MktValue = pos.MarketValue;
            Date = date;
        }

        [Key]
        public int PositionId { get; set; }

        public string Account { get; set; }

        public string PositionType { get; set; }

        public string LocalSymbol { get; set; }
        public double Size { get; set; }

        public double RealisedPnL { get; set; }

        public double UnrealisedPnL { get; set; }

        public double AveragePrice { get; set; }

        public double MktPrice { get; set; }

        public double MktValue { get; set; }

        public DateTime Date { get; set; }
    }
}
