using System;
using SFM.WindowResize;

public static class CommandLine
{
	private static readonly string[] Args = Environment.GetCommandLineArgs();

	public static bool HasArg(string argName)
	{
		foreach (var arg in Args)
		{
			if (
				arg.Equals(argName, StringComparison.OrdinalIgnoreCase) ||
				arg.StartsWith(argName + "=", StringComparison.OrdinalIgnoreCase)
			)
			{
				return true;
			}
		}
		return false;
	}

	public static T? GetArg<T>(string argName, Func<string, T> parseFunc) where T : struct
	{
		for (var i = 0; i < Args.Length; i++)
		{
			if (string.IsNullOrEmpty(Args[i]))
				continue;

			string? value = null;

			var equalIndex = Args[i].IndexOf("=");
			if (equalIndex >= 0)
			{
				var argPart = Args[i].Substring(0, equalIndex);
				if (argPart.Equals(argName, StringComparison.OrdinalIgnoreCase))
					value = Args[i].Substring(equalIndex + 1);
			}
			else if (Args[i].Equals(argName, StringComparison.OrdinalIgnoreCase) && i + 1 < Args.Length)
			{
				var next = Args[i + 1];
				var isNumber = next.Length > 1 && char.IsDigit(next[1]);
				if (!next.StartsWith("-") || isNumber)
					value = next;
			}

			if (value is not null)
			{
				try
				{
					return parseFunc(value);
				}
				catch (Exception ex)
				{
					Plugin.Log.LogWarning($"Failed to parse command line argument '{argName}' with value '{value}': {ex.Message}");
				}
			}
		}
		return null;
	}
}
