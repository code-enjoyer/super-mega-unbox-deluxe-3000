namespace SuperMegaUnboxDeluxe.Common.Results;

public enum ErrorKind { Validation, NotFound }

public sealed record Error(ErrorKind Kind, string Code, string Title, string? Detail = null);
