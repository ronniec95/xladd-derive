use std::hash::Hash;

struct ProcIter<I>
where
    I: Iterator,
{
    curr: I,
}

impl<I> Iterator for ProcIter<I>
where
    I: Iterator,
    I::Item: std::fmt::Debug,
{
    type Item = I::Item;
    fn next(&mut self) -> Option<Self::Item> {
        println!("{:?}", self.curr.next());
        None
    }
}

trait ProcIterExt: Iterator {
    fn proc_iter(self) -> ProcIter<Self>
    where
        Self::Item: Hash + Eq + Clone,
        Self: Sized,
    {
        ProcIter { curr: self }
    }
}

impl<I: Iterator> ProcIterExt for I {}

fn split_process() {
    let v = vec![0u32; 1000];
    let context = Context::new(env::args());
    v.chunks(10).proc_iter().for_each(|_sl| {})
}

#[cfg(test)]
mod tests {
    #[test]
    fn chunk_test() {
        super::split_process();
    }
}
