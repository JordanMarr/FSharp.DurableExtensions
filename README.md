## FSharp.DurableExtensions
[![NuGet version (FSharp.DurableExtensions)](https://img.shields.io/nuget/v/FSharp.DurableExtensions.svg?style=flat-square)](https://www.nuget.org/packages/FSharp.DurableExtensions/)

This library adds the `CallActivity` extension method to the `IDurableOrchestrationContext` for strongly typed activity calls.

```F#
let! addResp = context.CallActivity(this.AddFive, { NumberToAdd = 2 })
let! mltResp = context.CallActivity(this.MultiplyByTwo, { NumberToMultiply = addResp.Sum })
logger.LogInformation $"Result: {mltResp.Product}"
```

## What problem does this library solve?
Calling activity functions from a durable function "orchestrator" normally involves calling the function by passing its name as a string, its input as an `obj`, and  then manually specifying the expected output type using a generic argument. This approach can lead to runtime errors.

### Normal Usage Example: (Magic Strings + Manually Entered Generic Arguments)

```F#
type AddFiveRequest = { NumberToAdd: int } 
type AddFiveResponse = { Sum: int }
type MultiplyByTwoRequest = { NumberToMultiply: int }
type MultiplyByTwoResponse = { Product: int }

type Fns() = 
    [<FunctionName "chaining-orchestrator">]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: IDurableOrchestrationContext, logger: ILogger) = 
        task {
            let! addResp = context.CallActivityAsync<AddFiveResponse>("add-five", { NumberToAdd = 2 })
            let! mltResp = context.CallActivityAsync<MultiplyByTwoResponse>("multiply-by-two", { NumberToMultiply = addResp.Sum })
            logger.LogInformation $"Result: {mltResp.Product}"            
        }
    
    [<FunctionName "add-five">]
    member this.AddFive([<ActivityTrigger>] req: AddFiveRequest, logger: ILogger) : Task<AddFiveResponse> = 
        task {
            logger.LogInformation $"Adding 5 to {req.NumberToAdd}"
            return { Sum = req.NumberToAdd + 5 }
        }

    [<FunctionName "multiply-by-two">]
    member this.MultiplyByTwo([<ActivityTrigger>] req: MultiplyByTwoRequest, logger: ILogger) : Task<MultiplyByTwoResponse> = 
        task {
            logger.LogInformation $"Multiplying {req.NumberToMultiply} by 2"
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
This library addresses all the above problems with the new `CallActivity` extension methods that are added to the `IDurableOrchestrationContext`.
`CallActivity` allows you to directly pass the function you are calling, and infers both the input and output types for you. This completely eliminates runtime errors by utilizing the compiler at design-time, and also makes it easy to navigate directly to the referenced function via "F12" / "Go to definition".

```F#
open FSharp.DurableExtensions

type AddFiveRequest = { NumberToAdd: int } 
type AddFiveResponse = { Sum: int }
type MultipleByTwoRequest = { NumberToMultiply: int }
type MultipleByTwoResponse = { Product: int }

type Fns() =
    [<FunctionName "chaining-orchestrator">]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: IDurableOrchestrationContext, logger: ILogger) = 
        task {
            let! addResp = context.CallActivity(this.AddFive, { NumberToAdd = 2 })
            let! mltResp = context.CallActivity(this.MultiplyByTwo, { NumberToMultiply = addResp.Sum })
            logger.LogInformation $"Result: {mltResp.Product}"
        }
    
    [<FunctionName "add-five">]
    member this.AddFive([<ActivityTrigger>] req: AddFiveRequest, logger: ILogger) : Task<AddFiveResponse> = 
        task {
            logger.LogInformation $"Adding 5 to {req.NumberToAdd}"
            return { Sum = req.NumberToAdd + 5 }
        }

    [<FunctionName "multiply-by-two">]
    member this.MultiplyByTwo([<ActivityTrigger>] req: MultiplyByTwoRequest, logger: ILogger) : Task<MultiplyByTwoResponse> = 
        task {
            logger.LogInformation $"Multiplying {req.NumberToMultiply} by 2"
            return { Product = req.NumberToMultiply * 2 }
        }
```

## Retry Options
`RetryOptions` may optionally be passed in:

```F#
type Fns() = 
    [<FunctionName "chaining-orchestrator">]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: IDurableOrchestrationContext, logger: ILogger) = 
        task {
            let retry = RetryOptions(TimeSpan.FromSeconds 5, 3)
            let! addResp = context.CallActivity(this.AddFive, { NumberToAdd = 2 }, retry)
            let! mltResp = context.CallActivity(this.MultiplyByTwo, { NumberToMultiply = addResp.Sum }, retry)
            logger.LogInformation $"Result: {mltResp.Product}"
        }
```

