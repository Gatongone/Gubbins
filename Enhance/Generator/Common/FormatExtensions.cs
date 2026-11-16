// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/18-03:22:46
// Github: https://github.com/Gatongone

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Gubbins.Generator;

internal static class FormatExtensions
{
    internal static string Format(this string str, params Expression<Func<string, object>>[] args)
    {
        var parameters = args.ToDictionary
        (
            e => $"{{{e.Parameters[0].Name}}}",
            e => e.Compile()(e.Parameters[0].Name)
        );

        var sb = new StringBuilder(str);
        foreach (var kv in parameters)
        {
            sb.Replace(kv.Key, kv.Value != null ? kv.Value.ToString() : "");
        }

        return sb.ToString();
    }
}