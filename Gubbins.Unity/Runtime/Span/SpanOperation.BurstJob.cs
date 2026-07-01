using Unity.Burst;

namespace Gubbins.Span
{
    /// <summary>
    /// Burst-compiled job-scheduled long integer span operation.
    /// </summary>
    internal class BurstJobIntOperation : JobIntOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    /// <summary>
    /// Burst-compiled job-scheduled unsigned integer span operation.
    /// </summary>
    internal class BurstJobUintOperation : JobUintOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    /// <summary>
    /// Burst-compiled job-scheduled long integer span operation.
    /// </summary>
    internal class BurstJobLongOperation : JobLongOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    /// <summary>
    /// Burst-compiled job-scheduled unsigned long integer span operation.
    /// </summary>
    internal class BurstJobUlongOperation : JobUlongOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    /// <summary>
    /// Burst-compiled job-scheduled floating-point span operation.
    /// </summary>
    internal class BurstJobFloatOperation : JobFloatOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    /// <summary>
    /// Burst-compiled job-scheduled double-precision floating-point span operation.
    /// </summary>
    internal class BurstJobDoubleOperation : JobDoubleOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }
}