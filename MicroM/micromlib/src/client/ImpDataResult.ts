
export interface ImpDataResult {
    ProcessedCount: number,
    SuccessCount: number,
    ErrorCount: number,
    Errors: Record<number, string>
}