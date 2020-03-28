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

pub extern "stdcall" fn xlAutoOpen() -> i32 {
    let reg = Reg::new();
    register_normalize(&reg);
    1
}

fn main() {}
