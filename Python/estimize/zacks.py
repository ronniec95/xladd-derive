from bs4 import BeautifulSoup
import csv
import sys
import json
import utils
from sqlserver import conn, TableDef, create_table, delete, insert


def earnings(symbol):
    raw_html = utils.download_get(
        "https://www.zacks.com/stock/research/{}/earnings-announcements".format(symbol))
    html = BeautifulSoup(raw_html, 'html.parser')
    for script in html.select("script"):
        script = str(script)
        start = script.find("obj_data = {")
        if not start == -1:
            end = script.rfind("}")
            array = json.loads(script[start+11:end+1])
            for (key, value) in array.items():
                with open("{}_{}.csv".format(symbol, key), "w") as f:
                    writer = csv.writer(f)
                    for row in value:
                        row_data = []
                        for item in row:
                            if item.startswith("--"):
                                continue
                            elif item.startswith("<"):
                                html = BeautifulSoup(item, 'html.parser')
                                row_data += [html.text]
                            else:
                                row_data += [item]
                        writer.writerow(row_data)


def fundamentals(symbol):
    cxn = conn()

    for type in [
        # "book_value_per_share",
        #          "eps_diluted_ttm",
        #          "eps_diluted",
        #          "q_profit_margin",
        #          "debt_to_equity",
        #          "enterprise_value",
        #          "volume",
        #          "pe_ratio",
        #          "peg_ratio",
        #          "price_and_eps_estimates_consensus",
        #          "ps_ratio",
        "return_on_assets",
        "revenue",
        "revenue_yoy",
        "roe",
        "revenue_ttm",
        "expenses_total_ttm",
            "net_income_ttm"]:

        table_def = TableDef(
            "Zacks" + type, [('Symbol', 'nvarchar'), ('KeyName', 'nvarchar'), ('ValueName', 'nvarchar')])
        create_table(cxn, table_def)

        data = json.loads(utils.download_get(
            "https://widget3.zacks.com/data/chart/json/{}/{}/www.zacks.com".format(symbol, type)))
        for item_name in data.keys():
            with open("{}_{}.csv".format(symbol, item_name), "w") as f:
                writer = csv.writer(f)
                for (key, value) in data[item_name].items():
                    writer.writerow([key, value])
                    delete(cxn, table_def, [symbol, key])
                    insert(cxn, table_def, [symbol, key, value])


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("need a list of stock tickers")
        exit(-1)
    for symbol in sys.argv[1:]:
        fundamentals(symbol)
#        earnings(symbol)
