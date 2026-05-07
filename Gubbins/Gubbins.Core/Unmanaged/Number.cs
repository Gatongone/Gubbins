using Gubbins.Enhance;
using System.Runtime.CompilerServices;

namespace Gubbins.Unmanaged;

/// <summary>
/// Provides high-performance utility methods for number formatting and conversion operations.
/// This class contains optimized algorithms for converting numeric values to their string representations
/// with minimal memory allocation and maximum performance.
/// </summary>
internal static class Number
{
    /// <inheritdoc cref="MAX_INT64_NUMBERS_LENGTH"/>
    public static readonly int MaxInt64StringLength = MAX_INT64_NUMBERS_LENGTH;

    /// <inheritdoc cref="MAX_UINT64_NUMBERS_LENGTH"/>
    public static readonly int MaxUInt64StringLength = MAX_UINT64_NUMBERS_LENGTH;

    /// <inheritdoc cref="MAX_FLOAT_STRING_LENGTH"/>
    public static readonly int MaxSingleStringLength = MAX_FLOAT_STRING_LENGTH;

    /// <inheritdoc cref="MAX_DOUBLE_STRING_LENGTH"/>
    public static readonly int MaxDoubleStringLength = MAX_DOUBLE_STRING_LENGTH;

    /// <inheritdoc cref="MAX_DECIMAL_STRING_LENGTH"/>
    public static readonly int MaxDecimalStringLength = MAX_DECIMAL_STRING_LENGTH;

    /// <summary>Maximum length of a float value when formatted as a string.</summary>
    private const int MAX_FLOAT_STRING_LENGTH = 24;

    /// <summary>Maximum length of a double value when formatted as a string.</summary>
    private const int MAX_DOUBLE_STRING_LENGTH = 32;

    /// <summary>Maximum length of a decimal value when formatted as a string.</summary>
    private const int MAX_DECIMAL_STRING_LENGTH = 32;

    /// <summary>Maximum length of a long value when formatted as a string.</summary>
    private const int MAX_INT64_NUMBERS_LENGTH = 20; // 0 to 18446744073709551615

    /// <summary>Maximum length of a ulong value when formatted as a string.</summary>
    private const int MAX_UINT64_NUMBERS_LENGTH = 20; // -9223372036854775808 to 9223372036854775807

    /// <summary>Maximum length of leading zeros in floating point representation</summary>
    private const int MAX_FLOATING_ZERO_LENGTH = 5;

    /// <summary>Maximum positive double value</summary>
    private const double MAX_POSITIVE_DOUBLE_VALUE = double.MaxValue;

    /// <summary>Minimum positive double value</summary>
    private const double MIN_POSITIVE_DOUBLE_VALUE = 4.94065645841246544E-324;

    /// <summary>Character constant representing the positive sign</summary>
    private const char POSITIVE_SIGN = '+';

    /// <summary>Character constant representing the negative sign</summary>
    private const char NEGATIVE_SIGN = '-';

    /// <summary>Character constant representing the decimal point</summary>
    private const char DOT_SIGN = '.';

    /// <summary>Character constant representing the exponent notation</summary>
    private const char EXPONENT_SIGN = 'E';

    /// <summary>Pre-computed array of positive powers of 10 (1, 10, 100, 1000, 1e4, 1e5, 1e6, ... 1e308)</summary>
    private static double[] s_PositiveExponents;

    /// <summary>Pre-computed array of negative powers of 10 (1, 0.1, 0.01, 0.001, 1e-4, 1e-5, 1e-6, ... 1e-324)</summary>
    private static double[] s_NegativeExponents;

    /// <summary>Maximum digital length for float formatting: 7 + 1 (decimal point)</summary>
    private static readonly int s_MaxFloatDigitalLength = GetIntegerLength(0x7FFFFF) + 1;

    /// <summary>Maximum digital length for double formatting: 15 + 1 (decimal point)</summary>
    private static readonly int s_MaxDoubleDigitalLength = GetIntegerLength(0xFFFFFFFFFFFFF) + 1;

