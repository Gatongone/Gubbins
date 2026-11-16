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
            SpanOperations.RegisterSpanOperation(new BurstIntOperation(), IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstUintOperation(), IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstLongOperation(), IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstUlongOperation(), IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstFloatOperation(), IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstDoubleOperation(), IterationMode.Simd);

            SpanOperations.RegisterSpanOperation(new JobIntOperation(), IterationMode.Parallel);
            SpanOperations.RegisterSpanOperation(new JobUintOperation(), IterationMode.Parallel);
            SpanOperations.RegisterSpanOperation(new JobLongOperation(), IterationMode.Parallel);
            SpanOperations.RegisterSpanOperation(new JobUlongOperation(), IterationMode.Parallel);
            SpanOperations.RegisterSpanOperation(new JobFloatOperation(), IterationMode.Parallel);
            SpanOperations.RegisterSpanOperation(new JobDoubleOperation(), IterationMode.Parallel);

            SpanOperations.RegisterSpanOperation(new BurstJobIntOperation(), IterationMode.Parallel | IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstJobUintOperation(), IterationMode.Parallel | IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstJobLongOperation(), IterationMode.Parallel | IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstJobUlongOperation(), IterationMode.Parallel | IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstJobFloatOperation(), IterationMode.Parallel | IterationMode.Simd);
            SpanOperations.RegisterSpanOperation(new BurstJobDoubleOperation(), IterationMode.Parallel | IterationMode.Simd);
        }
    }
}