namespace MicroM.Core;

public record ResultWithStatus<TResult, TStatus>
(
    TResult? Result,
    TStatus? Status
);