    /// <summary>
    /// Initializes static members of the Number class.
    /// Pre-computes arrays of positive and negative exponents for optimized power calculations.
    /// </summary>
    static Number()
    {
        // Initialize positive exponents array
        s_PositiveExponents = new double[SlowGetLength(MAX_POSITIVE_DOUBLE_VALUE)];
        for (var i = 0; i < s_PositiveExponents.Length; i++)
        {
            s_PositiveExponents[i] = Math.Pow(10, i);
        }

        // Initialize negative exponents array
        s_NegativeExponents = new double[SlowGetFractionalLength(MIN_POSITIVE_DOUBLE_VALUE)];
        for (var i = 0; i < s_NegativeExponents.Length; i++)
        {
            s_NegativeExponents[i] = Math.Pow(10, -i);
        }
    }

    /// <summary>
    /// Determines whether a float value is a special value (NaN, Infinity, or Negative Infinity).
    /// </summary>
    /// <param name="value">The float value to check.</param>
    /// <returns>True if the value is NaN, Infinity, or Negative Infinity; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpecialValue(float value)
    {
        const uint specialValue = 0x7F800000;
        return (Native.Cast<float, uint>(ref value) & specialValue) == specialValue;
    }

    /// <summary>
    /// Determines whether a double value is a special value (NaN, Infinity, or Negative Infinity).
    /// </summary>
    /// <param name="value">The double value to check.</param>
    /// <returns>True if the value is NaN, Infinity, or Negative Infinity; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpecialValue(double value)
    {
        const ulong specialValue = 0x7FF0000000000000;
        return (Native.Cast<double, ulong>(ref value) & specialValue) == specialValue;
    }

    /// <summary>
    /// Calculates the power of a floating-point number using pre-computed exponent arrays for optimization.
    /// </summary>
    /// <param name="x">The base floating-point number.</param>
    /// <param name="y">The exponent (power to raise x to).</param>
    /// <returns>The result of x raised to the power of y.</returns>
    private static double Pow(double x, int y)
    {
        if (y >= 0 && y < s_PositiveExponents.Length)
        {
            return x * s_PositiveExponents[y];
        }

        if (y < 0 && -y < s_PositiveExponents.Length)
        {
            return x / s_PositiveExponents[-y];
        }

        // For extremely large y values, divide by half and calculate twice
        return Pow(Pow(x, y / 2), y - y / 2);
    }

    /// <summary>
    /// Converts a double value to its UTF-16 string representation using unsafe pointer operations.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    /// <param name="dest">Pointer to the destination character buffer.</param>
    /// <returns>The number of characters written to the destination buffer.</returns>
    /// <remarks>
    /// The destination buffer must have sufficient space, requiring at most <see cref="MAX_DOUBLE_STRING_LENGTH"/> characters.
    /// </remarks>
    public static unsafe int WriteString(double value, char* dest)
    {
        if (IsSpecialValue(value))
        {
            return WriteSpecialValue(value, dest);
        }

        var start = dest;
        // Write negative sign if needed
        if (value < 0)
        {
            *dest = NEGATIVE_SIGN;
            dest++;
            value = -value;
        }

        // Get the exponent part of the floating-point number
        var exponent = GetExponent(value);
        // Length of integer part. If digits exceed max or negative, use scientific notation with integer length of 1
        var lengthOfInteger = exponent >= 0 && exponent < s_MaxDoubleDigitalLength ? exponent + 1 : 1;
        // Length of fractional part + 1 (for rounding)
        var lengthOfFractional = s_MaxDoubleDigitalLength - lengthOfInteger;
        // exponent now represents the scientific notation exponent. If 0, no scientific notation needed
        if (exponent < s_MaxDoubleDigitalLength && exponent > -MAX_FLOATING_ZERO_LENGTH)
        {
            exponent = 0;
        }

        // Convert the significant part of the floating-point number to integer: 123.45678 => 12345678
        var mantissa = (ulong) Pow(value, lengthOfFractional - exponent);

        ulong temp;
        // Round the last digit
        if (lengthOfFractional >= 1)
        {
            temp = mantissa / 10;
            if (mantissa - temp * 10 >= 5)
                mantissa = temp + 1;
            else
                mantissa = temp;
            lengthOfFractional -= 1;
        }

        // Remove trailing zeros
        while (lengthOfFractional >= 4 && mantissa == (temp = mantissa / 10000) * 10000)
        {
            mantissa           =  temp;
            lengthOfFractional -= 4;
        }

        while (lengthOfFractional > 0 &&
            mantissa == (temp = mantissa / 10) * 10)
        {
            mantissa           =  temp;
            lengthOfFractional -= 1;
        }

        // Write the digits
        if (lengthOfFractional == 0) // No fractional part, write as integer
        {
            var charsWritten = WriteString(mantissa, dest);
            if (charsWritten > lengthOfInteger && exponent != 0)
            {
                exponent     += 1;
                charsWritten -= 1;
            }

            dest += charsWritten;
        }
        // If integer part is 0, write integer part first
        else if (value < 1)
        {
            *dest       =  '0';
            *(dest + 1) =  DOT_SIGN;
            dest        += 2;
            // Integer is zero, no need to move decimal point, write fractional part directly
            var length = GetIntegerLength(mantissa);
            var firstNumIndex = lengthOfFractional - length;
            for (var i = 0; i < firstNumIndex; i++)
            {
                dest[i] = '0';
            }

            dest += firstNumIndex;
            WriteString(mantissa, dest);
            dest += length;
        }
        else
        {
            // Has fractional part, write as integer first, then insert decimal point
            WriteString(mantissa, dest);
            dest += lengthOfInteger;
            // Shift fractional part one position back to make room for decimal point
            for (var i = lengthOfFractional; i > 0;)
            {
                dest[i] = dest[--i];
            }

            *dest =  DOT_SIGN;
            dest  += lengthOfFractional + 1;
        }

        // If scientific notation is needed, write the scientific notation part
        if (exponent != 0)
        {
            dest[0] = EXPONENT_SIGN;
            if (exponent > 0)
            {
                dest[1] =  POSITIVE_SIGN;
                dest    += 2;
                dest    += WriteString((ulong) exponent, dest);
            }
            else
            {
                dest[1] =  NEGATIVE_SIGN;
                dest    += 2;
                dest    += WriteString((ulong) (-exponent), dest);
            }
        }

        return (int) (dest - start);
    }

