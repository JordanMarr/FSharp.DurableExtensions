module FSharp.DurableExtensions

open System.Threading.Tasks
open Microsoft.FSharp.Quotations
open Microsoft.Azure.WebJobs.Extensions.DurableTask

/// Tries to get the custom FunctionName attribute name, else uses the method name.
let private getFunctionName (expr: Expr<'a>) = 
    match expr with
    | DerivedPatterns.Lambdas(_, Patterns.Call(_,mi,_)) ->         
        mi.CustomAttributes |> Seq.tryFind (fun a -> a.AttributeType.Name = "FunctionNameAttribute")
        |> Option.bind (fun att -> Seq.tryHead att.ConstructorArguments)
        |> Option.map (fun nameArg -> string nameArg.Value)
        |> Option.defaultValue mi.Name        
    | _ -> 
        failwith "Invalid function: unable to get function name."

/// Calls either CallActivityAsync or CallActivityWithRetryAsync depending on whether a RetryOptions is provided.
let private callActivity<'Output> (ctx: IDurableOrchestrationContext) (functionName: string) (retry: RetryOptions option) (req: obj) = 
    match retry with
        | Some opt -> 
            ctx.CallActivityWithRetryAsync<'Output>(functionName, opt, req)
        | None -> 
            ctx.CallActivityAsync<'Output>(functionName, req)

/// Extension methods for calling Azure Durable Activity Functions with strongly typed inputs and outputs.
type IDurableOrchestrationContext with 
    
    /// Calls a function with no ActivityTrigger input.
    member ctx.CallActivity<'D1, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'D1 -> Task<'Output>>, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry null

    /// Calls a function with an ActivityTrigger input parameter and no dependency parameters.
    member ctx.CallActivity<'Input, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input -> Task<'Output>>, req: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry req        

    /// Calls a function with an ActivityTrigger input parameter and one dependency parameter.
    member ctx.CallActivity<'Input, 'D1, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 -> Task<'Output>>, req: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry req
        
    /// Calls a function with an ActivityTrigger input parameter and two dependency parameters.
    member ctx.CallActivity<'Input, 'D1, 'D2, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 * 'D2 -> Task<'Output>>, req: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry req

    /// Calls a function with an ActivityTrigger input parameter and three dependency parameters.
    member ctx.CallActivity<'Input, 'D1, 'D2, 'D3, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 * 'D2 * 'D3 -> Task<'Output>>, req: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry req
