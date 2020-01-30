const DIGITS: &[u8] = "0123456789abcdefghijklmnopqrstuvwxyz".as_bytes();

pub fn str_radix(name: &str) -> usize {
    name.as_bytes().iter().fold(0, |v, ch| {
        v * 36 + DIGITS.iter().position(|&x| x == *ch).unwrap()
    })
}

pub fn radix_str(value: u64) -> String {
    let mut s = Vec::new();
    let mut value = value as usize;
    while value > 0 {
        s.push(DIGITS[value % 36]);
        value = value / 36;
    }
    s.reverse();
    String::from(std::str::from_utf8(&s).unwrap())
}
