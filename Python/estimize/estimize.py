from bs4 import BeautifulSoup
import csv
import sys
import json
import utils
from sqlserver import conn, TableDef, create_table, delete, insert


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Need a symbol list")
        exit
    else:
        cxn = conn()
        table_def = TableDef("EstimizeEPS", [('Symbol', 'nvarchar'), ('period', 'nvarchar'), ('actual', 'real'), ('selection', 'real'), (
            'wallstreetMean', 'real'), ('estimizeMean', 'real'), ('estimatesCount', 'real'), ('yearOnYear', 'real')])
        create_table(cxn, table_def)
        for symbol in sys.argv[1:]:
            print("For symbol " + symbol)
            print("Retriving EPS")
            url = "https://www.estimize.com/{}#chart=table"
            raw_html = utils.download_get(url.format(symbol))
            if raw_html is not None:
                html = BeautifulSoup(raw_html, 'html.parser')
                for div in html.select('div'):
                    if 'data' in div.attrs:
                        jobj = json.loads(div.attrs["data"])
                        release_options = []
                        for metric in jobj['presenter']['allReleases']:
                            item = {"id": metric['id'], "subreleases": {"eps": {
                                "historical_estimate": True, "selected_user_historical_estimates": False, "form_estimate": False}}}
                            release_options.append(item)
                        data = json.dumps(release_options)
                        url = "https://www.estimize.com/{}/releases_data?release_slug=fq4-2019&metric_name=eps"
                        results = utils.download_post(url.format(symbol), {
                            "release_options": data})
                        results = json.loads(results)
                        # Print out the estimates
                        with open(symbol + "_eps.csv", "w") as f:
                            writer = csv.writer(
                                f, delimiter=',', dialect='excel')
                            writer.writerow(["name", "actual", "select", "wallstreetMean",
                                             "estimizeMean", "estimatesCount", "yearOverYear"])
                            for sub in results["presenter"]["releases"]:
                                eps = sub["subreleases"]["eps"]
                                writer.writerow([sub["name"], eps["actual"], eps["select"], eps["wallstreetMean"],
                                                 eps["estimizeMean"], eps["estimatesCount"], eps["yearOverYear"]])
                                delete(cxn, table_def, [symbol, sub["name"]])
                                insert(cxn, table_def, [symbol, sub["name"], eps["actual"], eps["select"], eps["wallstreetMean"],
                                                        eps["estimizeMean"], eps["estimatesCount"], eps["yearOverYear"]])

            print("Getting revenue")
            table_def = TableDef("EstimizeRevenue", [('Symbol', 'nvarchar'), ('period', 'nvarchar'), ('actual', 'real'), ('selection', 'real'), (
                'wallstreetMean', 'real'), ('estimizeMean', 'real'), ('estimatesCount', 'real'), ('yearOnYear', 'real')])
            create_table(cxn, table_def)

            url = "https://www.estimize.com/ibm/fq4-2019?metric_name=revenue&chart=table"
            raw_html = utils.download_get(url.format(symbol))
            if raw_html is not None:
                html = BeautifulSoup(raw_html, 'html.parser')
                for div in html.select('div'):
                    if 'data' in div.attrs:
                        jobj = json.loads(div.attrs["data"])
                        release_options = []
                        for metric in jobj['presenter']['allReleases']:
                            item = {"id": metric['id'], "subreleases": {"revenue": {
                                "historical_estimate": True, "selected_user_historical_estimates": False, "form_estimate": False}}}
                            release_options.append(item)
                        data = json.dumps(release_options)
                        url = "https://www.estimize.com/{}/releases_data?release_slug=fq4-2019&metric_name=revenue"
                        results = utils.download_post(url.format(symbol), {
                            "release_options": data})
                        results = json.loads(results)
                        with open(symbol + "_revenue.csv", "w") as f:
                            writer = csv.writer(
                                f, delimiter=',', dialect='excel')
                            writer.writerow(["name", "actual", "select", "wallstreetMean",
                                             "estimizeMean", "estimatesCount", "yearOverYear"])
                            for sub in results["presenter"]["releases"]:
                                eps = sub["subreleases"]["revenue"]
                                writer.writerow([sub["name"], eps["actual"], eps["select"], eps["wallstreetMean"],
                                                 eps["estimizeMean"], eps["estimatesCount"], eps["yearOverYear"]])
                                delete(cxn, table_def, [symbol, sub["name"]])
                                insert(cxn, table_def, [symbol, sub["name"], eps["actual"], eps["select"], eps["wallstreetMean"],
                                                        eps["estimizeMean"], eps["estimatesCount"], eps["yearOverYear"]])
        cxn.commit()
