module FSharp.DurableExtensions

open System.Threading.Tasks
open Microsoft.FSharp.Quotations
open Microsoft.DurableTask
open Microsoft.DurableTask.Client

[<AutoOpen>]
module private Helpers =
    /// Tries to get the custom Function attribute name, else uses the method name.
    let getFunctionName (expr: Expr<'a>) =
        match expr with
        | DerivedPatterns.Lambdas(_, Patterns.Call(_,mi,_)) ->
            mi.CustomAttributes |> Seq.tryFind (fun a -> a.AttributeType.Name = "FunctionAttribute")
            |> Option.bind (fun att -> Seq.tryHead att.ConstructorArguments)
            |> Option.map (fun nameArg -> string nameArg.Value)
            |> Option.defaultValue mi.Name
        | _ ->
            failwith "Invalid function: unable to get function name."

    /// Converts an optional RetryPolicy to TaskOptions.
    let toTaskOptions (retry: RetryPolicy option) =
        match retry with
        | Some policy -> TaskOptions.FromRetryPolicy(policy)
        | None -> Unchecked.defaultof<TaskOptions>

    /// Converts an optional RetryPolicy and instanceId to SubOrchestrationOptions.
    let toSubOrchestrationOptions (retry: RetryPolicy option) (instanceId: string option) =
        let baseOptions = toTaskOptions retry
        match instanceId with
        | Some id when not (isNull baseOptions) -> baseOptions.WithInstanceId(id)
        | Some id -> TaskOptions().WithInstanceId(id)
        | None when not (isNull baseOptions) -> SubOrchestrationOptions(baseOptions)
        | None -> Unchecked.defaultof<SubOrchestrationOptions>

/// Extension methods for calling ActivityTrigger functions with strongly typed inputs and outputs.
type TaskOrchestrationContext with

    /// Calls a function with no ActivityTrigger input.
    member ctx.CallActivity<'Args, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<'Output>>, ?retry: RetryPolicy) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toTaskOptions retry
        ctx.CallActivityAsync<'Output>(functionName, null, options)

    /// Calls a function with an ActivityTrigger input parameter and no dependency parameters.
    member ctx.CallActivity<'Input, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input -> Task<'Output>>, input: 'Input, ?retry: RetryPolicy) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toTaskOptions retry
        ctx.CallActivityAsync<'Output>(functionName, input, options)

    /// Calls a function with an ActivityTrigger input parameter and one dependency parameter.
    member ctx.CallActivity<'Input, 'D1, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 -> Task<'Output>>, input: 'Input, ?retry: RetryPolicy) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toTaskOptions retry
        ctx.CallActivityAsync<'Output>(functionName, input, options)

    /// Calls a function with an ActivityTrigger input parameter and two dependency parameters.
    member ctx.CallActivity<'Input, 'D1, 'D2, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 * 'D2 -> Task<'Output>>, input: 'Input, ?retry: RetryPolicy) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toTaskOptions retry
        ctx.CallActivityAsync<'Output>(functionName, input, options)

    /// Calls a function with an ActivityTrigger input parameter and three dependency parameters.
    member ctx.CallActivity<'Input, 'D1, 'D2, 'D3, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Input * 'D1 * 'D2 * 'D3 -> Task<'Output>>, input: 'Input, ?retry: RetryPolicy) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toTaskOptions retry
        ctx.CallActivityAsync<'Output>(functionName, input, options)

    /// Calls a sub-orchestrator function with no input.
    member ctx.CallSubOrchestrator<'Args, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<'Output>>, ?retry: RetryPolicy, ?instanceId: string) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toSubOrchestrationOptions retry instanceId
        ctx.CallSubOrchestratorAsync<'Output>(functionName, null, options)

    /// Calls a sub-orchestrator function with an input parameter.
    member ctx.CallSubOrchestrator<'Args, 'Output> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<'Output>>, input: obj, ?retry: RetryPolicy, ?instanceId: string) : Task<'Output> =
        let functionName = getFunctionName azureFn
        let options = toSubOrchestrationOptions retry instanceId
        ctx.CallSubOrchestratorAsync<'Output>(functionName, input, options)

/// Extension methods for starting OrchestrationTrigger functions with strongly typed inputs.
type DurableTaskClient with

    /// Starts a function with no input.
    member client.StartNew<'Args> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<unit>>) : Task<string> =
        let functionName = getFunctionName azureFn
        client.ScheduleNewOrchestrationInstanceAsync(functionName, cancellation = System.Threading.CancellationToken.None)

    /// Starts a function with an input parameter.
    member client.StartNew<'Args, 'Input when 'Input : not struct> ([<ReflectedDefinition>] azureFn: Expr<'Args -> Task<unit>>, input: 'Input) : Task<string> =
        let functionName = getFunctionName azureFn
        client.ScheduleNewOrchestrationInstanceAsync(functionName, input :> obj, System.Threading.CancellationToken.None)
