using Unity.Burst;

namespace Gubbins.Span
{
    internal class BurstJobIntOperation : JobIntOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    internal class BurstJobUintOperation : JobUintOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    internal class BurstJobLongOperation : JobLongOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    internal class BurstJobUlongOperation : JobUlongOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    internal class BurstJobFloatOperation : JobFloatOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }

    internal class BurstJobDoubleOperation : JobDoubleOperation
    {
        public override bool Supported => base.Supported && BurstCompiler.IsEnabled;
    }
}