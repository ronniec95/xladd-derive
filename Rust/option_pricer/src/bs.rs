use crate::norm_dist::*;
use ndarray::{Array1, Zip};

pub struct Greeks {
    call_price: Array1<f64>,
    put_price: Array1<f64>,

    call_delta: Array1<f64>,
    put_delta: Array1<f64>,

    call_theta: Array1<f64>,
    put_theta: Array1<f64>,

    call_rho: Array1<f64>,
    put_rho: Array1<f64>,

    gamma: Array1<f64>,
    vega: Array1<f64>,
}

pub fn bs_call(
    s: &Array1<f64>,
    k: &Array1<f64>,
    t: &Array1<f64>,
    r: &Array1<f64>,
    v: &Array1<f64>,
    q: &Array1<f64>,
) -> Array1<f64> {
    let f = (((r - q) * t).mapv(f64::exp)) * s;
    let tsq = t.mapv(f64::sqrt);
    let d1 = (&f / k).mapv(f64::log10) + ((v * v / 2.0) * t) / (v * &tsq);
    let d2 = &d1 - &(v * &tsq);
    (-r * t).mapv(f64::exp) * (f * nd2(&d1) - (k * &nd2(&d2)))
}

pub fn bs_put(
    s: &Array1<f64>,
    k: &Array1<f64>,
    t: &Array1<f64>,
    r: &Array1<f64>,
    v: &Array1<f64>,
    q: &Array1<f64>,
) -> Array1<f64> {
    let f = (((r - q) * t).mapv(f64::exp)) * s;
    let tsq = t.mapv(f64::sqrt);
    let d1 = (&f / k).mapv(f64::log10) + ((v * v / 2.0) * t) / (v * &tsq);
    let d2 = &d1 - &(v * &tsq);
    (-r * t).mapv(f64::exp) * (k * &nd2(&-d2) - (f * &nd2(&-d1)))
}

pub fn bs_price_single(call: bool, s: f64, k: f64, t: f64, r: f64, v: f64, q: f64) -> f64 {
    let f = (((r - q) * t).exp()) * s;
    let tsq = t.sqrt();
    let d1 = (&f / k).log10() + ((v * v / 2.0) * t) / (v * tsq);
    let d2 = &d1 - &(v * &tsq);
    if call {
        (-r * t).exp() * (f * nd2_single(d1) - (k * nd2_single(d2)))
    } else {
        (-r * t).exp() * (k * nd2_single(-d2) - (f * nd2_single(-d1)))
    }
}

pub fn greeks(
    price: &Array1<f64>,
    strike: &Array1<f64>,
    interest: &Array1<f64>,
    dividend: &Array1<f64>,
    volatility: &Array1<f64>,
    expiry: &Array1<f64>,
) -> Greeks {
    let tsq = expiry.mapv(f64::sqrt);
    let volsq = &(volatility * volatility);
    let pricesq = &(price * price);
    let sst = &(volatility * &tsq);
    let d1 =
        ((price / strike).mapv(f64::log10) + (interest - dividend + volsq / 2.0) * expiry) / sst;
    let d2 = &d1 - sst;
    let nd1 = &nd2(&d1);
    let nd2 = &nd2(&d2);
    let pd1 = &pdf_stdgauss(&d1);
    let exp_interest = (-interest * expiry).mapv(f64::exp);
    let exp_dividend = (-dividend * expiry).mapv(f64::exp);
    let call_price = price * &exp_dividend * nd1 - strike * &exp_interest * nd2;
    let put_price = &call_price + &(strike * &exp_interest) - price * &exp_dividend;

    let call_delta = &exp_dividend * nd1;
    let put_delta = &call_delta - &exp_dividend;
    let gamma = &exp_dividend * pd1 / (price * sst);
    let call_theta = &(dividend * price) * &(&exp_dividend * nd1)
        - interest * strike * &exp_interest * nd2
        - 0.5 * &(volsq * pricesq) * &gamma;
    let put_theta =
        &call_theta + &(interest * strike * &exp_interest) - dividend * price * &exp_dividend;
    let vega = price * &exp_dividend * pd1 * tsq;
    let call_rho = strike * &exp_interest * nd2 * expiry;
    let put_rho = strike * &exp_interest * (nd2 - 1.0) * expiry;

    Greeks {
        call_price,
        put_price,

        call_delta,
        put_delta,

        call_theta,
        put_theta,

        call_rho,
        put_rho,

        gamma,
        vega,
    }
}

