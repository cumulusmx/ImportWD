using System.Globalization;

namespace ImportWD
{
	internal class WdVantageRecord
	{
		// Uses space separated fields
		// 0 - day
		// 1 - month
		// 2 - year
		// 3 - hour
		// 4 - minute
		// 5 - solar radiation
		// 6 - UV-I
		// 7 - ET today
		// 8 - Soil moisture
		// 9 - Soil temperature

		public DateTime? Timestamp { get; set; }
		public int? SolarRad { get; private set; }
		public double? UVI { get; private set; }
		public double? ET { get; private set; }
		public int? SoilMoisture { get; private set; }
		public double? SoilTemp { get; private set; }

		public WdVantageRecord(string entry, int lineNo)
		{
			var arr = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (arr.Length < 10)
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing entry: {entry}");
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
			if (double.TryParse(arr[5], out double sol))
			{
				SolarRad = (int) sol;
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 6 (solar rad)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 6 (solar rad)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[6], CultureInfo.InvariantCulture, out double uv))
			{
				UVI = uv;
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 7 (UV-I)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 7 (UV-I)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[7], CultureInfo.InvariantCulture, out double et))
			{
				ET = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(et) : ConvertUnits.RainMMToUser(et);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 8 (ET)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 8 (ET)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[8], out double sm))
			{
				if (sm < 255)
					SoilMoisture = (int) sm;
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 9 (soil moisture)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 9 (soil moisture)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[9], CultureInfo.InvariantCulture, out double st))
			{
				SoilTemp = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(st) : ConvertUnits.TempFToUser(st);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 8 (soil temperature)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 8 (soil temperature)", ConsoleColor.Red);
			}
		}
	}
}
