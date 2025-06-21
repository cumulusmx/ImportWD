using System.Globalization;

namespace ImportWD
{
	internal class WdIndoorRecord
	{
		// Uses space separated fields
		// 0 - day
		// 1 - month
		// 2 - year
		// 3 - hour
		// 4 - minute
		// 5 - temperature
		// 6 - humidity

		public DateTime? Timestamp { get; set; }

		public double? Temp { get; private set; }

		public int? Hum { get; private set; }



		public WdIndoorRecord(string entry, int lineNo)
		{
			var arr = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (arr.Length < 7)
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


			if (double.TryParse(arr[5], CultureInfo.InvariantCulture, out double temp))
			{
				Temp = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(temp) : ConvertUnits.TempFToUser(temp);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 6 (temperature)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 6 (temperature})", ConsoleColor.Red);
			}

			if (int.TryParse(arr[6], out int hum))
			{
				Hum = hum;
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 6 (humidity)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 6 (humidity})", ConsoleColor.Red);
			}
		}
	}
}
