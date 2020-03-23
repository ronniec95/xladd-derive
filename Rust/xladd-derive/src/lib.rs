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

    // Create the excel method
    let xl_ident = proc_macro2::Ident::new(
        &format!("xl_{}", item.sig.ident),
        proc_macro2::Span::call_site(),
    );

    let error_handler_ident = proc_macro2::Ident::new(
        &format!("_{}", item.sig.ident),
        proc_macro2::Span::call_site(),
    );

    let wrapper_inputs = &item
        .sig
        .inputs
        .iter()
        .map(|arg| {
            let mut new_arg = arg.clone();
            match &mut new_arg {
                FnArg::Typed(x) => {
                    x.ty = Box::new(syn::Type::Verbatim(quote!(LPXLOPER12)));
                    quote! {#new_arg}
                }
                FnArg::Receiver(x) => quote! {#arg},
            }
        })
        .collect::<Vec<_>>();

    let variant_conversion = &item
        .sig
        .inputs
        .iter()
        .map(|arg| match &arg {
            FnArg::Typed(pat) => {
                let pat = &pat.pat;
                let arg = match &**pat {
                    syn::Pat::Ident(ident) => ident.ident.clone(),
                    _ => proc_macro2::Ident::new("unknown", proc_macro2::Span::call_site()),
                };
                quote! { let #arg = Variant::from(#arg); }
            }
            FnArg::Receiver(x) => quote! {#arg},
        })
        .collect::<Vec<_>>();

    let arg_list = &item
        .sig
        .inputs
        .iter()
        .map(|arg| match &arg {
            FnArg::Typed(pat) => {
                let pat = &pat.pat;
                let arg = match &**pat {
                    syn::Pat::Ident(ident) => ident.ident.clone(),
                    _ => proc_macro2::Ident::new("unknown", proc_macro2::Span::call_site()),
                };
                quote! { #arg }
            }
            FnArg::Receiver(x) => quote! {#arg},
        })
        .collect::<Vec<_>>();
    let wrapper = quote! {
        pub extern "stdcall" fn #xl_ident(#(#wrapper_inputs),*)  -> LPXLOPER12 {
            #(#variant_conversion)*
            match #error_handler_ident(#(#arg_list),*) {
                Ok(v) -> LPXLOPER12::from(v),
                Err(e) => LPXLOPER12::from(Variant::from(e.into())),
            }
        }

        #item
    };
    // let tokens = quote! {
    //     pub extern "stdcall" fn #xl_ident(#inputs) #output #stmts
    // };
    println!("{}", wrapper.to_string());

    wrapper.into()
}
