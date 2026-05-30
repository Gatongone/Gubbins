namespace Gubbins.Span
{
    internal static class SpanOperationsRegistration
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        private static void RegisterDefaultOperation()
        {
            SpanOperations.RegisterSpanOperation(new BurstIntOperation(), SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new BurstUintOperation(), SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new BurstLongOperation(), SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new BurstUlongOperation(), SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new BurstFloatOperation(), SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new BurstDoubleOperation(), SpanOperationMask.Simd);


            SpanOperations.RegisterSpanOperation(new JobIntOperation(), SpanOperationMask.Parallel);
            SpanOperations.RegisterSpanOperation(new JobUintOperation(), SpanOperationMask.Parallel);
            SpanOperations.RegisterSpanOperation(new JobLongOperation(), SpanOperationMask.Parallel);
            SpanOperations.RegisterSpanOperation(new JobUlongOperation(), SpanOperationMask.Parallel);
            SpanOperations.RegisterSpanOperation(new JobFloatOperation(), SpanOperationMask.Parallel);
            SpanOperations.RegisterSpanOperation(new JobDoubleOperation(), SpanOperationMask.Parallel);

            SpanOperations.RegisterSpanOperation(new JobIntOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new JobUintOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new JobLongOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new JobUlongOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new JobFloatOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
            SpanOperations.RegisterSpanOperation(new JobDoubleOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        }
    }
}