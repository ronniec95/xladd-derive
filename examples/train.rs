use xladd::Reg;
use xladd_derive::xl_func;

#[unsafe(xl_func())]
fn add(arg1: f64, arg2: f64) -> Result<f64, Box<dyn std::error::Error>> {
    Ok(arg1 + arg2)
}

/// This function adds any number of values together
/// * v - array of f64
/// * ret - returns the sum0
#[xl_func()]
fn add_array(v: &[f64]) -> Result<f64, Box<dyn std::error::Error>> {
    Ok(v.iter().sum())
}

/// This function adds any number of values together
/// * v - array of f64
/// * ret - returns the sum
#[xl_func()]
fn add_array_v2(v: &[f64]) -> Result<(Vec<f64>, usize), Box<dyn std::error::Error>> {
    Ok((v.to_vec(), 2))
}

/// This function adds any number of values together
/// * v - array of f64
/// * ret - returns the sum
#[unsafe(xl_func())]
fn add_str(a: &str, b: &str) -> Result<String, Box<dyn std::error::Error>> {
    Ok([a, b].join("-"))
}

#[xl_func(category = "OptionPricing", prefix = "my", rename = "foo")]
fn add_str_2(a: &[&str]) -> Result<(Vec<String>, usize), Box<dyn std::error::Error>> {
    Ok((vec![a.join("-")], 1))
}

#[cfg(feature = "use_ndarray")]
use ndarray::Array2;

#[cfg(feature = "use_ndarray")]
#[xl_func(category = "OptionPricing", prefix = "my", rename = "bar")]
fn add_str_3(a: Array2<String>) -> Result<Array2<f64>, Box<dyn std::error::Error>> {
    Ok(Array2::from_elem([2, 2], 0.0f64))
}

#[cfg(feature = "use_ndarray")]
#[xl_func(category = "OptionPricing", prefix = "my", rename = "baz")]
fn add_f64_2(a: Array2<f64>) -> Result<Array2<f64>, Box<dyn std::error::Error>> {
    Ok(Array2::from_elem([2, 2], 0.0f64))
}

// Don't forget to register your functions
#[unsafe(no_mangle)]
pub extern "stdcall" fn xlAutoOpen() -> i32 {
    let r = Reg::new();
    #[cfg(feature = "use_ndarray")]
    register_add_f64_2(&r);
    1
}

fn main() {} // Not needed for actual dll
