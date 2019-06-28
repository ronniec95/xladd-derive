 console.log('Submitting form...');

function AARCtoFixed(nbr, dec_places) {
    var mult = Math.pow(10, dec_places);
    return Math.round(nbr * mult) / mult;
}
function AARCtoDate(d) {
    var yyyy = parseInt(d / 10000);
    var mm = parseInt((d - yyyy * 10000) / 100);
    var dd = parseInt(d - yyyy * 10000 - mm * 100);
    var d = new Date(yyyy, mm, dd);
    return d;
}
function AARCunpack(rows, data) {
    rows.forEach(function (row, index) {
        row.close = data.closingPrices[index];
        row.high = data.high[index];
        row.low = data.low[index];
        row.open = data.open[index];
        row.volume = data.volume[index];
    })
}
function AARCdate2Int(date) {
    return date.getFullYear() * 10000 + (date.getMonth() + 1) * 100 + date.getDate();
}
// Page Ready
$(function () {
    console.log('Submitting form...');
    $("#startdatepicker").datepicker();
    $("#startdatepicker").datepicker("setDate", "01/01/2012");
    $("#enddatepicker").datepicker();
    $("#enddatepicker").datepicker("setDate", "06/06/2019");

    var startDate = $("#startdatepicker").datepicker("getDate");
    var endDate = $("#enddatepicker").datepicker("getDate");

    var s_yyyyMMdd = AARCdate2Int(startDate);
    var e_yyyyMMdd = AARCdate2Int(endDate);

    var myData = { "tickers": ["AAPL", "MSFT", "INTL", "AMZN"], "startDate": s_yyyyMMdd, "endDate": e_yyyyMMdd };
    //        var myData = { "tickers": ["AAPL", "MSFT", "INTL", "AMZN"], "startDate": 20190601, "endDate": 20190613 };

    var saveData = $.ajax({
        type: 'POST',
        url: "/api/History",
        data: JSON.stringify(myData),
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        success: function (resultData) {
            var plotData = [];
            var gridRows = [];
            for (var key in resultData) {
                var prices = resultData[key];
                var dates = [];
                prices.dates.forEach(function (d, index) {
                    var date = AARCtoDate(d);
                    dates.push(date);
                    var row = { ticker: key, date: d };
                    gridRows.push(row);
                });
                var data = {
                    x: dates,
                    name: key,
                    close: prices.closingPrices,
                    high: prices.high,
                    low: prices.low,
                    open: prices.open,
                    volume: prices.volume,
                    type: 'ohlc',
                    xaxis: 'x',
                    yaxis: 'y'
                };
                plotData.push(data);
                AARCunpack(gridRows, prices);
            };
            var layout = {
                dragmode: 'zoom',
                margin: {
                    r: 10,
                    t: 25,
                    b: 40,
                    l: 60
                },
                showlegend: false,
                xaxis: {
                    autorange: true,
                    rangeslider: { range: [startDate.toISOString(), '2019-06-13'] },
                    title: 'Date',
                    type: 'date'
                },
                yaxis: {
                    autorange: true,
                    type: 'linear'
                }
            }
            Plotly.plot('myDiv', plotData, layout);
            $("#jsGrid").jsGrid({
                width: "100%",
                height: "400px",
                editing: true,
                sorting: true,
                paging: true,

                data: gridRows,

                fields: [
                    { name: "ticker", type: "text", width: 150, title: "Ticker" },
                    {
                        name: "date", type: "number", width: 60, title: "Date"
                    },
                    { name: "open", type: "number", width: 200, title: "Open" },
                    { name: "high", type: "number", width: 200, title: "High" },
                    { name: "low", type: "number", width: 200, title: "Low" },
                    { name: "close", type: "number", width: 200, title: "Close" },
                    { name: "volume", type: "number", width: 200, title: "Volume" }
                ]
            });
            console.log("Save Complete")
        },
        error: function (err) { console.log("Something went wrong"); }
    })
});