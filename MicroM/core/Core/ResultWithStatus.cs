namespace MicroM.Core;

public record ResultWithStatus<T, E>
(
    T? Result,
    E? Status
);