    /// <summary>
    /// Converts a float value to its UTF-16 string representation using unsafe pointer operations.
    /// </summary>
    /// <param name="value">The float value to convert.</param>
    /// <param name="dest">Pointer to the destination character buffer.</param>
    /// <returns>The number of characters written to the destination buffer.</returns>
    /// <remarks>
    /// The destination buffer must have sufficient space, requiring at most <see cref="MAX_FLOAT_STRING_LENGTH"/> characters.
    /// </remarks>
    public static unsafe int WriteString(float value, char* dest)
    {
        if (IsSpecialValue(value))
        {
            return WriteSpecialValue(value, dest);
        }

        var start = dest;
        // Write negative sign if needed
        if (value < 0)
        {
            *dest = NEGATIVE_SIGN;
            dest++;
            value = -value;
        }

        // Get the exponent part of the floating-point number
        var exponent = GetExponent(value);
        // Length of integer part. If digits exceed max or negative, use scientific notation with integer length of 1
        var lengthOfInteger = exponent >= 0 && exponent < s_MaxFloatDigitalLength ? exponent + 1 : 1;
        // Length of fractional part + 1 (for rounding)
        var lengthOfFractional = s_MaxFloatDigitalLength - lengthOfInteger;
        // exponent now represents the scientific notation exponent. If 0, no scientific notation needed
        if (exponent < s_MaxFloatDigitalLength && exponent > -MAX_FLOATING_ZERO_LENGTH)
        {
            exponent = 0;
        }

        // Convert the significant part of the floating-point number to integer: 123.45678 => 12345678
        var mantissa = (ulong) Pow(value, lengthOfFractional - exponent);

        {
            ulong temp;
            // Round the last digit
            if (lengthOfFractional >= 1)
            {
                temp = mantissa / 10;
                if (mantissa - temp * 10 >= 5)
                    mantissa = temp + 1;
                else
                    mantissa = temp;
                lengthOfFractional -= 1;
            }

            // Remove trailing zeros
            while (lengthOfFractional >= 4 &&
                mantissa == (temp = mantissa / 10000) * 10000)
            {
                mantissa           =  temp;
                lengthOfFractional -= 4;
            }

            while (lengthOfFractional > 0 &&
                mantissa == (temp = mantissa / 10) * 10)
            {
                mantissa           =  temp;
                lengthOfFractional -= 1;
            }
        }
        // Write the digits
        if (lengthOfFractional == 0) // No fractional part, write as integer
        {
            var charsWritten = WriteString(mantissa, dest);
            if (charsWritten > lengthOfInteger && exponent != 0)
            {
                exponent     += 1;
                charsWritten -= 1;
            }

            dest += charsWritten;
        }
        // If integer part is 0, write integer part first
        else if (value < 1)
        {
            *dest       =  '0';
            *(dest + 1) =  DOT_SIGN;
            dest        += 2;
            // Integer is zero, no need to move decimal point, write fractional part directly
            var length = GetIntegerLength(mantissa);
            var firstNumIndex = lengthOfFractional - length;
            for (var i = 0; i < firstNumIndex; i++)
            {
                dest[i] = '0';
            }

            dest += firstNumIndex;
            WriteString(mantissa, dest);
            dest += length;
        }
        else
        {
            // Has fractional part, write as integer first, then insert decimal point
            WriteString(mantissa, dest);
            dest += lengthOfInteger;
            // Shift fractional part one position back to make room for decimal point
            for (var i = lengthOfFractional; i > 0;)
            {
                dest[i] = dest[--i];
            }

            *dest =  DOT_SIGN;
            dest  += lengthOfFractional + 1;
        }

        // If scientific notation is needed, write the scientific notation part
        if (exponent != 0)
        {
            dest[0] = EXPONENT_SIGN;
            if (exponent > 0)
            {
                dest[1] =  POSITIVE_SIGN;
                dest    += 2;
                dest    += WriteString((ulong) exponent, dest);
            }
            else
            {
                dest[1] =  NEGATIVE_SIGN;
                dest    += 2;
                dest    += WriteString((ulong) (-exponent), dest);
            }
        }

        return (int) (dest - start);
    }

