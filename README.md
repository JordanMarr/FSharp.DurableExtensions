## FSharp.DurableExtensions
[![NuGet version (FSharp.DurableExtensions)](https://img.shields.io/nuget/v/FSharp.DurableExtensions.svg?style=flat-square)](https://www.nuget.org/packages/FSharp.DurableExtensions/)

F# extensions for Azure Durable Functions that provide strongly typed orchestration and activity calls.

> **v2.0** - This version targets the **isolated worker model** (.NET 8+) using `Microsoft.Azure.Functions.Worker.Extensions.DurableTask`. For the legacy in-process model, use v1.x.

This library adds the following extension methods:
* `DurableTaskClient` - `StartNew` for starting an orchestrator function.
* `TaskOrchestrationContext` - `CallActivity` for calling an activity function.
* `TaskOrchestrationContext` - `CallSubOrchestrator` for calling a sub-orchestrator function.

```F#
[<Function "start">]
member this.Start(
    [<HttpTrigger(AuthorizationLevel.Function, "post")>] req: HttpRequestData,
    [<DurableClient>] client: DurableTaskClient) =
    task {
        let! instanceId = client.StartNew(this.Orchestrator)
        return client.CreateCheckStatusResponse(req, instanceId)
    }

[<Function "orchestrator">]
member this.Orchestrator ([<OrchestrationTrigger>] context: TaskOrchestrationContext) =
    task {
        let! addResp = context.CallActivity(this.AddFive, { NumberToAdd = 2 })
        let! mltResp = context.CallActivity(this.MultiplyByTwo, { NumberToMultiply = addResp.Sum })
        return mltResp.Product
    }
```

## What problem does this library solve?
Calling activity functions from a durable orchestrator normally involves calling the function by passing its name as a string, its input as an `obj`, and then manually specifying the expected output type using a generic argument. This approach can lead to runtime errors.

### Normal Usage Example: (Magic Strings + Manually Entered Generic Arguments)

```F#
type AddFiveRequest = { NumberToAdd: int }
type AddFiveResponse = { Sum: int }
type MultiplyByTwoRequest = { NumberToMultiply: int }
type MultiplyByTwoResponse = { Product: int }

type Fns() =
    [<Function "chaining-orchestrator">]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: TaskOrchestrationContext) =
        task {
            let! addResp = ctx.CallActivityAsync<AddFiveResponse>("add-five", { NumberToAdd = 2 })
            let! mltResp = ctx.CallActivityAsync<MultiplyByTwoResponse>("multiply-by-two", { NumberToMultiply = addResp.Sum })
            return mltResp.Product
        }

    [<Function "add-five">]
    member this.AddFive([<ActivityTrigger>] req: AddFiveRequest) : Task<AddFiveResponse> =
        task {
            return { Sum = req.NumberToAdd + 5 }
        }

    [<Function "multiply-by-two">]
    member this.MultiplyByTwo([<ActivityTrigger>] req: MultiplyByTwoRequest) : Task<MultiplyByTwoResponse> =
        task {
            return { Product = req.NumberToMultiply * 2 }
        }
```

### Problems with this approach:
* Using magic strings to call functions provides no compile-time safety and can easily result in runtime errors if the strings are incorrect.
* Not refactor-proof: changing the function name may break the orchestration.
* Specifying the wrong input or output generic arguments can result in runtime errors.
* Hard to navigate: using string identifiers makes it difficult to navigate to the target function because you cannot take advantage of IDE features like "Go to definition".
* Bloated code: It is common to create constants to hold function names which bloats the code and still doesn't solve the problems listed above.

## The Solution: FSharp.DurableExtensions
This library addresses all the above problems with the new `CallActivity` extension methods that are added to `TaskOrchestrationContext`.
`CallActivity` allows you to directly pass the function you are calling, and infers both the input and output types for you. This completely eliminates runtime errors by utilizing the compiler at design-time, and also makes it easy to navigate directly to the referenced function via "F12" / "Go to definition".

```F#
open FSharp.DurableExtensions

type AddFiveRequest = { NumberToAdd: int }
type AddFiveResponse = { Sum: int }
type MultiplyByTwoRequest = { NumberToMultiply: int }
type MultiplyByTwoResponse = { Product: int }

type Fns() =
    [<Function "chaining-orchestrator">]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: TaskOrchestrationContext) =
        task {
            let! addResp = ctx.CallActivity(this.AddFive, { NumberToAdd = 2 })
            let! mltResp = ctx.CallActivity(this.MultiplyByTwo, { NumberToMultiply = addResp.Sum })
            return mltResp.Product
        }

    [<Function "add-five">]
    member this.AddFive([<ActivityTrigger>] req: AddFiveRequest) : Task<AddFiveResponse> =
        task {
            return { Sum = req.NumberToAdd + 5 }
        }

    [<Function "multiply-by-two">]
    member this.MultiplyByTwo([<ActivityTrigger>] req: MultiplyByTwoRequest) : Task<MultiplyByTwoResponse> =
        task {
            return { Product = req.NumberToMultiply * 2 }
        }
```

## Retry Policy
A `RetryPolicy` may optionally be passed in to configure automatic retries:

```F#
open Microsoft.DurableTask

type Fns() =
    [<Function "chaining-orchestrator">]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: TaskOrchestrationContext) =
        task {
            let retry = RetryPolicy(maxNumberOfAttempts = 3, firstRetryInterval = TimeSpan.FromSeconds(5))
            let! addResp = ctx.CallActivity(this.AddFive, { NumberToAdd = 2 }, retry)
            let! mltResp = ctx.CallActivity(this.MultiplyByTwo, { NumberToMultiply = addResp.Sum }, retry)
            return mltResp.Product
        }
```

## Starting Orchestrations
Use the `StartNew` extension method on `DurableTaskClient` to start an orchestration:

```F#
type Fns() =
    [<Function "start">]
    member this.Start(
        [<HttpTrigger(AuthorizationLevel.Function, "post")>] req: HttpRequestData,
        [<DurableClient>] client: DurableTaskClient) =
        task {
            // Start with no input
            let! instanceId = client.StartNew(this.Orchestrator)
            return client.CreateCheckStatusResponse(req, instanceId)
        }

    [<Function "start-with-input">]
    member this.StartWithInput(
        [<HttpTrigger(AuthorizationLevel.Function, "post")>] req: HttpRequestData,
        [<DurableClient>] client: DurableTaskClient) =
        task {
            // Start with input
            let! instanceId = client.StartNew(this.OrchestratorWithInput, { NumberToAdd = 10 })
            return client.CreateCheckStatusResponse(req, instanceId)
        }
```

## Calling Sub-Orchestrators
Use the `CallSubOrchestrator` extension method to call sub-orchestrations:

```F#
type Fns() =
    [<Function "main-orchestrator">]
    member this.MainOrchestrator ([<OrchestrationTrigger>] ctx: TaskOrchestrationContext) =
        task {
            // Call sub-orchestrator
            let! result = ctx.CallSubOrchestrator(this.SubOrchestrator, { Input = "data" })

            // Call with retry policy
            let retry = RetryPolicy(maxNumberOfAttempts = 3, firstRetryInterval = TimeSpan.FromSeconds(5))
            let! result2 = ctx.CallSubOrchestrator(this.SubOrchestrator, { Input = "data" }, retry)

            // Call with specific instance ID
            let! result3 = ctx.CallSubOrchestrator(this.SubOrchestrator, { Input = "data" }, instanceId = "my-instance-id")

            return result
        }
```

## Migration from v1.x (In-Process Model)

If you're migrating from v1.x (in-process model), here are the key changes:

| v1.x (In-Process) | v2.x (Isolated Worker) |
|-------------------|------------------------|
| `IDurableOrchestrationContext` | `TaskOrchestrationContext` |
| `IDurableOrchestrationClient` | `DurableTaskClient` |
| `[<FunctionName>]` | `[<Function>]` |
| `RetryOptions` | `RetryPolicy` |
| `.NET 6` | `.NET 8+` |

The extension method names (`CallActivity`, `CallSubOrchestrator`, `StartNew`) remain the same, making migration straightforward.
