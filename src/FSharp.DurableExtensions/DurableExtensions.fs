module FSharp.DurableExtensions

open System.Threading.Tasks
open Microsoft.FSharp.Quotations
open Microsoft.Azure.WebJobs.Extensions.DurableTask

[<AutoOpen>]
module private Helpers = 
    /// Tries to get the custom FunctionName attribute name, else uses the method name.
    let getFunctionName (expr: Expr<'a>) = 
        match expr with
        | DerivedPatterns.Lambdas(_, Patterns.Call(_,mi,_)) ->         
            mi.CustomAttributes |> Seq.tryFind (fun a -> a.AttributeType.Name = "FunctionNameAttribute")
            |> Option.bind (fun att -> Seq.tryHead att.ConstructorArguments)
            |> Option.map (fun nameArg -> string nameArg.Value)
            |> Option.defaultValue mi.Name        
        | _ -> 
            failwith "Invalid function: unable to get function name."

    /// Calls either CallActivityAsync or CallActivityWithRetryAsync depending on whether a RetryOptions is provided.
    let callActivity<'Output> (ctx: IDurableOrchestrationContext) (functionName: string) (retry: RetryOptions option) (input: obj) = 
        match retry with
            | Some retry -> 
                ctx.CallActivityWithRetryAsync<'Output>(functionName, retry, input)
            | None -> 
                ctx.CallActivityAsync<'Output>(functionName, input)

    /// Calls either CallSubOrchestratorAsync or CallSubOrchestratorWithRetryAsync depending on whether a RetryOptions is provided.
    let callSubOrchestrator<'Output> (ctx: IDurableOrchestrationContext) (functionName: string) (retry: RetryOptions option) (instanceId: string option) (input: obj) = 
        match retry, instanceId with
            | Some retry, Some instanceId -> 
                ctx.CallSubOrchestratorWithRetryAsync<'Output>(functionName, retry, instanceId, input)
            | Some retry, None ->
                ctx.CallSubOrchestratorWithRetryAsync<'Output>(functionName, retry, input)
            | None, Some instanceId ->
                ctx.CallSubOrchestratorAsync<'Output>(functionName, instanceId, input)
            | None, None -> 
                ctx.CallSubOrchestratorAsync<'Output>(functionName, input)

/// Extension methods for calling ActivityTrigger functions with strongly typed inputs and outputs.
type IDurableOrchestrationContext with 
    
    /// Calls a function with no ActivityTrigger input.
    member ctx.CallActivity<'Args, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<'Output>>, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry null

    /// Calls a function with an ActivityTrigger input parameter and no dependency parameters.
    member ctx.CallActivity<'Input, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input -> Task<'Output>>, input: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry input        

    /// Calls a function with an ActivityTrigger input parameter and one dependency parameter.
    member ctx.CallActivity<'Input, 'D1, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 -> Task<'Output>>, input: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry input
        
    /// Calls a function with an ActivityTrigger input parameter and two dependency parameters.
    member ctx.CallActivity<'Input, 'D1, 'D2, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 * 'D2 -> Task<'Output>>, input: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry input

    /// Calls a function with an ActivityTrigger input parameter and three dependency parameters.
    member ctx.CallActivity<'Input, 'D1, 'D2, 'D3, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 * 'D2 * 'D3 -> Task<'Output>>, input: 'Input, ?retry: RetryOptions) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callActivity<'Output> ctx functionName retry input

    /// Calls a sub-orchestrator function with no input.
    member ctx.CallSubOrchestrator<'Args, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<'Output>>, ?retry: RetryOptions, ?instanceId: string) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callSubOrchestrator<'Output> ctx functionName retry instanceId null

    /// Calls a sub-orchestrator function with an input parameter.
    member ctx.CallSubOrchestrator<'Args, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<'Output>>, input: obj, ?retry: RetryOptions, ?instanceId: string) : Task<'Output> = 
        let functionName = getFunctionName azureFn
        callSubOrchestrator<'Output> ctx functionName retry instanceId input

/// Extension methods for calling OrchestrationTrigger functions with strongly typed inputs.
type IDurableOrchestrationClient with 

    /// Calls a function with no input.
    member client.StartNew<'Args> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<unit>>) : Task<string> = 
        let functionName = getFunctionName azureFn
        client.StartNewAsync(functionName, null)
    
    /// Calls a function with an input parameter.
    member client.StartNew<'Args, 'Input when 'Input : not struct> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<unit>>, input: 'Input) : Task<string> = 
        let functionName = getFunctionName azureFn
        client.StartNewAsync<'Input>(functionName, input)
