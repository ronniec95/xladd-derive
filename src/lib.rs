use proc_macro::*;
use std::collections::BTreeMap;
use quote::quote;
use syn::{FnArg, ItemFn};




#[proc_macro_attribute]
pub fn xl_func(attr: TokenStream, input: TokenStream) -> TokenStream {
    // println!("{:?}", attr);
    // println!("{:?}", input);
    // println!("{:?}", input);

    let item = syn::parse::<ItemFn>(input).expect("Failed to parse.");
    let mut params = BTreeMap::new();
    let tree = attr.into_iter().collect::<Vec<TokenTree>>();
    for chunk in tree.as_slice().windows(3) {
        match chunk {
            [TokenTree::Ident(i),TokenTree::Punct(_),TokenTree::Literal(l)] => {
                let l = l.to_string();
                params.insert(i.to_string(),l[1..l.len()-1].to_string());
            },
            _ => (),
        }
    }
    let name = item.sig.ident.to_string();
    let category = if let Some(v) = params.get("category") { v } else { "" };
    let prefix = if let Some(v) = params.get("prefix") { v } else { "xl" };
    let rename = if let Some(v) = params.get("rename") { v } else { name.as_str() };
    let async_function = if let Some(_) = params.get("async") { true } else { false };
    let single_threaded = if let Some(_) = params.get("single_threaded") { true } else { true };
    // Use `quote` to convert the syntax tree back into tokens so we can return them. Note
    // that the tokens we're returning at this point are still just the input, we've simply
    // converted it between a few different forms.
    let output = &item.sig.output;
    let func = &item.sig.ident;

    let xl_function = proc_macro2::Ident::new(
        &format!("{}_{}", prefix, rename),
        proc_macro2::Span::call_site(),
    );
    let error_handler_function = proc_macro2::Ident::new(
        &format!("_error_hndlr_{}", func),
        proc_macro2::Span::call_site(),
    );

    let register_function = proc_macro2::Ident::new(
        &format!("register_{}", func),
        proc_macro2::Span::call_site(),
    );
    // From the signature, identify the types we handle
    // f32,f64,i32,i64,bool,&str,&[&str],&[f64]
    // and map them to the corresponding owned types

    let typed_args = &item.sig.inputs.iter().map(|arg| match arg {
        FnArg::Typed(typed_arg) => {
            // Arg name
            let arg_name = {
                let pat = &typed_arg.pat;
                match &**pat {
                    syn::Pat::Ident(ident) => quote!(#ident),
                    _ => panic!("Type not covered"),
                }
            };
            // Owned type
            let owned_type = {
                let ty = &typed_arg.ty;
                match &**ty {
                    syn::Type::Path(p) => {
                        let segment = &p.path.segments[0];
                        let ident = &segment.ident;
                        let p_type = if ident == "str" {
                            quote!(String)
                        } else {
                            quote!(#p)
                        };
                        quote!( 
                            if #arg_name.is_missing_or_null() {
                                return Err(Box::new(xladd::variant::XLAddError::MissingArgument(stringify!(#func).to_string(),stringify!(#arg_name).to_string())));
                            }
                            let #arg_name = std::convert::TryInto::<#p_type>::try_into(&#arg_name)?; 
                            log::trace!("{}:[{:?}]",stringify!(#arg_name),#arg_name);
                        )
                    }
                    syn::Type::Reference(p) => {
                        let elem = &p.elem;
                        // Slice
                        match &**elem {
                            syn::Type::Slice(s) => {
                                let elem = &s.elem;
                                match &**elem {
                                    syn::Type::Path(p) => {
                                        let segment = &p.path.segments[0];
                                        let ident = &segment.ident;
                                        quote!( 
                                                if #arg_name.is_missing_or_null() {
                                                    return Err(Box::new(xladd::variant::XLAddError::MissingArgument(stringify!(#func).to_string(),stringify!(#arg_name).to_string())));
                                                }
                                                let #arg_name = std::convert::TryInto::<Vec<#ident>>::try_into(&#arg_name)?;
                                                log::trace!("{}:[{:?}]",stringify!(#arg_name),#arg_name);
                                                //let #arg_name = #arg_name.as_slice();
                                        )
                                    }
                                    syn::Type::Reference(p) => {
                                        let elem = &p.elem;
                                        match &**elem {
                                            syn::Type::Path(p) => {
                                                let segment = &p.path.segments[0];
                                                let ident = &segment.ident;
                                                if ident == "str" {
                                                    quote!( if #arg_name.is_missing_or_null() {
                                                                return Err(Box::new(xladd::variant::XLAddError::MissingArgument(stringify!(#func).to_string(),stringify!(#arg_name).to_string())));
                                                            }
                                                            let #arg_name = std::convert::TryInto::<Vec<String>>::try_into(&#arg_name)?;
                                                            let #arg_name = #arg_name.iter().map(AsRef::as_ref).collect::<Vec<_>>();
                                                            log::trace!("{}:[{:?}]",stringify!(#arg_name),#arg_name);
                                                            //let #arg_name = #arg_name.as_slice();
                                                    )
                                                } else {
                                                    panic!("Only slices of &[&str] supported")
                                                }
                                            }
                                            _ => panic!("Type not covered"),
                                        }
                                    }
                                    _ => panic!("Type not covered"),
                                }
                            }
                            syn::Type::Path(s) => {
                                let segment = &s.path.segments[0];
                                let ident = &segment.ident;
                                if ident == "str" { 
                                    quote!(
                                        if #arg_name.is_missing_or_null() {
                                            return Err(Box::new(xladd::variant::XLAddError::MissingArgument(stringify!(#func).to_string(),stringify!(#arg_name).to_string())));
                                        }
                                        let #arg_name = std::convert::TryInto::<String>::try_into(&#arg_name)?;
                                        log::trace!("{}:[{:?}]",stringify!(#arg_name),#arg_name);
                                       // let #arg_name = #arg_name.as_str();
                                    )
                                } else { 
                                    quote!(
                                        if #arg_name.is_missing_or_null() {
                                            return Err(Box::new(xladd::variant::XLAddError::MissingArgument(stringify!(#func).to_string(),stringify!(#arg_name).to_string())));
                                        }
                                        let #arg_name = std::convert::TryInto::<#ident>::try_into(&#arg_name)?;
                                        log::trace!("{}:[{:?}]",stringify!(#arg_name),#arg_name);
                                        )
                                    }
                            }
                            _ => panic!("Type not covered"),
                        }

                        // or Path
                    }
                    _ => panic!("Type not covered"),
                }
            };
            // Slice converted for &[&str],&[f64]
            (arg_name, owned_type)
        }
        FnArg::Receiver(_) => panic!("Free functions only"),
    });


    // Rerefence args
    let reference_args = &item.sig.inputs.iter().map(|arg| match arg {
        FnArg::Typed(typed_arg) => {
            // Arg name
            let arg_name = {
                let pat = &typed_arg.pat;
                match &**pat {
                    syn::Pat::Ident(ident) => quote!(#ident),
                    _ => panic!("Type not covered"),
                }
            };
            // Owned type
            let owned_type = {
                let ty = &typed_arg.ty;
                match &**ty {
                    syn::Type::Path(_) => {
                        quote!()
                    }
                    syn::Type::Reference(p) => {
                        let elem = &p.elem;
                        // Slice
                        match &**elem {
                            syn::Type::Slice(s) => {
                                let elem = &s.elem;
                                match &**elem {
                                    syn::Type::Path(_) => {
                                        quote!( let #arg_name = #arg_name.as_slice(); )
                                    }
                                    syn::Type::Reference(p) => {
                                        let elem = &p.elem;
                                        match &**elem {
                                            syn::Type::Path(p) => {
                                                let segment = &p.path.segments[0];
                                                let ident = &segment.ident;
                                                if ident == "str" {
                                                    quote!( let #arg_name = #arg_name.as_slice(); )
                                                } else {
                                                    panic!("Only slices of &[&str] supported")
                                                }
                                            }
                                            _ => panic!("Type not covered"),
                                        }
                                    }
                                    _ => panic!("Type not covered"),
                                }
                            }
                            syn::Type::Path(s) => {
                                let segment = &s.path.segments[0];
                                let ident = &segment.ident;
                                if ident == "str" { 
                                    quote!(let #arg_name = #arg_name.as_str(); )
                                } else { 
                                    quote!()
                                    }
                            }
                            _ => panic!("Type not covered"),
                        }

                        // or Path
                    }
                    _ => panic!("Type not covered"),
                }
            };
            // Slice converted for &[&str],&[f64]
            (arg_name, owned_type)
        }
        FnArg::Receiver(_) => panic!("Free functions only"),
    });
    // Parse the doc comments

    let comments = &item.attrs.iter().filter_map(|attr: &syn::Attribute| {
        let segment = &attr.path.segments[0];
        if segment.ident == "doc" {
            Some(attr.tokens.to_string())
        } else {
            None
        }
    });
    let args = typed_args
        .clone()
        .filter_map(|(name, _)| {
            let name = name.to_string();
            comments.clone().find_map(|v| {
                if v.starts_with(&format!("= \" * {} -", name)) {
                    let v = &v[name.len() + 9..v.len() - 1];
                    Some(quote! {#v})
                } else {
                    None
                }
            })
        })
        .collect::<Vec<_>>();
    let ret = comments.clone().find_map(|v| {
        if v.starts_with("= \" * ret -") {
            let v = &v[12..v.len() - 1];
            Some(v.to_owned())
        } else {
            None
        }
    });
    let docs = comments.clone().find_map(|v| {
        if !v.starts_with("= \" *") {
            let v = &v[4..v.len() - 1];
            Some(v.to_owned())
        } else {
            None
        }
    });

    let docs_ret = vec![
        if ret.is_some() {
            ret.as_ref().unwrap()
        } else {
            ""
        },
        if docs.is_some() {
            docs.as_ref().unwrap()
        } else {
            ""
        },
    ]
    .join(" and ");
    // Return type convert back to variant
    let output = {
        match output {
            syn::ReturnType::Default => quote! {},
            syn::ReturnType::Type(_, path) => match &**path {
                syn::Type::Path(path) => {
                    let segment = &path.path.segments[0];
                    if segment.ident == "Result" {
                        let args = &segment.arguments;
                        match args {
                            syn::PathArguments::AngleBracketed(generic_args) => {
                                let arg0 = &generic_args.args[0];
                                match &*arg0 {
                                    syn::GenericArgument::Type(path) => match path {
                                        syn::Type::Tuple(tuple) => {
                                            let elems = &tuple.elems[0];
                                            match &*elems {
                                                syn::Type::Path(path) => {
                                                    let segment = &path.path.segments[0];
                                                    if segment.ident == "Vec" {
                                                        let args = &segment.arguments;
                                                        match &*args {
                                                            syn::PathArguments::AngleBracketed(generic_args) => {
                                                                let arg0 = &generic_args.args[0];
                                                                match &*arg0 {
                                                                    syn::GenericArgument::Type(path) => {
                                                                        match path {
                                                                            syn::Type::Path(p) => {
                                                                                if p.path.segments[0].ident == "String" {
                                                                                    quote! {Ok(Variant::from(&(res.0.iter().map(AsRef::as_ref).collect::<Vec<_>>().as_slice(),res.1)))}
                                                                                } else {
                                                                                    quote! {Ok(Variant::from(&(res.0.as_slice(),res.1)))}
                                                                                }
                                                                            },
                                                                            _ => panic!("Expected a type of f64,u32,bool,String")
                                                                        }
 
                                                                    },
                                                                    _ =>  panic!("Expected a simple type after a vec"),

                                                                }
                                                            },
                                                                syn::PathArguments::Parenthesized(_) => {
                                                                    quote! {Ok(Variant::from(true))}
                                                                },
                                                                syn::PathArguments::None => panic!("Unhandled type for result0"),
                                                        }
//                                                        quote! {Ok(Variant::from(&(res.0.as_slice(),res.1)))}
                                                    } else {
                                                        quote! {Ok(Variant::from(res))}
                                                    }        
                                                },
                                                _ => panic!("Tuple returned must of <Vec<f64>,Dimension(usize)>")

                                            }
                                        }
                                        syn::Type::Path(_) => {
                                            quote! {Ok(Variant::from(res))}
                                        },
                                        _ => panic!("XL functions must return a basic type of f64,i64,u32,i32,bool or a tuple of (Vec<f64>,Dimension(usize))")
                                    },
                                    _ => panic!("XL functions must return a basic type of f64,i64,u32,i32,bool or a tuple of (Vec<f64>,Dimension(usize))")
                                }
                            }
                            syn::PathArguments::Parenthesized(_) => {
                                quote! {Ok(Variant::from(true))}
                            }
                            syn::PathArguments::None => quote! {} //panic!("XL functions must return a basic type of f64,i64,u32,i32,bool or a tuple of (Vec<f64>,Dimension(usize))")

                        }
                    } else {
                        panic!("XL functions must return a Result<TYPE,Error>. Error can be coerced into a Box<std::error::Error>")
                    }
                }
                _ => panic!("Unhandled type"),
            },
        }
    };
    // Now collate
    let lpx_oper_args = typed_args
        .clone()
        .map(|(name, _)| quote!(#name: LPXLOPER12))
        .collect::<Vec<_>>();
    let variant_args = typed_args
        .clone()
        .map(|(name, _)| quote!(#name: xladd::variant::Variant))
        .collect::<Vec<_>>();
    let to_variant = typed_args
        .clone()
        .map(|(name, _)| quote!(let #name = xladd::variant::Variant::from(#name);))
        .collect::<Vec<_>>();
    let caller_args = typed_args
        .clone()
        .map(|(name, _)| quote!(#name))
        .collect::<Vec<_>>();
    let caller_args_str = typed_args
        .clone()
        .map(|(name, _)| name.to_string())
        .collect::<Vec<_>>()
        .join(",");
    let mut q_args = typed_args
        .clone()
        .map(|(_, _)| "Q")
        .collect::<Vec<_>>()
        .join("");
    // Mark function as async
    if async_function {
        q_args.insert(0,'>');
        q_args.push('X');
    } else {
        // Return type is a variant
        q_args.push('Q');
    }
    // not_thread_safe
    if !(single_threaded || async_function) { q_args.push('$'); };
    let convert_to_owned_rust_types = typed_args
        .clone()
        .map(|(_, owned_type)| owned_type)
        .collect::<Vec<_>>();

    let convert_to_ref_rust_types = reference_args
        .clone()
        .map(|(_, owned_type)| owned_type)
        .collect::<Vec<_>>();

        let xl_function_str = xl_function.to_string();
    // Async function
    if async_function {
        let wrapper = quote! {
             // Error handler
             fn #error_handler_function(#(#variant_args),*, return_handle: LPXLOPER12) -> Result<Variant, Box<dyn std::error::Error>> {
                log::trace!("{} called [*ASYNC*] ..waiting for results",stringify!(#xl_function));
                #(#convert_to_owned_rust_types)*;
                let raw_ptr = xladd::variant::XLOPERPtr(return_handle);
                std::thread::spawn(move ||{
                    #(#convert_to_ref_rust_types)*;
                    match #func(#(#caller_args),*) {
                        Ok(v) => {
                            log::trace!("Results [{:?}]",v);
                            xladd::entrypoint::excel12(
                                                xladd::xlcall::xlAsyncReturn,
                                                &mut [Variant::from(raw_ptr), v]);
                        }
                        Err(e) => {
                            log::error!("Error {:?}",e.to_string());
                            xladd::entrypoint::excel12(
                                                xladd::xlcall::xlAsyncReturn,
                                                &mut [Variant::from(raw_ptr), Variant::from(e.to_string())]);
                        }
                    }
                });
                Ok(Variant::default())
            }
            // Excel function
            #[no_mangle]
            pub extern "stdcall" fn #xl_function(#(#lpx_oper_args),* ,return_handle: LPXLOPER12) {
                #(#to_variant)*
                match #error_handler_function(#(#caller_args),*, return_handle) {
                    Ok(_) => (),
                    Err(e) => {
                        log::error!("{}",e.to_string());
                        let raw_ptr = xladd::variant::XLOPERPtr(return_handle);
                        xladd::entrypoint::excel12(
                            xladd::xlcall::xlAsyncReturn,
                            &mut [xladd::variant::Variant::from(raw_ptr), xladd::variant::Variant::from(e.to_string())],
                        );
                    },
                }
            }

            pub (crate) fn #register_function(reg: &xladd::registrator::Reg) {
                reg.add(#xl_function_str,#q_args,#caller_args_str,#category,#docs_ret,&[#(#args),*]);
            }
            // User function
            #item
        };
        wrapper.into()
    } else {
        let wrapper = quote! {
            // Error handler
            fn #error_handler_function(#(#variant_args),*) -> Result<xladd::variant::Variant, Box<dyn std::error::Error>> {
                log::trace!("{} called",stringify!(#xl_function));
                #(#convert_to_owned_rust_types)*;
                #(#convert_to_ref_rust_types)*;
                let res = std::panic::catch_unwind(|| #func(#(#caller_args),*));
                match res {
                    Ok(result) => {
                        let res = result?;
                        log::trace!("Results [{:?}]",res);
                        #output        
                    }
                    Err(_) => {
                        log::error!("Unexpected error while calling function"); 
                        Err("Error when trying to execute function, check for invalid values, ranges, or #n/a".into())
                    }
                }
            }
            // Excel function
            #[no_mangle]
            pub extern "stdcall" fn #xl_function(#(#lpx_oper_args),*)  -> LPXLOPER12 {
                #(#to_variant)*
                match #error_handler_function(#(#caller_args),*) {
                    Ok(v) => LPXLOPER12::from(v),
                    Err(e) => {
                        log::error!("{}",e.to_string());
                        LPXLOPER12::from(xladd::variant::Variant::from(e.to_string().as_str()))
                    },
                }
            }

            pub (crate) fn #register_function(reg: &xladd::registrator::Reg) {
                reg.add(#xl_function_str,#q_args,#caller_args_str,#category,#docs_ret,&[#(#args),*]);
            }
            // User function
            #item
        };
    wrapper.into()
    }   
}