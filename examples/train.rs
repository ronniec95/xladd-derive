use xladd::registrator::Reg;
use xladd::variant::Variant;
use xladd::xlcall::LPXLOPER12;
use xladd_derive::xl_func;

#[xl_func()]
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

// Don't forget to register your functions
#[no_mangle]
pub extern "stdcall" fn xlAutoOpen() -> i32 {
    let r = Reg::new();
    register_add(&r);
    register_add_array(&r);
    register_add_array_v2(&r);
    1
}

fn main() {} // Not needed for actual dll
