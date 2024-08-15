using System.Globalization;
using System.Text.RegularExpressions;

namespace ImportWD
{
	internal class WdExtraSensorsRecord
	{
		// Uses comma+space separated fields
		// 0 - day
		// 1 - month
		// 2 - year
		// 3 - hour
		// 4 - minute
		// 5 - Temp-1
		// 6 - Hum-1
		// 7 - Temp-2
		// 8 - Hum-2
		// 9 - Temp-3
		// 10 - Hum-3
		// 11 - Temp-4
		// 12 - Hum-4
		// 13 - Temp-5
		// 14 - Hum-5
		// 15 - Temp-6
		// 16 - Hum-6
		// 17 - Temp-7
		// 18 - Hum-7
		// 19 - Temp-8
		// 20 - Hum-8
		// 21 - Temp-9
		// 22 - Hum-9

		public DateTime? Timestamp { get; private set; }

		public double?[] Temp { get; private set; } = { null, null, null, null, null, null, null, null, null };

		public int?[] Hum { get; private set; } = { null, null, null, null, null, null, null, null, null };


		public WdExtraSensorsRecord(string entry)
		{
			var arr = Regex.Split(entry, @"\s*[, ]\s*")
				.Where(substring => !string.IsNullOrWhiteSpace(substring))
				.ToArray();


			try
			{
				Timestamp = new DateTime(int.Parse(arr[2]), int.Parse(arr[1]), int.Parse(arr[0]), int.Parse(arr[3]), int.Parse(arr[4]), 0, DateTimeKind.Local);
			}
			catch (Exception ex)
			{
				Program.LogMessage("  Error parsing date/time fields: " + ex.Message);
				Program.LogConsole("  Error parsing date/time fields: " + ex.Message, ConsoleColor.Red);
				return;
			}

			// skip the first five entries (date/time)

			var ind = 0;
			// temperature in fileds 5, 7, 9 etc
			for (var i = 5; i < arr.Length; i += 2)
			{
				if (double.TryParse(arr[i], CultureInfo.InvariantCulture, out double temp))
				{
					if (temp > -100)
					{
						Temp[ind] = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(temp) : ConvertUnits.TempFToUser(temp);
					}
					ind++;
				}
				else
				{
					Program.LogMessage($"  Error parsing field {i} (temperature-{i - 4})");
					Program.LogConsole($"  Error parsing field {i} (temperature-{i - 4})", ConsoleColor.Red);
				}
			}

			ind = 0;
			// humidity in fileds 6, 8,10 etc
			for (var i = 6; i < arr.Length; i += 2)
			{
				if (int.TryParse(arr[i], out int hum))
				{
					if (hum > -100)
					{
						Hum[ind] = hum;
					}
					ind++;
				}
				else
				{
					Program.LogMessage($"  Error parsing field {i} (humidity-{i - 5})");
					Program.LogConsole($"  Error parsing field {i} (humidity-{i - 5})", ConsoleColor.Red);
				}
			}
		}
	}
}
