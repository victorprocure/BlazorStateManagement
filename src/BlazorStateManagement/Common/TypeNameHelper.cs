/// Copyright Microsoft: https://github.com/dotnet/dotnet/blob/main/src/aspnetcore/src/Shared/TypeNameHelper/TypeNameHelper.cs
using System.Collections.ObjectModel;
using System.Text;

namespace BlazorStateManagement.Common;

internal static class TypeNameHelper
{
    private const char DefaultNestedTypeDelimiter = '+';

    private static readonly ReadOnlyDictionary<Type, string> BuiltInTypeNames = new Dictionary<Type, string>
        {
            { typeof(void), "void" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(object), "object" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" }
        }.AsReadOnly();

    public static string GetTypeDisplayName(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = DefaultNestedTypeDelimiter)
    {
        var stringBuilder = default(StringBuilder);
        var name = ProcessType(ref stringBuilder, type, new DisplayNameOptions(fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter));
        return name ?? stringBuilder?.ToString() ?? string.Empty;
    }

    public static string GetTypeDisplayName<T>(bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = DefaultNestedTypeDelimiter)
    {
        return GetTypeDisplayName(typeof(T), fullName, includeGenericParameterNames, includeGenericParameters, nestedTypeDelimiter);
    }

    private static string? ProcessType(ref StringBuilder? stringBuilder, Type type, in DisplayNameOptions options)
    {
        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            stringBuilder ??= new StringBuilder();
            ProcessGenericType(stringBuilder, type, genericArguments, genericArguments.Length, options);
        }
        else if (type.IsArray)
        {
            stringBuilder ??= new StringBuilder();
            ProcessArrayType(stringBuilder, type, options);
        }
        else if (BuiltInTypeNames.TryGetValue(type, out var builtInName))
        {
            if (stringBuilder is null)
                return builtInName;

            stringBuilder.Append(builtInName);
        }
        else if (type.IsGenericParameter)
        {
            if (options.IncludeGenericParameterNames)
            {
                if (stringBuilder is null)
                    return type.Name;

                stringBuilder.Append(type.Name);
            }
        }
        else
        {
            var name = options.FullName ? type.FullName! : type.Name;

            if (stringBuilder is null)
            {
                if (options.NestedTypeDelimiter != DefaultNestedTypeDelimiter)
                {
                    return name.Replace(DefaultNestedTypeDelimiter, options.NestedTypeDelimiter);
                }

                return name;
            }

            stringBuilder.Append(name);
            if (options.NestedTypeDelimiter != DefaultNestedTypeDelimiter)
            {
                stringBuilder.Replace(DefaultNestedTypeDelimiter, options.NestedTypeDelimiter, stringBuilder.Length - name.Length, name.Length);
            }
        }

        return null;
    }

    private static void ProcessArrayType(StringBuilder builder, Type type, in DisplayNameOptions options)
    {
        var innerType = type;
        while (innerType.IsArray)
        {
            innerType = innerType.GetElementType()!;
        }

        ProcessType(ref builder!, innerType, options);

        while (type.IsArray)
        {
            builder.Append('[');
            builder.Append(',', type.GetArrayRank() - 1);
            builder.Append(']');
            type = type.GetElementType()!;
        }
    }

    private static void ProcessGenericType(StringBuilder stringBuilder, Type type, Type[] genericArguments, int length, in DisplayNameOptions options)
    {
        var offset = 0;
        if (type.IsNested)
        {
            offset = type.DeclaringType!.GetGenericArguments().Length;
        }

        if (options.FullName)
        {
            if (type.IsNested)
            {
                ProcessGenericType(stringBuilder, type.DeclaringType!, genericArguments, offset, options);
                stringBuilder.Append(options.NestedTypeDelimiter);
            }
            else if (!string.IsNullOrEmpty(type.Namespace))
            {
                stringBuilder.Append(type.Namespace);
                stringBuilder.Append('.');
            }
        }

        var genericPartIndex = type.Name.IndexOf('`');
        if (genericPartIndex <= 0)
        {
            stringBuilder.Append(type.Name);
            return;
        }

        stringBuilder.Append(type.Name, 0, genericPartIndex);
        if (options.IncludeGenericParameters)
        {
            stringBuilder.Append('<');
            for (int i = offset; i < length; i++)
            {
                ProcessType(ref stringBuilder!, genericArguments[i], options);
                if (i + 1 == length)
                {
                    continue;
                }

                stringBuilder.Append(',');
                if (options.IncludeGenericParameterNames || !genericArguments[i + 1].IsGenericParameter)
                {
                    stringBuilder.Append(' ');
                }
            }

            stringBuilder.Append('>');
        }
    }

    private readonly record struct DisplayNameOptions(bool FullName, bool IncludeGenericParameterNames, bool IncludeGenericParameters, char NestedTypeDelimiter);
}