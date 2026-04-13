namespace HNTAS.Core.Api.Common;

public record ValidationGateResult(bool IsValid, string Message = "", int StatusCode = 400);

