using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable InconsistentNaming

namespace AARC.Model
{
    [Flags]
    public enum StockFundamentalType
    {
        None = 0,
        book_value_per_share = 1,   //  says "daily_book_value_per_share" but it's quarterly also "monthly_book_value_per_share" probably best
        eps_diluted_ttm = 2,        // (TTM)
        eps_diluted = 4,            // (quarterly)
        q_profit_margin = 8,        // Profit Margin (quarterly)
        debt_to_equity = 16,        // Debt to Equity ratio (quarterly)
        enterprise_value = 32,      // Real cost to buy company (daily)
        pe_ratio = 64,              // PE ratio (TTM)
        peg_ratio = 128,            // PE Growth (TTM)
        ps_ratio = 256,             // Price to Sales ratio (TTM)
        return_on_assets = 512,     // (TTM)
        revenue = 1024,             // (quarterly)
        revenue_yoy = 2048,         // Revenue Growth (quarterly)
        roe = 4096,                 // Return on Equity (TTM)
        revenue_ttm = 8192,         // Revenue (TTM) -- note get "monthly_revenue_ttm"
        expenses_total_ttm = 16384, // Total Expenses (TTM) -- note get "monthly_expenses_total_ttm"
        net_income_ttm = 32768,     // Net Income (TTM) -- "monthly_net_income_ttm" - note daily/weekly/monthly - all actually quarterly dates, net_income = quarterly
        All = ~None
    }

    [Table("StockFundamentals")]
    public class StockFundamental
    {
        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(16)]
        public string Ticker { get; set; }

        [Key]
        [Column(Order = 2)]
        [Required]
        public DateTime Date { get; set; }

        [Key]
        [Column(Order = 3)]
        [Required]
        public int Type { get; set; }

        [Required]
        public double Value { get; set; }
    }
}