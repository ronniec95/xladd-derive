# xladd-derive
Macros to help write Excel User defined functions easily in Rust

# Version 0.2 release notes
* Added a new feature: *prefix* which can be used to name your functions. Previously all the excel exported functions were called "xl_myfunction", now with `prefix = "project_"` your exports are renamed to `project_myfunction`. If it's not specified it defaults to "xl_".
* Added *rename* which renames the Excel exposed function to whatever specified. The prefix still stands.

## Why?

I find quickly knocking up a sample GUI in Excel by drag/dropping extremely good for prototyping. Writing a test function and being able to pass all sorts of data to it interactively is a useful feature

# Background
I found xladd from MarcusRainbow which enabled a raw api to create **user defined functions** for Excel. I wasn't totally happy with it and raised a couple of PRs against his project but didn't get any responses so I forked his project and contined to add functionality as I saw fit.

But it was still a pain and I wanted to learn about proc-macros so created this proc-macro crate to make it easier to write functions in Rust.

## Usage

Add

    [dependencies]
    xladd-derive="0.1"
    xladd = {git="https://github.com/ronniec95/xladd}

to your Cargo.toml

Write a Rust function and add the following annotation `#[xl_func()]` like

    #[xl_func()]
    fn add(arg1: f64, arg2: f64) -> Result<f64, Box<dyn std::error::Error>> {
        // No comments in this method, defaults will be allocated
        Ok(arg1 + arg2)
    }

    /// This function adds any number of values together
    /// * v - array of f64
    /// * ret - returns the sum0
    #[xl_func()]
    fn add_array(v: &[f64]) -> Result<f64, Box<dyn std::error::Error>> {
        // Comments will be filled into the Excel function dialog box
        Ok(v.iter().sum())
    }

    /// This function adds any number of values together
    /// * v - array of f64
    /// * ret - returns the sum
    #[xl_func(category="MyCategory")] // Custom attribute to assign function to a particular category
    fn add_array_v2(v: &[f64]) -> Result<(Vec<f64>, usize), Box<dyn std::error::Error>> {
        // Comments will be filled into the Excel function dialog box
        // This returns a 2d array to excel using a (vec,usize) tuple. Note that if v.len() / columns != 0 you will have missing values
        Ok((v.to_vec(), 2))
    }

Right now there are a couple of restrictions which I hope to remove down the line

The return type of your function can be a `Result<type,Box<dyn std::error::Error>>` of any basic type:
 
- f32
- f64
- i32
- i64
- bool
- String (owned)

or a tuple of 

- (Vec<[basic type]>,usize)

where the second parameter is the number of columns. This allows Excel to handle arrays of 2d data. The macro will calculate the rows from the size of the array.

I was thinking of making the input `&[]` arrays also be a tuple if there is demand for it.

Arguments are taken as LPXLOPER12 args which are then coerced to the Rust types. Errors in coercion are reported via a trace!() log. If you run Excel from the command line with env-logger or simplelog you could output these to a file for debugging.

## Documentation

The doc comments are interpreted in the following manner

    /// This is a normal multiline comment that 
    /// will be used as a description of the function
    /// * arg1 - This argument must be formatted with a `* <name> -` and will be used in the argument description
    /// * ret - This is a special return type argument which will appended to the description of the function

## Multithreading

Excel uses however many cores there are on the machine, but it relies on your UDFs being thread safe. Rust is multithread friendly, but watch out if you are reading/writing files.

If you want to control this aspect through an attribute, let me know.

## Registration with Excel

Excel calls this function in your .dll when it starts. The macro generates the register_* functions for you so follow this template. If someone knows how to automatically determine these from a proc-macro, please get in touch, or raise a PR

    pub extern "stdcall" fn xlAutoOpen() -> i32 {
        let reg = Reg::new();
        register_add(&reg);
        1 // Must return 1 to signal to excel SUCCESS
    }

## xladd dependency

As I cannot seem to be able to get in touch with MarcusRainbow, the original author of the xladd crate, I've created a fork of that, so in `Cargo.toml` you need to add a github dependency `xladd = { git ="https://github.com/ronniec95/xladd"}`. Let me know if that is a problem and I can see if there's a better way

## Unsafe code & Excel compatibility

As the xladd package calls into the Windows C api it's inherently unsafe.
This package uses the LPXLOPER12 api for excel so is compatible with 2007 onwards.
If the add-in is not working, check that your excel is the samme bit size (32 or 64bit) as your `rustc` compiler. Often Excel is installed as 32bit in a lot of organisations and your rustc compiler is probably 64bit. This will natually not work.

## Not yet handled

Asynchronous methods. I've not had the need for this especially as network and IO type work is much better done within Excel itself.

I also would like to add RTD support so you can subscribe to live data.

## Debugging

Within VSCode you can create a configuration for debugging and change the `program` to 

     "program": "C:/Program Files/Microsoft Office/root/Office16/EXCEL.EXE",
    
This will launch excel but you can set breakpoints in your code.