/*
pub extern "stdcall" fn aarc_normalize(
    array: LPXLOPER12,
    min: LPXLOPER12,
    max: LPXLOPER12,
    scale: LPXLOPER12,
) -> LPXLOPER12 {
    match normalize(
        Variant::from(array),
        Variant::from(min),
        Variant::from(max),
        Variant::from(scale),
    ) {
        Ok(v) => LPXLOPER12::from(v),
        _ => LPXLOPER12::from(Variant::from("Invalid")),
    }
}


pub fn normalize(
    array: Variant,
    min: Variant,
    max: Variant,
    norm_type: Variant,
) -> Result<Variant, AARCError> {
    let min: f64 = min.try_into()?;
    let max: f64 = max.try_into()?;
    let norm_type: f64 = norm_type.try_into()?;
    let (x, y) = array.dim();
    let array: Vec<f64> = array.into();
    let result = match norm_type as i64 {
        1 => normalize::tanh_est(&array),
        _ => normalize::min_max_norm(&array, min, max),
    };
    Ok(Variant::convert_float_array(result, x, y))
    // Zscore normalization
    // Tanh Normalization
}
*/

use rust_xl::variant::Variant;
use rust_xl::xlcall::LPXLOPER12;
use xlmacro::*;

#[xl_func()]
fn normalize(arg: f64, foo: &[f64], bar: &str) -> Result<Vec<f64>, Box<dyn std::error::Error>> {
    Ok(vec![])
}

fn main() {
    // assert!(
    //     xl_normalize(
    //         LPXLOPER12::from(Variant::from(1.0)),
    //         LPXLOPER12::from(Variant::convert_float_array(vec![1.0], 1, 1)),
    //         LPXLOPER12::from(Variant::from("hello"))
    //     ) == vec![]
    // );
}
