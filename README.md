## FSharp.DurableExtensions
This library adds the `CallActivity` extension method to the `IDurableOrchestrationContext` for strongly typed activity calls.

```F#
let! getOneResult = ctx.CallActivity(this.GetOne)            
let! addFiveResult = ctx.CallActivity(this.AddFive, getOneResult)
```

## What problem does this library solve?
Calling activity functions from a durable function "orchestrator" normally involves calling the function by passing its name as a string, along with manually specifying the expected input and output types using generic arguments. This approach can lead to runtime errors.

### Normal Usage Example: (Magic Strings + Manually Entered Generic Arguments)

```F#
    [<FunctionName (name = "Orchestrator")>]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: IDurableOrchestrationContext, logger: ILogger) = 
        task {
            let! getOneResult = ctx.CallActivityAsync<int>("GetOne", null) // getOneResult = 1
            let! addFiveResult = ctx.CallActivityAsync<int>("AddFive", getOneResult) // addFiveResult = 6
            logger.LogInformation $"Result: {addFiveResult}"
        }

    [<FunctionName (name = "GetOne")>]
    member this.GetOne(logger: ILogger) : Task<int> = 
        task {
            logger.LogInformation "Returning 1"
            return 1
        }

    [<FunctionName (name = "AddFive")>]
    member this.AddFive(n: int, logger: ILogger) : Task<int> = 
        task {
            logger.LogInformation $"Adding 5 to {n}"
            return n + 5
        }
```

### Problems with this approach:
* Using magic strings to call functions provides no compile-time safety and can easily result in runtime errors if the strings are incorrect.
* Not refactor-proof: changing the function name may break the orchestration.
* Specifying the wrong input or output generic arguments can result in runtime errors.
* Hard to navigate: using string identifiers makes it difficult to navigate to the target function because you cannot take advantage of IDE features like "Go to definition".
* Bloated code: It is common to create constants to hold function names which bloats the code and still doesn't solve the problems listed above.

## The Solution (FSharp.DurableExtensions)
This library addresses the above problems by adding a series of `CallActivity` extension methods to the `IDurableOrchestrationContext`.
`CallActivity` makes the assumption that your activity method name will match the function name, which allows you to directly reference the function you are calling, and infers both the input and output types for you. This eliminates runtime errors, and also makes it easy to navigate directly to the referenced function via "F12" / "Go to definition".

```F#
    [<FunctionName (name = "Orchestrator")>]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: IDurableOrchestrationContext, logger: ILogger) = 
        task {
            let! getOneResult = ctx.CallActivity(this.GetOne)            
            let! addFiveResult = ctx.CallActivity(this.AddFive, getOneResult)
            logger.LogInformation $"Result: {addFiveResult}"
        }

    [<FunctionName (name = "GetOne")>]
    member this.GetOne(logger: ILogger) : Task<int> = 
        task {
            logger.LogInformation "Returning 1"
            return 1
        }

    [<FunctionName (name = "AddFive")>]
    member this.AddFive(n: int, logger: ILogger) : Task<int> = 
        task {
            logger.LogInformation $"Adding 5 to {n}"
            return n + 5
        }
```

## Conventions
FSharp.DurableExtensions requires the following conventions:
* Activity function names must match the method name.
* If an input/request object is passed to `CallActivity`, this must be the first argument of the activity function. (Any dependency parameters passed to the function, such as `ILogger`, must be listed after the input.)

## Retry Options
`RetryOptions` may optionally be passed in:

```F#
    [<FunctionName (name = "Orchestrator")>]
    member this.Orchestrator ([<OrchestrationTrigger>] ctx: IDurableOrchestrationContext, logger: ILogger) = 
        task {
            let retry = RetryOptions(TimeSpan.FromSeconds 5, 3)
            let! getOneResult = ctx.CallActivity(this.GetOne, retry)            
            let! addFiveResult = ctx.CallActivity(this.AddFive, getOneResult, retry)
            logger.LogInformation $"Result: = {addFiveResult}"
        }
```

