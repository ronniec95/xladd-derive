import io
import sys
import json
from yahoo_fin import stock_info
from yahoo_fin import options
from yahoo_fin.stock_info import get_analysts_info
from pandas import DataFrame
from flask import Flask
from datetime import date
import datetime
import sys
import optparse
import time
import gzip, functools
from io import BytesIO as IO

app = Flask(__name__)

@app.route("/")
def index():
    return "AARC Home"

@app.route("/test")
def testdf():
    lst = ['Geeks', 'For', 'Geeks', 'is',  'portal', 'for', 'Geeks']
    df = DataFrame(lst)
    print(df)
    jsonstr = df.to_json(orient='records')
    return str(jsonstr)

@app.route("/date/<int:d>")
def getdate(d):
    dd = int2date(d)
    return str(dd)

@app.route("/stock/index/sp500")
def stockindexsp500():
    sp = stock_info.tickers_sp500()
    print (sp)
    data = {ticker : 0 for ticker in sp}
    return data

@app.route('/stock/index/nasdaq')
def stokcindexnasdaq():
    data = stock_info.tickers_nasdaq()
    return str(data)

@app.route('/stock/index/dow')
def stokcindexdow():
    data = stock_info.tickers_dow()
    return str(data)

@app.route('/stock/index/other')
def stockindexother():
    data = stock_info.tickers_other()
    return str(data)

@app.route("/stock/history/<string:ticker>")
@app.route("/stock/history/<string:ticker>/<int:fromdate>")
@app.route("/stock/history/<string:ticker>/<int:fromdate>/<int:todate>")
def stock_getdata(ticker, fromdate=0, todate=0):
    if fromdate > 0:
        start_date = int2date(fromdate)
    else:
         start_date = None

    if todate > 0:
        end_date = int2date(todate)
    else:
         end_date = None

    Data = stock_info.get_data(ticker, start_date = start_date, end_date = end_date)
    df = DataFrame(Data)
    df.rename(columns={'adjclose': 'adjustedclose'}, inplace=True)
    jsonstr = df.reset_index().to_json(orient='records', date_format="iso")
    return str(jsonstr)

@app.route("/stock/calls/<string:ticker>")
def calls(ticker):
    data = options.get_calls(ticker)
    return str(data)

@app.route("/stock/puts/<string:ticker>")
def puts(ticker):
    data = options.get_puts(ticker)
    return str(data)

@app.route("/stock/live/<string:ticker>")
def live(ticker):
    data = stock_info.get_live_price(ticker)
    return str(data)

@app.route("/stock/info/<string:ticker>")
def stockinfo(ticker):
    data = stock_info.get_stats(ticker)
    df = DataFrame(data)
    jsonstr = df.reset_index().to_json(orient='records', date_format="iso")
    return str(jsonstr)

@app.route("/stock/quote/<string:ticker>")
def stockquota(ticker):
    data = stock_info.get_quote_table(ticker, dict_result = True)
    jsonstr = json.dumps(data)
    return str(jsonstr)

@app.route("/stock/analysts/<string:ticker>")
def stockanalystsinfo(ticker):
    data = stock_info.get_stats(ticker)
    df = DataFrame(data)
    jsonstr = df.reset_index().to_json(orient='records', date_format="iso")
    return str(jsonstr)

@app.route("/stock/holders/<string:ticker>")
def stockaholders(ticker):
    data = stock_info.get_holders(ticker)
    df = DataFrame(data)
    jsonstr = df.reset_index().to_json(orient='records', date_format="iso")
    return str(jsonstr)

def int2date(argdate: int) -> date:
    """
    If you have date as an integer, use this method to obtain a datetime.date object.

    Parameters
    ----------
    argdate : int
      Date as a regular integer value (example: 20160618)

    Returns
    -------
    dateandtime.date
      A date object which corresponds to the given value `argdate`.
    """
    year = int(argdate / 10000)
    month = int((argdate % 10000) / 100)
    day = int(argdate % 100)

    return date(year, month, day)

if __name__ == '__main__':
    print (" * Starting ", __file__, datetime.datetime.now())
    parser = optparse.OptionParser(usage="python simpleapp.py -p ")
    parser.add_option('-p', '--port', action='store', dest='port', help='The port to listen on.')
    (args, _) = parser.parse_args()
    if args.port == None:
        print ("Missing required argument: -p/--port")
        sys.exit(1)
    app.run(host='0.0.0.0', port=int(args.port), debug=False)

#print (len(sys.argv))

#if len(sys.argv) > 1:
#ticker_prices = {}
 #   for i in range(1, len(sys.argv)):
    #    print ("getting ", ticker)
  ##      ticker = sys.argv[i]
     #   data = get_data(ticker, start_date = '01/01/2019')

      #  ticker_prices[ticker] = data
#print (ticker_prices)


#for ticker in sp:
#    print (ticker)
# pull data for each S&P stock
# price_data = {ticker : get_data(ticker) for ticker in sp}

# print (price_data)