pub fn get_implied_vol_call(
    option_price: Array1<f64>,
    asset_price: Array1<f64>,
    strike: Array1<f64>,
    expiry: Array1<f64>,
    interest: Array1<f64>,
    dividend: Array1<f64>,
) -> Array1<f64> {
    const MAX_ITERATION: usize = 75;
    const MAX_VOL: f64 = 2.99;
    let upper: Array1<f64> = Array1::from_elem(option_price.dim(), MAX_VOL);
    let lower = Array1::<f64>::zeros(option_price.dim());
    let mut upper_p: Array1<f64> =
        bs_call(&asset_price, &strike, &expiry, &interest, &upper, &dividend);
    let mut lower_p: Array1<f64> =
        bs_call(&asset_price, &strike, &expiry, &interest, &lower, &dividend);

    let mut iv = Array1::<f64>::zeros(option_price.dim());
    Zip::from(&mut iv)
        .and(&option_price)
        .and(&upper_p)
        .and(&lower_p)
        .apply(|res, op, u, l| {
            *res = if op > u {
                *u
            } else if op < l {
                0.0
            } else {
                u + l / -2.0
            }
        });
    // This is slower using bisection
    for i in 0..option_price.dim() {
        let mut iv = iv[i];
        let mut last_iv = 0.0;
        if iv < 0.0 {
            for _ in 0..MAX_ITERATION {
                if (iv - last_iv).abs() < 0.0001 {
                    break;
                }
                let new_price = bs_price_single(
                    true,
                    asset_price[i],
                    strike[i],
                    expiry[i],
                    interest[i],
                    -iv,
                    dividend[i],
                );
                if new_price > option_price[i] {
                    upper_p[i] = -iv;
                } else {
                    lower_p[i] = -iv;
                }
                last_iv = -iv;
                iv = (upper_p[i] + lower_p[i]) / -2.0;
            }
        }
    }
    iv.mapv(f64::abs)
}

pub fn get_implied_vol_put(
    option_price: Array1<f64>,
    asset_price: Array1<f64>,
    strike: Array1<f64>,
    expiry: Array1<f64>,
    interest: Array1<f64>,
    dividend: Array1<f64>,
) -> Array1<f64> {
    const MAX_ITERATION: usize = 75;
    const MAX_VOL: f64 = 2.99;
    let upper: Array1<f64> = Array1::from_elem(option_price.dim(), MAX_VOL);
    let lower = Array1::<f64>::zeros(option_price.dim());
    let mut upper_p: Array1<f64> =
        bs_put(&asset_price, &strike, &expiry, &interest, &upper, &dividend);
    let mut lower_p: Array1<f64> =
        bs_put(&asset_price, &strike, &expiry, &interest, &lower, &dividend);

    let mut iv = Array1::<f64>::zeros(option_price.dim());
    Zip::from(&mut iv)
        .and(&option_price)
        .and(&upper_p)
        .and(&lower_p)
        .apply(|res, op, u, l| {
            *res = if op > u {
                *u
            } else if op < l {
                0.0
            } else {
                u + l / -2.0
            }
        });
    // This is slower using bisection
    for i in 0..option_price.dim() {
        let mut iv = iv[i];
        let mut last_iv = 0.0;
        if iv < 0.0 {
            for _ in 0..MAX_ITERATION {
                if (iv - last_iv).abs() < 0.0001 {
                    break;
                }
                let new_price = bs_price_single(
                    true,
                    asset_price[i],
                    strike[i],
                    expiry[i],
                    interest[i],
                    -iv,
                    dividend[i],
                );
                if new_price > option_price[i] {
                    upper_p[i] = -iv;
                } else {
                    lower_p[i] = -iv;
                }
                last_iv = -iv;
                iv = (upper_p[i] + lower_p[i]) / -2.0;
            }
        }
    }
    iv.mapv(f64::abs)
}
