from bs4 import BeautifulSoup
import csv
import sys
import json
import utils
from sqlserver import conn, TableDef, create_table, delete, insert


def estimate_momentum(symbol):
    cxn = conn()
    table_def = TableDef("NasdaqMomentum", [(
        'Symbol', 'nvarchar'), ('fiscalQtrEndText', 'nvarchar'), ('fiscalYearEndText', 'nvarchar'), ('qtrMean', 'nvarchar'), ('yrMean', 'nvarchar')])
    create_table(cxn, table_def)
    data = json.loads(utils.download_get(
        "https://api.nasdaq.com/api/analyst/{}/estimate-momentum".format(
            symbol.upper())))
    data = data['data']
    symbol = data['symbol']
    change = data['changeInConsensus']
    current = change['currentData']
    delete(cxn, table_def, [symbol, change['fiscalQtrEndText']])
    insert(cxn, table_def, [symbol, change['fiscalQtrEndText'],
                            change['fiscalYearEndText'], current['qtrMean'], current['yrMean']])
    cxn.commit()


def earnings_forecast(symbol):
    cxn = conn()
    table_def = TableDef("NasdaqEarningsForecast", [(
        'Symbol', 'nvarchar'),  ('FiscalEnd', 'nvarchar'), ('Period', 'nvarchar'), ('ConsensusEPSForecast', 'real'), ('HighEPSForecast', 'real'), ('LowEPSForecast', 'real'), ('NumEstimates', 'int')])
    create_table(cxn, table_def)
    data = json.loads(utils.download_get(
        "https://api.nasdaq.com/api/analyst/{}/earnings-forecast".format(
            symbol.upper())))
    data = data['data']
    symbol = data['symbol']
    with open('nasdaq_earnings_forecast_{}.csv'.format(symbol), 'w') as f:
        writer = csv.writer(f)
        writer.writerow(["Period", "Description", "ConsensusEPSForecast",
                         "HighEPSForecast", "LowEPSForecast", "NumEstimates"])
        quarterly = data['quarterlyForecast']['rows']
        for row in quarterly:
            delete(cxn, table_def, [symbol, row['fiscalEnd']])
            insert(cxn, table_def, [symbol, row['fiscalEnd'], 'Quarterly', row['consensusEPSForecast'],
                                    row['highEPSForecast'], row['lowEPSForecast'], row['noOfEstimates']])
            writer.writerow(["Quarterly", row['fiscalEnd'], row['consensusEPSForecast'],
                             row['highEPSForecast'], row['lowEPSForecast'], row['noOfEstimates']])
    cxn.commit()


def earnings_surprise(symbol):
    cxn = conn()
    table_def = TableDef("NasdaqEarningsSurprise", [(
        'Symbol', 'nvarchar'),  ('EndDate', 'nvarchar'), ('Description', 'nvarchar'), ('dateReported', 'nvarchar'),  ('eps', 'nvarchar'), ('consensusForecast', 'nvarchar'), ('percentageSurprise', 'nvarchar')])
    create_table(cxn, table_def)

    data = json.loads(utils.download_get(
        "https://api.nasdaq.com/api/company/{}/earnings-surprise".format(
            symbol.upper())))
    data = data['data']
    symbol = data['symbol']
    with open('nasdaq_earnings_surprise_{}.csv'.format(symbol), 'w') as f:
        writer = csv.writer(f)
        writer.writerow(["Period", "Description", "DateReported",
                         "EPS", "ConsensusForecast", "PercentageSurprise"])
        surprise = data['earningsSurpriseTable']['rows']
        for row in surprise:
            delete(cxn, table_def, [symbol, row['fiscalQtrEnd']])
            insert(cxn, table_def, [symbol, row['fiscalQtrEnd'], 'Quarterly', row['dateReported'],
                                    row['eps'], row['consensusForecast'], row['percentageSurprise']])
    cxn.commit()


def eps(symbol):
    cxn = conn()
    table_def = TableDef("NasdaqEps", [(
        'Symbol', 'nvarchar'),  ('Period', 'nvarchar'), ('Type', 'nvarchar'),  ('Consensus', 'nvarchar'), ('Earnings', 'nvarchar')])
    create_table(cxn, table_def)

    data = json.loads(utils.download_get(
        "https://api.nasdaq.com/api/quote/{}/eps".format(
            symbol.upper())))
    data = data['data']
    symbol = data['symbol']
    with open('nasdaq_eps_{}.csv'.format(symbol), 'w') as f:
        writer = csv.writer(f)
        writer.writerow(["Type", "Period", "Consensus", "Earnings"])
        surprise = data['earningsPerShare']
        for row in surprise:
            delete(cxn, table_def, [symbol, row['period']])
            insert(cxn, table_def, [symbol, row['period'], row["type"], row['consensus'],
                                    row['earnings']])
            writer.writerow([row["type"], row['period'], row['consensus'],
                             row['earnings']])
    cxn.commit()


def earnings_dates(symbol):
    cxn = conn()
    table_def = TableDef("NasdaqEarningsDates", [
                         ('Symbol', 'nvarchar'),  ('Date', 'nvarchar'), ('Description', 'nvarchar')])
    create_table(cxn, table_def)
    data = json.loads(utils.download_get(
        "https://api.nasdaq.com/api/analyst/{}/earnings-date".format(
            symbol.upper())))
    data = data['data']
    announcement = data['announcement'].split(":")[1]
    delete(cxn, table_def, [symbol, announcement])
    insert(cxn, table_def, [symbol, announcement,
                            data['reportText'].replace("'", "")])
    cxn.commit()


def dividend_history(symbol):
    cxn = conn()
    table_def = TableDef("NasdaqDividendHistory", [(
        'Symbol', 'nvarchar'),  ('exOrEffDate', 'nvarchar'), ('DivTpe', 'nvarchar'), ('amount', 'nvarchar'), ('declarationDate', 'nvarchar'), ('recordDate', 'nvarchar'), ('paymentDate', 'nvarchar')])
    create_table(cxn, table_def)

    data = json.loads(utils.download_get(
        "https://api.nasdaq.com/api/quote/{}/dividends?assetclass=stocks".format(
            symbol.upper())))
    data = data['data']
    with open('nasdaq_dividend_history_{}.csv'.format(symbol), 'w') as f:
        writer = csv.writer(f)
        writer.writerow(["exEFFDate", "Type", "Cash",
                         "Decl Date", "Record Date", "Payment Date"])
        dividends = data['dividends']['rows']
        for row in dividends:
            writer.writerow([row["exOrEffDate"], row['type'], row['amount'],
                             row['declarationDate'], row['recordDate'], row['paymentDate']])
            delete(cxn, table_def, [symbol, row['exOrEffDate']])
            insert(cxn, table_def, [symbol, row["exOrEffDate"], row['type'], row['amount'],
                                    row['declarationDate'], row['recordDate'], row['paymentDate']])
    cxn.commit()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("need a list of stock tickers")
        exit(-1)
    for symbol in sys.argv[1:]:
        earnings_dates(symbol)
        eps(symbol)
