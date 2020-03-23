use ndarray::{azip, Array1};

const B1: f64 = 0.319381530;
const B2: f64 = -0.356563782;
const B3: f64 = 1.781477937;
const B4: f64 = -1.821255978;
const B5: f64 = 1.330274429;
const P: f64 = 0.2316419;
const C: f64 = 0.39894228;

pub(crate) fn nd2(x: &Array1<f64>) -> Array1<f64> {
    let exp = x.map(|&v| (-v * v / 2.0).exp());
    let cv = x.map(|&v| if v >= 0.0 { 1.0 - C } else { C });
    let t = 1.0f64 / (1.0 + P * x);
    let mut res = Array1::<f64>::zeros(x.dim());
    azip!((res in &mut res,&cv in &cv,&exp in &exp,&t in &t)
            *res = cv * (exp * t * (t * (t * (t * (t * B5 + B4) + B3) + B2) + B1)));
    res
}

pub(crate) fn nd2_single(x: f64) -> f64 {
    let exp = (-x * x / 2.0).exp();
    let cv = if x >= 0.0 { 1.0 - C } else { C };
    let t = 1.0f64 / (1.0 + P * x);
    cv * (exp * t * (t * (t * (t * (t * B5 + B4) + B3) + B2) + B1))
}

pub(crate) fn pdf_stdgauss(x: &Array1<f64>) -> Array1<f64> {
    const C2: f64 = 0.39894228;
    (-x * x / 2.0).mapv(f64::exp) * C2
}
/*
using System;
using AARC.Utilities;

namespace AARC.QuantLib.Statistics
{
    /*
     * also
     * http://social.msdn.microsoft.com/Forums/en/csharpgeneral/thread/504672ef-e5bc-4af9-bc61-7e5f1809d4cb
     * http://www.johndcook.com/csharp_phi.html
     */


    // http://bytes.com/topic/c-sharp/answers/240995-normal-distribution
    public class NormDistHelper
    {
        public static double N(double x)
        {
            return pdf_stdgauss(x);
        }

        public static double CND(double x)
        {
            return ND2(x);  // for efficiency
            //return Phi(x);  // for accuracy
        }

        // TESTING:
        // 1] put all the errf fns together
        // 2] the cdf fns
        // 3] the pft fns
        // then sort by implementation...
        // test each one for accuracy and speed and choose best


        public static double[,] TestNDH()
        {
            double[] x = { -3, -1, 0.0, 0.5, 2.1 };
            // Output computed by Mathematica
            // y = Phi[x]
            double[] y = { 0.00134989803163, 0.158655253931, 0.5, 0.691462461274, 0.982135579437 };

            // and pdf... 0.004431848411938, 0.24197072, 0.39894228,

            // gaussian error function results - calculated by http://scistatcalc.blogspot.co.uk/2013/10/gauss-error-function-calculator.html
            double[] erf = { -0.999977909503024, -0.842700792949715, 0, 0.520499877813047, 0.997020533343666 };

            /*
                *** Timing Results ***
                ND2: 1056888
                Phi: 1850548
                Phi2: 1534364

                *** Error Results
                ND2 error: 1.45693178152688E-07
                Phi error: 4.59888532735198E-08
                Phi2 error: 1.43564259472888E-07
            */

            // test cdf
            double[,] results = new double[6, 6];
            int numIterations = 1000000;

            ProfileTimer.Start("ND2");
            for (int j = 0; j < numIterations; j++)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    results[i, 3] = ND2(x[i]);
                }
            }
            ProfileTimer.Stop("ND2");
            Console.WriteLine("ND2: " + ProfileTimer.Info["ND2"].SW.ElapsedMilliseconds);

            ProfileTimer.Start("Phi");
            for (int j = 0; j < numIterations; j++)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    results[i, 4] = Phi(x[i]);      // seems closest to mathematica to ~6-7 decimal places
                }
            }
            ProfileTimer.Stop("Phi");
            Console.WriteLine("Phi: " + ProfileTimer.Info["Phi"].SW.ElapsedMilliseconds);

            ProfileTimer.Start("Phi2");
            for (int j = 0; j < numIterations; j++)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    results[i, 5] = Phi2(x[i]);
                }
            }
            ProfileTimer.Stop("Phi2");
            Console.WriteLine("Phi2: " + ProfileTimer.Info["Phi2"].SW.ElapsedMilliseconds);

            for (int i = 0; i < x.Length; i++)
            {
                results[5, 3] += Math.Abs(results[i, 3] - y[i]);  // ND2
                results[5, 4] += Math.Abs(results[i, 4] - y[i]);  // Phi
                results[5, 5] += Math.Abs(results[i, 5] - y[i]);  // Phi2
            }

            Console.WriteLine("ND2 error: " + results[5, 3]);       // fastest
            Console.WriteLine("Phi error: " + results[5, 4]);       // most accurate
            Console.WriteLine("Phi2 error: " + results[5, 5]);      // another formulation

            // ND2 is the fastest with error < 1.5E-07


            return results;
        }

        //
        // PDF FUNCTIONS
        //

        private static double NormDist(double z)
        {
            return (1.0 / Math.Sqrt(2.0 * Math.PI)) * Math.Exp(-0.5 * z * z);
        }

        private static double pdf_stdgauss(double x)
        {
            const double c2 = 0.39894228;  // 1/sqrt(2*PI)
            return Math.Exp(-x * x / 2.0) * c2;
            //return Math.Exp(-x * x / 2.0) / Math.Sqrt(2.0 * Math.PI);
        }

        //
        // CDF FUNCTIONS
        //

        public static double ND2(double x)
        {
            //if (x > 6.0) { return 1.0; }; // this guards against overflow
            //if (x < -6.0) { return 0.0; };

            const double b1 = 0.319381530;
            const double b2 = -0.356563782;
            const double b3 = 1.781477937;
            const double b4 = -1.821255978;
            const double b5 = 1.330274429;
            const double p = 0.2316419;
            const double c = 0.39894228;

            if (x >= 0.0)
            {
                double t = 1.0 / (1.0 + p * x);
                return (1.0 - c * Math.Exp(-x * x / 2.0) * t * (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1));
            }
            else
            {
                double t = 1.0 / (1.0 - p * x);
                return (c * Math.Exp(-x * x / 2.0) * t * (t * (t * (t * (t * b5 + b4) + b3) + b2) + b1));
            }
        }

        private static double Phi2(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }

        // cumulative normal distribution (using erf)
        // phi x = 1/2 * (1 + erf(x / sqrt(2)))
        private static double Phi(double z)
        {
            // 1.4142135623730950488016887242097 = Math.Sqrt(2.0)
            //return 0.5 * (1.0 + erf(z / (Math.Sqrt(2.0))));
            return 0.5 * (1.0 + erf(z / 1.414213562373095));
        }

        // cumulative normal distribution with mean mu and std deviation sigma
        private static double Phi(double z, double mu, double sigma)
        {
            return Phi((z - mu) / sigma);
        }

        private static double NormDist(double x, double mean, double std, bool cumulative)
        {
            if (cumulative)
            {
                return Phi(x, mean, std);
            }
            else
            {
                double tmp = 1 / ((Math.Sqrt(2 * Math.PI) * std));
                return tmp * Math.Exp(-.5 * Math.Pow((x - mean) / std, 2));
            }
        }

        //
        // ERF FUNCTIONS
        //

        // http://www.johndcook.com/csharp_phi.html
        // http://www.johndcook.com/csharp_erf.html
        private static double ERF(double x)
        {
            // constants
            double a1 =  0.254829592;
            double a2 = -0.284496736;
            double a3 =  1.421413741;
            double a4 = -1.453152027;
            double a5 =  1.061405429;
            double p  =  0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A & S 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        //from http://www.cs.princeton.edu/introcs/...Math.java.html
        // fractional error less than 1.2 * 10 ^ -7.
        private static double erf(double z)
        {
            double t = 1.0 / (1.0 + 0.5 * Math.Abs(z));

            // use Horner's method
            double ans = 1 - t * Math.Exp(-z * z - 1.26551223 +
                t * (1.00002368 +
                t * (0.37409196 +
                t * (0.09678418 +
                t * (-0.18628806 +
                t * (0.27886807 +
                t * (-1.13520398 +
                t * (1.48851587 +
                t * (-0.82215223 +
                t * (0.17087277))))))))));

            return (z >= 0) ? ans : -ans;
        }


        // ---

        // Note: Cauchy does not have mean and std, but rather a - mode, and b - scale parameter
        public static double CauchyDist(double x, double mean, double std)
        {
            double a = mean, b = std;
            double v = b / (Math.PI * (Math.Pow(b, 2) + Math.Pow((x - a), 2)));
            //double v = 1 / 2 + (1 / Math.PI) * (Math.Atan((x - mean) / std));
            return v;
        }

        private static double secx(double x)
        {
            return 2 / (Math.Pow(Math.E, x) + Math.Pow(Math.E, -x));
        }

        public static double HypSecDist(double x, double mean, double std)
        {
            double v = (x - mean) / std;
            return (1 / (2 * std)) * secx((Math.PI / 2) * v);
        }

    }
}
*/
