using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Gubbins.Enhance;

namespace Gubbins.Unity
{
    public class BatchTest
    {
        public static int Count = 1024 * 1024 * 8;

        private static IBatchOperation<int> op1 = new SerialOperation<int>();
        private static IBatchOperation<int> op2 = new SimdOperation<int>();
        private static IBatchOperation<int> op3 = new ParallelOperation<int>();
        private static IBatchOperation<int> op4 = new ParallelSimdOperation<int>();
        private static IBatchOperation<int> op5 = new GpuOperation<int>();
        private static IBatchOperation<int> op6 = new BurstOperation<int>();
        private static IBatchOperation<int> op7 = new BurstJobIntOperation();

        public static string Test(int count)
        {
            Count = count;
            var sb = new StringBuilder();
            sb.AppendLine($"|{"Pattern",-20}|{"Spent/ms",-12}|{"Spent/tick",-12}|{"Result(top6)",-12}|");
            sb.AppendLine($"|{new string('-', 20),-20}|{new string('-', 12),-12}|{new string('-', 12),-12}|{new string('-', 12),-12}|");
            sb.AppendLine(DoAdd(op1));
            sb.AppendLine(DoAdd(op2));
            sb.AppendLine(DoAdd(op3));
            sb.AppendLine(DoAdd(op4));
            sb.AppendLine(DoAdd(op5));
            sb.AppendLine(DoAdd(op6));
            sb.AppendLine(DoAdd(op7));
            return sb.ToString();
        }

        private static string DoAdd(IBatchOperation<int> operation)
        {
            var st = new Stopwatch();
            var array = Enumerable.Range(1, Count).ToArray();
            st.Start();
            operation.Add(array, 1);
            st.Stop();
            var result = $"|{operation.GetType().Name.Replace("Operation`1", string.Empty),-20}|{st.ElapsedMilliseconds,-12}|{st.ElapsedTicks,-12}|{string.Join(",", array.Take(5)),-12}|";
            GC.Collect();
            return result;
        }
    }
}