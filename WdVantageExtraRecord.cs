using System.Globalization;

namespace ImportWD
{
	internal class WdVantageExtraRecord
	{
		// Uses space separated fields
		// 0 - day
		// 1 - month
		// 2 - year
		// 3 - hour
		// 4 - minute
		// 5 - Temp-1
		// 6 - Temp-2
		// 7 - Temp-3
		// 8 - Temp-4
		// 9 - Temp-5
		// 10 - Temp-6
		// 11 - Temp-7
		// 12 - Hum-1
		// 13 - Hum-2
		// 14 - Hum-3
		// 15 - Hum-4
		// 16 - Hum-5
		// 17 - Hum-6
		// 18 - Hum-7

		public DateTime? Timestamp { get; set; }

		public double?[] Temp { get; private set; } = [null, null, null, null, null, null, null];

		public int?[] Hum { get; private set; } = [null, null, null, null, null, null, null];


		public WdVantageExtraRecord(string entry, int lineNo)
		{
			var arr = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (arr.Length < 19)
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing entry: " + entry);
				Program.LogConsole("  Error parsing entry: " + entry, ConsoleColor.Red);
				return;
			}

			try
			{
				Timestamp = new DateTime(int.Parse(arr[2]), int.Parse(arr[1]), int.Parse(arr[0]), int.Parse(arr[3]), int.Parse(arr[4]), 0, DateTimeKind.Local);
			}
			catch (Exception ex)
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing date/time fields: " + ex.Message);
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing date/time fields: " + ex.Message, ConsoleColor.Red);
				return;
			}

			// skip the first five entries (date/time)

			for (var i = 5; i < 12; i++)
			{
				if (double.TryParse(arr[i], CultureInfo.InvariantCulture, out double temp))
				{
					Temp[i - 5] = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(temp) : ConvertUnits.TempFToUser(temp);
				}
				else
				{
					Program.LogMessage($"  Line {lineNo}: Error parsing field {i + 5} (temperature-{i - 4})");
					Program.LogMessage("  Error line: " + entry);
					Program.LogConsole($"  Error parsing field {i + 5} (temperature-{i - 4})", ConsoleColor.Red);
				}
			}

			for (var i = 12; i < 19; i++)
			{
				if (int.TryParse(arr[i], out int hum))
				{
					Hum[i - 12] = hum;
				}
				else
				{
					Program.LogMessage($"  Line {lineNo}: Error parsing field {i + 12} (humidity-{i - 11})");
					Program.LogMessage("  Error line: " + entry);
					Program.LogConsole($"  Error parsing field {i + 12} (humidity-{i - 11})", ConsoleColor.Red);
				}
			}
		}
	}
}
