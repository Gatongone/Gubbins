namespace Gubbins.Enhance;

/// <summary>
/// Provides atomic operations for thread-safe manipulation of shared variables.
/// </summary>
public static class Atomic
{
    /// <summary>
    /// Defines a delegate that transforms two values of the same type into a result value.
    /// </summary>
    /// <typeparam name="T">The type of the values to transform.</typeparam>
    /// <param name="left">The first value to transform.</param>
    /// <param name="right">The second value to transform.</param>
    /// <returns>The transformed result value.</returns>
    public delegate T Morpher<T>(T left, T right);

    /// <summary>
    /// Atomically applies a transformation function to a reference type target using the provided value.
    /// </summary>
    /// <typeparam name="T">The reference type of the target and value.</typeparam>
    /// <param name="target">The reference to the target variable to be modified atomically.</param>
    /// <param name="value">The value to be used in the transformation.</param>
    /// <param name="morpher">The function that defines how to combine the current target value with the provided value.</param>
    /// <returns>The new value that was successfully stored in the target.</returns>
    public static T Morph<T>(ref T target, T value, Morpher<T> morpher) where T : class
    {
        var curVal = target;
        T startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = morpher(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically applies a transformation function to an integer target using the provided value.
    /// </summary>
    /// <param name="target">The reference to the integer variable to be modified atomically.</param>
    /// <param name="value">The integer value to be used in the transformation.</param>
    /// <param name="morpher">The function that defines how to combine the current target value with the provided value.</param>
    /// <returns>The new integer value that was successfully stored in the target.</returns>
    public static int Morph(ref int target, int value, Morpher<int> morpher)
    {
        var curVal = target;
        int startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = morpher(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically applies a transformation function to a long integer target using the provided value.
    /// </summary>
    /// <param name="target">The reference to the long integer variable to be modified atomically.</param>
    /// <param name="value">The long integer value to be used in the transformation.</param>
    /// <param name="morpher">The function that defines how to combine the current target value with the provided value.</param>
    /// <returns>The new long integer value that was successfully stored in the target.</returns>
    public static long Morph(ref long target, long value, Morpher<long> morpher)
    {
        var curVal = target;
        long startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = morpher(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically applies a transformation function to a double-precision floating-point target using the provided value with tolerance-based comparison.
    /// </summary>
    /// <param name="target">The reference to the double variable to be modified atomically.</param>
    /// <param name="value">The double value to be used in the transformation.</param>
    /// <param name="tolerance">The tolerance value used for floating-point comparison to determine operation completion.</param>
    /// <param name="morpher">The function that defines how to combine the current target value with the provided value.</param>
    /// <returns>The new double value that was successfully stored in the target.</returns>
    public static double Morph(ref double target, double value, double tolerance, Morpher<double> morpher)
    {
        var curVal = target;
        double startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = morpher(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (Math.Abs(startVal - curVal) > tolerance);

        return desiredVal;
    }

    /// <summary>
    /// Atomically sets the target to the maximum of its current value and the provided value.
    /// </summary>
    /// <param name="target">The reference to the integer variable to be modified atomically.</param>
    /// <param name="value">The integer value to compare with the current target value.</param>
    /// <returns>The maximum value that was successfully stored in the target.</returns>
    public static int Maximum(ref int target, int value)
    {
        var curVal = target;
        int startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = Math.Max(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically sets the target to the maximum of its current value and the provided value.
    /// </summary>
    /// <param name="target">The reference to the long integer variable to be modified atomically.</param>
    /// <param name="value">The long integer value to compare with the current target value.</param>
    /// <returns>The maximum value that was successfully stored in the target.</returns>
    public static long Maximum(ref long target, long value)
    {
        var curVal = target;
        long startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = Math.Max(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically sets the target to the maximum of its current value and the provided value with tolerance-based comparison.
    /// </summary>
    /// <param name="target">The reference to the double variable to be modified atomically.</param>
    /// <param name="value">The double value to compare with the current target value.</param>
    /// <param name="tolerance">The tolerance value used for floating-point comparison to determine operation completion.</param>
    /// <returns>The maximum value that was successfully stored in the target.</returns>
    public static double Maximum(ref double target, double value, double tolerance)
    {
        var curVal = target;
        double startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = Math.Max(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (Math.Abs(startVal - curVal) > tolerance);

        return desiredVal;
    }

    /// <summary>
    /// Atomically sets the target to the minimum of its current value and the provided value.
    /// </summary>
    /// <param name="target">The reference to the integer variable to be modified atomically.</param>
    /// <param name="value">The integer value to compare with the current target value.</param>
    /// <returns>The minimum value that was successfully stored in the target.</returns>
    public static int Minimum(ref int target, int value)
    {
        var curVal = target;
        int startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = Math.Min(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically sets the target to the minimum of its current value and the provided value.
    /// </summary>
    /// <param name="target">The reference to the long integer variable to be modified atomically.</param>
    /// <param name="value">The long integer value to compare with the current target value.</param>
    /// <returns>The minimum value that was successfully stored in the target.</returns>
    public static long Minimum(ref long target, long value)
    {
        var curVal = target;
        long startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = Math.Min(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically sets the target to the minimum of its current value and the provided value with tolerance-based comparison.
    /// </summary>
    /// <param name="target">The reference to the double variable to be modified atomically.</param>
    /// <param name="value">The double value to compare with the current target value.</param>
    /// <param name="tolerance">The tolerance value used for floating-point comparison to determine operation completion.</param>
    /// <returns>The minimum value that was successfully stored in the target.</returns>
    public static double Minimum(ref double target, double value, double tolerance)
    {
        var curVal = target;
        double startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = Math.Min(startVal, value);
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (Math.Abs(startVal - curVal) > tolerance);

        return desiredVal;
    }

    /// <summary>
    /// Atomically multiplies the target value by the specified multiplier.
    /// </summary>
    /// <param name="target">The reference to the integer variable to be modified atomically.</param>
    /// <param name="multiplier">The integer value to multiply the current target value by.</param>
    /// <returns>The product value that was successfully stored in the target.</returns>
    public static int Multiply(ref int target, int multiplier)
    {
        var curVal = target;
        int startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = curVal * multiplier;
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically multiplies the target value by the specified multiplier.
    /// </summary>
    /// <param name="target">The reference to the long integer variable to be modified atomically.</param>
    /// <param name="multiplier">The long integer value to multiply the current target value by.</param>
    /// <returns>The product value that was successfully stored in the target.</returns>
    public static long Multiply(ref long target, long multiplier)
    {
        var curVal = target;
        long startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = curVal * multiplier;
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically multiplies the target value by the specified multiplier with tolerance-based comparison.
    /// </summary>
    /// <param name="target">The reference to the double variable to be modified atomically.</param>
    /// <param name="multiplier">The double value to multiply the current target value by.</param>
    /// <param name="tolerance">The tolerance value used for floating-point comparison to determine operation completion.</param>
    /// <returns>The product value that was successfully stored in the target.</returns>
    public static double Multiply(ref double target, double multiplier, double tolerance)
    {
        var curVal = target;
        double startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = curVal * multiplier;
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (Math.Abs(startVal - curVal) > tolerance);

        return desiredVal;
    }

    /// <summary>
    /// Atomically divides the target value by the specified divisor.
    /// </summary>
    /// <param name="target">The reference to the integer variable to be modified atomically.</param>
    /// <param name="divisor">The integer value to divide the current target value by.</param>
    /// <returns>The quotient value that was successfully stored in the target.</returns>
    public static int Division(ref int target, int divisor)
    {
        var curVal = target;
        int startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = curVal / divisor;
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically divides the target value by the specified divisor.
    /// </summary>
    /// <param name="target">The reference to the long integer variable to be modified atomically.</param>
    /// <param name="divisor">The long integer value to divide the current target value by.</param>
    /// <returns>The quotient value that was successfully stored in the target.</returns>
    public static long Division(ref long target, long divisor)
    {
        var curVal = target;
        long startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = curVal / divisor;
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (startVal != curVal);

        return desiredVal;
    }

    /// <summary>
    /// Atomically divides the target value by the specified divisor with tolerance-based comparison.
    /// </summary>
    /// <param name="target">The reference to the double variable to be modified atomically.</param>
    /// <param name="divisor">The double value to divide the current target value by.</param>
    /// <param name="tolerance">The tolerance value used for floating-point comparison to determine operation completion.</param>
    /// <returns>The quotient value that was successfully stored in the target.</returns>
    public static double Division(ref double target, double divisor, double tolerance)
    {
        var curVal = target;
        double startVal, desiredVal;
        do
        {
            startVal = curVal;
            desiredVal = curVal / divisor;
            curVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
        } while (Math.Abs(startVal - curVal) > tolerance);

        return desiredVal;
    }
}