    /// <summary>
    /// Gets the exponent part of a positive floating-point number.
    /// </summary>
    /// <param name="value">The positive floating-point number.</param>
    /// <returns>The exponent value.</returns>
    /// <remarks>This method does not handle negative numbers, NaN, infinity, or negative infinity.</remarks>
    private static int GetExponent(double value)
    {
        return value switch
        {
            >= 1        => GetPositiveExponent(value),
            > 0 and < 1 => GetNegativeExponent(value),
            0           => 0,
            // Negative numbers, NaN, infinity, or negative infinity are not supported
            _ => throw new NotSupportedException($"Invalid Float Point Value: {value}")
        };

        // Get the number of digits in the integer part of a number greater than 1, which is also the exponent in scientific notation
        static int GetPositiveExponent(double v)
        {
            var exponents = s_PositiveExponents;
            var i = 0;
            while (i + 128 < exponents.Length &&
                v >= exponents[i + 128])
            {
                i += 128;
            }

            while (i + 16 < exponents.Length &&
                v >= exponents[i + 16])
            {
                i += 16;
            }

            while (i < exponents.Length &&
                v >= exponents[i])
            {
                i += 1;
            }

            return i - 1;
        }

        // Get the exponent part of a number between 0 and 1, such as 1e-2 for 0.01
        static int GetNegativeExponent(double v)
        {
            var i = 0;
            var exponents = s_NegativeExponents;
            while (i + 128 < exponents.Length &&
                v < exponents[i + 128])
            {
                i += 128;
            }

            while (i + 16 < exponents.Length &&
                v < exponents[i + 16])
            {
                i += 16;
            }

            while (i < exponents.Length &&
                v < exponents[i])
            {
                i += 1;
            }

            return -i;
        }
    }

    private static int SlowGetLength(double value)
    {
        var result = 0;
        while (value >= 1e16)
        {
            value  /= 1e16;
            result += 16;
        }

        while (value >= 1)
        {
            value  /= 10;
            result += 1;
        }

        return result;
    }

    private static int SlowGetFractionalLength(double value)
    {
        var result = 0;
        while (value < 1e-16)
        {
            value  *= 1e16;
            result += 16;
        }

        while (value < 1)
        {
            value  *= 10;
            result += 1;
        }

        return result;
    }

    /// <summary>
    /// Converts a signed 64-bit unsigned integer to its UTF-16 string representation and writes it to the specified destination span.
    /// </summary>
    /// <param name="value">The signed 64-bit integer value to convert to string.</param>
    /// <param name="dest">The destination span of characters where the string representation will be written.</param>
    /// <returns>The number of characters written to the destination span, or 0 if the conversion failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int WriteString(ulong value, char* dest)
    {
        if (value < 10)
        {
            *dest = (char) ('0' + value);
            return 1;
        }

        var digits = GetIntegerLength(value);
        for (var i = digits; i > 0; i--)
        {
            var temp = '0' + value;
            value       /= 10;
            dest[i - 1] =  (char) (temp - value * 10);
        }

        return digits;
    }

