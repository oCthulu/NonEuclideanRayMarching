using SceneBuilding;

public static class Util
{
    public static string GetBinaryFunctionNest<T>(string functionName, Span<T> args, Func<T, string> getArgString, string extraArgs = "")
    {
        if (args.Length == 0) throw new ArgumentException("No arguments provided.");
        if (args.Length == 1) return getArgString(args[0]);

        return $"{functionName}({getArgString(args[0])}, {GetBinaryFunctionNest(functionName, args[1..], getArgString, extraArgs)}{extraArgs})";
    }

    public static string AppendBinaryFunctionSequence<T>(SourceBuilder sb, string varType, string functionName, Span<T> args, Func<T, string> getArgString)
    {
        if (args.Length == 0) throw new ArgumentException("No arguments provided.");

        string varName = sb.NewVariableName();

        sb.AppendLine($"{varType} {varName} = {getArgString(args[0])};");
        for (int i = 1; i < args.Length; i++)
        {
            sb.AppendLine($"{varName} = {functionName}({varName}, {getArgString(args[i])});");

        }

        return varName;
    }
}