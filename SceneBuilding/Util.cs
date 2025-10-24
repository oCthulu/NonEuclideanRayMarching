public static class Util{
    public static string GetBinaryFunctionNest<T>(string functionName, Span<T> args, Func<T, string> getArgString, string extraArgs = ""){
        if(args.Length == 0) throw new ArgumentException("No arguments provided.");
        if(args.Length == 1) return getArgString(args[0]);

        return $"{functionName}({getArgString(args[0])}, {GetBinaryFunctionNest(functionName, args[1..], getArgString, extraArgs)}{extraArgs})";
    }
}