    /// <summary>
    /// Converts a signed 64-bit integer to its UTF-16 string representation and writes it to the specified destination span.
    /// </summary>
    /// <param name="value">The signed 64-bit integer value to convert to string.</param>
    /// <param name="dest">The destination span of characters where the string representation will be written.</param>
    /// <returns>The number of characters written to the destination span, or 0 if the conversion failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int WriteString(long value, char* dest)
    {
        if (value < 0)
        {
            *dest = '-';
            value = unchecked(-value);
            return 1 + WriteString((ulong) value, dest + 1);
        }

        return WriteString((ulong) value, dest);
    }

    /// <summary>
    /// Calculates the number of digits in the decimal representation of an unsigned 64-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 64-bit integer value for which to calculate the digit count.</param>
    /// <returns>The number of digits in the decimal representation of the specified value, ranging from 1 to 20.</returns>
    private static int GetIntegerLength(ulong value)
    {
        if (value >= 100000)
        {
            if (value >= 10000000000)
                if (value >= 1000000000000000)
                    if (value >= 100000000000000000)
                        if (value >= 10000000000000000000) return 20;
                        else if (value >= 1000000000000000000) return 19;
                        else return 18;
                    else if (value >= 10000000000000000) return 17;
                    else return 16;
                else if (value >= 1000000000000)
                    if (value >= 100000000000000) return 15;
                    else if (value >= 10000000000000) return 14;
                    else return 13;
                else if (value >= 100000000000) return 12;
                else return 11;
            if (value >= 10000000)
                if (value >= 1000000000) return 10;
                else if (value >= 100000000) return 9;
                else return 8;
            if (value >= 1000000) return 7;
            return 6;
        }

        if (value >= 100)
            if (value >= 10000) return 5;
            else if (value >= 1000) return 4;
            else return 3;
        if (value >= 10) return 2;
        return 1;
    }

    /// <summary>
    /// Writes special floating-point values (NaN, Infinity, -Infinity) to the destination buffer.
    /// </summary>
    /// <param name="value">The special floating-point value to write.</param>
    /// <param name="dest">Pointer to the destination character buffer.</param>
    /// <returns>The number of characters written to the destination buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int WriteSpecialValue(double value, char* dest)
    {
        if (double.IsNaN(value))
        {
            dest[0] = 'N';
            dest[1] = 'a';
            dest[2] = 'N';
            return 3;
        }

        if (double.IsPositiveInfinity(value))
        {
            dest[0] = 'I';
            dest[1] = 'n';
            dest[2] = 'f';
            dest[3] = 'i';
            dest[4] = 'n';
            dest[5] = 'i';
            dest[6] = 't';
            dest[7] = 'y';
            return 8;
        }

        if (double.IsNegativeInfinity(value))
        {
            dest[0] = '-';
            dest[1] = 'I';
            dest[2] = 'n';
            dest[3] = 'f';
            dest[4] = 'i';
            dest[5] = 'n';
            dest[6] = 'i';
            dest[7] = 't';
            dest[8] = 'y';
            return 9;
        }

        return 0;
    }

    /// <summary>
    /// Writes special floating-point values (NaN, Infinity, -Infinity) to the destination buffer.
    /// </summary>
    /// <param name="value">The special floating-point value to write.</param>
    /// <param name="dest">Pointer to the destination character buffer.</param>
    /// <returns>The number of characters written to the destination buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int WriteSpecialValue(float value, char* dest)
    {
        if (float.IsNaN(value))
        {
            dest[0] = 'N';
            dest[1] = 'a';
            dest[2] = 'N';
            return 3;
        }

        if (float.IsPositiveInfinity(value))
        {
            dest[0] = 'I';
            dest[1] = 'n';
            dest[2] = 'f';
            dest[3] = 'i';
            dest[4] = 'n';
            dest[5] = 'i';
            dest[6] = 't';
            dest[7] = 'y';
            return 8;
        }

        if (float.IsNegativeInfinity(value))
        {
            dest[0] = '-';
            dest[1] = 'I';
            dest[2] = 'n';
            dest[3] = 'f';
            dest[4] = 'i';
            dest[5] = 'n';
            dest[6] = 'i';
            dest[7] = 't';
            dest[8] = 'y';
            return 9;
        }

        return 0;
    }
}