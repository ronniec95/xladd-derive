# xladd-derive
Macros to help write Excel User defined functions easily in Rust

# Version 0.61 release notes
Some minor bug fixes in xladd integrated in

# Version 0.6 release notes
Update to use ndarray 0.14
Support for sref which allows use of index and offset ranges in excel
Some minor bug fixes

# Version 0.5 release notes

* The main new feature is that arrays can now be 1078,576 rows in length which is the maximum that excel supports
* To support that profiled the code as much as possible to reduce copying of arrays around. I'm sure there can be further improvements
* Async UDFs are now supported. They are supported ok in Excel but it's not a great idea to have too many of them as they screw up Excel's dependency management. However functions are marked as threadsafe, and should be in Rust (no globals) so you should be ok. Watch out for overwrites though
* make_row and make_col variant arrays are supported now to help return tables.
* RTD and Ribbon Bar support coming soon

# Version 0.4 release notes

## New features
* Previously, when calling a function from excel, if the user made a mistake with paramter entry it would return a generic "error". Now we get "missing parameter [name] for function [xxx]" which is a better use experience.
* It also traps where a particular type was expected and it's not parseable.
* trace logging added. If logging is enabled and is set to LevelFilter::Trace then you can get a log of every function call, parameters passed and resulting values. This does have a small impact on performance that I've measured even when disabled. I would recommend having a Excel UDF created that you could call such as `enable_trace_logging` that outputs to a file that is called on demand in your testing spreadsheet
* Nan values are converted to #N/A in excel. This I found useful when user inputs were out of bounds and produced NAN values in some mathematical functions. Rather than crashing or skipping we get a #NA in excel telling us that we need to look at the inputs.
* Dependency on log::* crate added

# Version 0.3 release notes

## New features
* Main new feature is the ability to have 2d arrays going in and out through using NDArray.
* Added a feature flag "use_ndarray". But I cannot see how to pass it through to xladd automatically if the old version of the crate didn't have it as a feature? PR welcome
  This allows you to use Array2<f64> or Array2<String> types as input or output parameters. This fixes the problem of 2d arrays which was a hacky solution at best before
  Using &[f64] is still supported as before and still makes sense for single column or row data
  Using (Vec<f64>,usize) as a return type is still supported but I think it's ugly as it doesn't really show the intention of the developer

## Bugfixes

* If you specify an array (vec or array2) and you are dragging a range of values, the first cell is actually sent as a single f64 and not a range. I didn't handle this case before and could lead to a crash

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

    [lib]
    crate-type = ["cdylib"]

    [dependencies]
    xladd-derive= {"^0.4" }
    xladd = {git="https://github.com/ronniec95/xladd" , features=["use_ndarray"] } # Needed to patch the old abandoned crate

to your Cargo.toml

Write a Rust function and add the following annotation `#[xl_func()]` like

    // These imports are needed from xladd
    use xladd::registrator::Reg;
    use xladd::variant::Variant;
    use xladd::xlcall::LPXLOPER12;
    use xladd_derive::xl_func;
    use log::*; // Needed from 0.4.* onwards to give tracing

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

    use ndarray::Array2;
    /// 2d arrays can now be accepted and returned opening up a lot more possibilities for interfacing with Excel
    #[xl_func(category = "OptionPricing", prefix = "my", rename = "baz")]
    fn add_f64_2(a: Array2<f64>) -> Result<Array2<f64>, Box<dyn std::error::Error>> {
        Ok(Array2::from_elem([2, 2], 0.0f64))
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

    // For excel to register this XLL ensure `no_mangle` is specified 
    #[no_mangle]
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