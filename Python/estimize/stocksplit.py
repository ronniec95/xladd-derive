from bs4 import BeautifulSoup
import csv
import sys
import json
import utils
from sqlserver import conn, TableDef, create_table, delete, insert


def stock_splits(symbol):
    cxn = conn()
    table_def = TableDef("StockSplit", [(
        'Symbol', 'nvarchar'), ('Date', 'nvarchar'), ('Ratio', 'nvarchar')])
    create_table(cxn, table_def)
    html = BeautifulSoup(utils.download_get(
        "https://www.stocksplithistory.com/?symbol={}".format(symbol)), 'html.parser')
    for table_id in html.find_all('b'):
        if "Ratio" in table_id.text:
            for sibling in table_id.parent.parent.parent.next_siblings:
                if not type(sibling).__name__ == "NavigableString":
                    data = sibling.find_all("td")
                    delete(cxn, table_def, [symbol, data[0].text])
                    insert(cxn, table_def, [
                           symbol, data[0].text, data[1].text.strip()])
    cxn.commit()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("need a list of stock tickers")
        exit(-1)
    for symbol in sys.argv[1:]:
        stock_splits(symbol)
