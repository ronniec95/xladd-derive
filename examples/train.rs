use xladd::registrator::Reg;
use xladd::variant::Variant;
use xladd::xlcall::LPXLOPER12;
use xladd_derive::xl_func;

/// This normalizes a set of values
/// * arg - Takes a floating point number
/// * foo - Takes an array of values
/// * bar - Takes a string
/// * ret - Returns an array
#[xl_func()]
fn normalize(
    arg: f64,
    foo: &[f64],
    bar: &str,
) -> Result<(Vec<f64>, usize), Box<dyn std::error::Error>> {
    Ok((vec![], 2))
}

#[xl_func()]
fn add(arg1: f64, arg2: f64) -> Result<f64, Box<dyn std::error::Error>> {
    Ok(arg1 + arg2)
}

pub extern "stdcall" fn xlAutoOpen() -> i32 {
    let reg = Reg::new();
    register_normalize(&reg);
    register_add(&reg);
    1
}

fn main() {}
