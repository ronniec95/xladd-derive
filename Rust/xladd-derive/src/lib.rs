use proc_macro::*;
use quote::quote;
use quote::ToTokens;
use rust_xl::xlcall::*;
use syn::{FnArg, ItemFn, TypePath, TypeSlice};

#[proc_macro_attribute]
pub fn xl_func(attr: TokenStream, input: TokenStream) -> TokenStream {
    // println!("{:?}", attr);
    // println!("{:?}", input);
    // println!("{:?}", input);

    let item = syn::parse::<ItemFn>(input).expect("Failed to parse.");

    // Use `quote` to convert the syntax tree back into tokens so we can return them. Note
    // that the tokens we're returning at this point are still just the input, we've simply
    // converted it between a few different forms.
    let output = &item.sig.output;
    let func = &item.sig.ident;
    let stmts = &item.block;

    let xl_function = proc_macro2::Ident::new(
        &format!("xl_{}", item.sig.ident),
        proc_macro2::Span::call_site(),
    );
    let error_handler_function = proc_macro2::Ident::new(
        &format!("_error_hndlr_{}", item.sig.ident),
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
                        let ident = if ident == "str" {
                            quote!(String)
                        } else {
                            quote!(#ident)
                        };
                        quote!( let #arg_name = TryInto::<#ident>::try_into(&#arg_name)?; )
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
                                        if ident == "str" {
                                            quote!(let #arg_name = TryInto::<Vec<String>>::try_into(&#arg_name)?.iter().map(AsRef::as_ref).collect();)
                                        } else {
                                            quote!(let #arg_name = TryInto::<Vec<#ident>>::try_into(&#arg_name)?;
                                                   let #arg_name = #arg_name.as_slice();
                                            )
                                        }
                                    }
                                    _ => panic!("Type not covered"),
                                }
                            }
                            syn::Type::Path(s) => {
                                let segment = &s.path.segments[0];
                                let ident = &segment.ident;
                                if ident == "str" { 
                                    quote!(let #arg_name = TryInto::<String>::try_into(&#arg_name)?;
                                        let #arg_name = #arg_name.as_str();)
                                } else { 
                                    quote!(let #arg_name = TryInto::<#ident>::try_into(&#arg_name)?;)
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

    // Return type convert back to variant
    dbg!(output);
    
    // Now collate
    let lpx_oper_args = typed_args
        .clone()
        .map(|(name, _)| quote!(#name: LPXLOPER12))
        .collect::<Vec<_>>();
    let variant_args = typed_args
        .clone()
        .map(|(name, _)| quote!(#name: Variant))
        .collect::<Vec<_>>();
    let to_variant = typed_args
        .clone()
        .map(|(name, _)| quote!(let #name = Variant::from(#name);))
        .collect::<Vec<_>>();
    let caller_args = typed_args
        .clone()
        .map(|(name, _)| quote!(#name))
        .collect::<Vec<_>>();
    let convert_to_owned_rust_types = typed_args
        .clone()
        .map(|(_, owned_type)| owned_type)
        .collect::<Vec<_>>();
    let wrapper = quote! {
        use std::convert::TryInto;
        // Error handler
        fn #error_handler_function(#(#variant_args),*) -> Result<Variant, Box<dyn std::error::Error>> {
            #(#convert_to_owned_rust_types)*;
            let res = #func(#(#caller_args),*)?;
            Ok(Variant::from(res))
        }
        // Excel function
        pub extern "stdcall" fn #xl_function(#(#lpx_oper_args),*)  -> LPXLOPER12 {
            #(#to_variant)*
            match #error_handler_function(#(#caller_args),*) {
                Ok(v) => LPXLOPER12::from(v),
                Err(e) => LPXLOPER12::from(Variant::from(e.to_string().as_str())),
            }
        }

        // User function
        #item
    };
    // let tokens = quote! {
    //     pub extern "stdcall" fn #xl_ident(#inputs) #output #stmts
    // };
    println!("{}", wrapper.to_string());

    wrapper.into()
}
