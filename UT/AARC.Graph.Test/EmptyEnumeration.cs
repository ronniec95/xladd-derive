using System;
namespace AARC.Graph.Test
{

    // External {i}setcloseprice  => [closepriceservice] => {o}newcloseprice
    // External {i}setrandomprice => [randompricesrice]  => {o}newrandom
    //
    // [closepriceservice] => {o}newcloseprice =>
    // [randompriceservice] => {o}newrandom =>
    // {i}newcloseprice,{i}newrandom => [method1service] => {o}method1price

    public enum Graph1
    {

    }

    public enum GraphMethod1
    {
        newcloseprice,
        newrandom,
        method1
    }

    public enum GraphNewClosePrice
    {
        setcloseprice, newcloseprice
    }

    public enum GraphNewRamdomPrice
    {
        setrandomprice, newrandom
    }
}
