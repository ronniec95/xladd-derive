// Black scholes with ndarray
// Option pricer with ndarray
// Excel interface for both
// Mesh interface for both

/*
public static double BlackScholes(bool call, double S, double K, double T, double r, double v, double q)
{
    double dBlackScholes = 0.0;

    double F = S * Math.Exp((r - q) * T);

    double d1 = (Math.Log(F / K) + (v * v / 2.0) * T) / (v * Math.Sqrt(T));
    double d2 = d1 - v * Math.Sqrt(T);

    if (call)
        dBlackScholes = Math.Exp(-r * T) * (F * NormDistHelper.CND(d1) - K * NormDistHelper.CND(d2));
    else
        dBlackScholes = Math.Exp(-r * T) * (K * NormDistHelper.CND(-d2) - F * NormDistHelper.CND(-d1));

    return dBlackScholes;
}

public static double GetImpliedVol(bool call, double optionPrice, double assetPrice, double strike, double expiry, double interest, double dividend)
        {
            // TODO: There can be cases where the optionPrice is just out of the range given the variables...
            // so should have a MaxIterations, and return current value or throw Exception
            const int maxIterations = 75;
            int numIterations = 0;

            double lower = 0.001;
            double upper = Constants.MaxPermittedVol;
            double iv = 0, lastIv = 1;
            double ivTolerance = 0.00001;
            double priceTolerance = 0.001;
            double calcPrice = 0;

            if (optionPrice < 0.005)
                return 0;

            if (optionPrice < 10)
                priceTolerance = 0.001 * optionPrice;

            double upperPrice = BlackScholes(call, assetPrice, strike, expiry, interest, upper, dividend);
            double lowerPrice = BlackScholes(call, assetPrice, strike, expiry, interest, lower, dividend);

            if (optionPrice > upperPrice)
                return upper;

            if (optionPrice < lowerPrice)
                return 0;	// save some time by ignoring prices that black scholes won't calculate

            // Keep guessing iv until we find something within tolerance
            while (Math.Abs(iv - lastIv) > ivTolerance)
            {
                numIterations++;

                // guess a new vol based on if calcPrice too high or too low
                if (calcPrice > optionPrice)
                {
                    //upperPrice = calcPrice;
                    upper = iv;
                }
                else
                {
                    //lowerPrice = calcPrice;
                    lower = iv;
                }
                // Is this a better gues than bisection method... ?
                lastIv = iv;
                iv = (upper + lower) / 2;
                //iv = (optionPrice - lowerPrice) / (upperPrice - lowerPrice) * (upper - lower) + lower;

                calcPrice = BlackScholes(call, assetPrice, strike, expiry, interest, iv, dividend);

                if (Math.Abs(calcPrice - optionPrice) < priceTolerance)
                    return iv;

                if (numIterations > maxIterations)
                {
                    Console.WriteLine("Unable to determine IV within {0} iterations. Returning iv={1:F3}", maxIterations, iv);
                    return iv; // double.NaN;//.PositiveInfinity;
                }
            }

            return (double)iv;
        }
        */
