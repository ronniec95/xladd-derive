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

    let xl_function = quote!(&format!("xl_{}", item.sig.ident));
    let error_handler_function = &format!("_error_hndlr_{}", item.sig.ident);

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
            // Arg type
            let arg_type = {
                let ty = &typed_arg.ty;
                match &**ty {
                    syn::Type::Path(p) => {
                        let segment = &p.path.segments[0];
                        let ident = &segment.ident;
                        quote!(&[#ident])
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
                                        quote!(&[#ident])
                                    }
                                    _ => panic!("Type not covered"),
                                }
                            }
                            syn::Type::Path(s) => {
                                let segment = &s.path.segments[0];
                                let ident = &segment.ident.to_string();
                                quote!(&[#ident])
                            }
                            _ => panic!("Type not covered"),
                        }

                        // or Path
                    }
                    _ => panic!("Type not covered"),
                }
            };
            // Owned type
            let owned_type = {
                let ty = &typed_arg.ty;
                match &**ty {
                    syn::Type::Path(p) => {
                        let segment = &p.path.segments[0];
                        let ident = &segment.ident.to_string();
                        let ident = if ident == "str" { "String" } else { ident };
                        quote!(&[#ident])
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
                                        let ident = if ident == "str" {
                                            String::from("Vec<String>")
                                        } else {
                                            format!("Vec<{}>", ident)
                                        };
                                        quote!(&[#ident])
                                    }
                                    _ => panic!("Type not covered"),
                                }
                            }
                            syn::Type::Path(s) => {
                                let segment = &s.path.segments[0];
                                let ident = &segment.ident.to_string();
                                let ident = if ident == "str" { "String" } else { ident };
                                quote!(&[#ident])
                            }
                            _ => panic!("Type not covered"),
                        }

                        // or Path
                    }
                    _ => panic!("Type not covered"),
                }
            };
            // Slice converted for &str

            // Slice converted for &[&str],&[f64]
            (arg_name, arg_type, owned_type)
        }
        FnArg::Receiver(_) => panic!("Free functions only"),
    });

    let lpx_oper = typed_args
        .clone()
        .map(|(name, _, _)| quote!(#name: LPXOPER12))
        .collect::<Vec<_>>();
    let to_variant = typed_args
        .clone()
        .map(|(name, _, _)| quote!(let #name = Variant::from(#name);))
        .collect::<Vec<_>>();
    let wrapper = quote! {
        // Error handler
        fn #error_handler_function() -> Result<Variant, Box<dyn std::error::Error>> {
            Ok(Variant::missing())
        }
        // Excel function
        pub extern "stdcall" fn #xl_function(#(#lpx_oper),*)  -> LPXLOPER12 {
            #(#to_variant)*

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
