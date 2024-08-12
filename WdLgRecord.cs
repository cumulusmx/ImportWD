using System.Globalization;

namespace ImportWD
{
	internal class WdLgRecord
	{
		// Uses space separated fields
		// 0 - day
		// 1 - month
		// 2 - year
		// 3 - hour
		// 4 - minute
		// 5 - temperature
		// 6 - humidity
		// 7 - dewpoint
		// 8 - pressure
		// 9 - wind speed
		// 10 - wind gust
		// 11 - wind direction
		// 12 - rain 1 min
		// 13 - rain today
		// 14 - rain month
		// 15 - rain year
		// 16 - heat index

		public DateTime? Timestamp { get; private set; }
		public double? OutsideTemp { get; private set; }
		public int? OutsideHumidity { get; private set; }
		public double? Dewpoint { get; private set; }
		public double? Baro { get; private set; }
		public double? WindSpeed { get; private set; }
		public double? WindGust { get; private set; }
		public int WindDir { get; private set; } = 0;
		public double? RainRate { get; private set; }
		public double? RainDay { get; private set; }
		public double? RainMonth { get; private set; }
		public double? RainYear { get; private set; }
		public double? HeatIndex { get; private set; }

		public WdLgRecord(string entry)
		{
			var arr = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			try
			{
				Timestamp = new DateTime(int.Parse(arr[2]), int.Parse(arr[1]), int.Parse(arr[0]), int.Parse(arr[3]), int.Parse(arr[4]), 0, DateTimeKind.Local);
			}
			catch(Exception ex)
			{
				Program.LogMessage("  Error parsing date/time fields: " + ex.Message);
				Program.LogConsole("  Error parsing date/time fields: " + ex.Message, ConsoleColor.Red);
				return;
			}

			// skip the first five entries (date/time)
			if (double.TryParse(arr[5], CultureInfo.InvariantCulture, out double temp))
			{
				OutsideTemp = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(temp) : ConvertUnits.TempFToUser(temp);
			}
			else
			{
				Program.LogMessage("  Error parsing field 6 (temperature)");
				Program.LogConsole("  Error parsing field 6 (temperature)", ConsoleColor.Red);
			}

			if (int.TryParse(arr[6], out int hum))
			{
				OutsideHumidity = hum;
			}
			else
			{
				Program.LogMessage("  Error parsing field 7 (humidity)");
				Program.LogConsole("  Error parsing field 7 (humidity)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[7], CultureInfo.InvariantCulture, out double dew))
			{
				Dewpoint = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(dew) : ConvertUnits.TempFToUser(dew);
			}
			else
			{
				Program.LogMessage("  Error parsing field 8 (temperature)");
				Program.LogConsole("  Error parsing field 8 (temperature)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[8], CultureInfo.InvariantCulture, out double baro))
			{
				Baro = Program.WdConfigPress == "inhg" ? ConvertUnits.PressINHGToUser(baro) : ConvertUnits.PressMBToUser(baro);
			}
			else
			{
				Program.LogMessage("  Error parsing field 9 (pressure)");
				Program.LogConsole("  Error parsing field 9 (pressure)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[9], CultureInfo.InvariantCulture, out double wind))
			{
				switch (Program.WdConfigWind)
				{
					case "kph": WindSpeed = ConvertUnits.WindKPHToUser(wind); break;
					case "ms": WindSpeed = ConvertUnits.WindMSToUser(wind); break;
					case "mph": WindSpeed = ConvertUnits.WindMPHToUser(wind); break;
					case "knots": WindSpeed = ConvertUnits.WindKnotsToUser(wind); break;
				}
			}
			else
			{
				Program.LogMessage("  Error parsing field 10 (wind speed)");
				Program.LogConsole("  Error parsing field 10 (wind speed)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[10], CultureInfo.InvariantCulture, out double gust))
			{
				switch (Program.WdConfigWind)
				{
					case "kph": WindGust = ConvertUnits.WindKPHToUser(gust); break;
					case "ms": WindGust = ConvertUnits.WindMSToUser(gust); break;
					case "mph": WindGust = ConvertUnits.WindMPHToUser(gust); break;
					case "knots": WindGust = ConvertUnits.WindKnotsToUser(gust); break;
				}
			}
			else
			{
				Program.LogMessage("  Error parsing field 11 (wind gust)");
				Program.LogConsole("  Error parsing field 11 (wind gust)", ConsoleColor.Red);
			}

			if (int.TryParse(arr[11], out int dir))
			{
				WindDir = dir;
			}
			else
			{
				Program.LogMessage("  Error parsing field 12 (wind direction)");
				Program.LogConsole("  Error parsing field 12 (wind direction)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[12], CultureInfo.InvariantCulture, out double rrate))
			{
				RainRate = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(rrate) : ConvertUnits.RainMMToUser(rrate);
			}
			else
			{
				Program.LogMessage("  Error parsing field 13 (rain 1 hr");
				Program.LogConsole("  Error parsing field 13 (rain 1 hr)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[13], CultureInfo.InvariantCulture, out double rday))
			{
				RainDay = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(rday) : ConvertUnits.RainMMToUser(rday);
			}
			else
			{
				Program.LogMessage("  Error parsing field 14 (rain day");
				Program.LogConsole("  Error parsing field 14 (rain day)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[14], CultureInfo.InvariantCulture, out double rmon))
			{
				RainMonth = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(rmon) : ConvertUnits.RainMMToUser(rmon);
			}
			else
			{
				Program.LogMessage("  Error parsing field 15 (rain month");
				Program.LogConsole("  Error parsing field 15 (rain month)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[15], CultureInfo.InvariantCulture, out double ryr))
			{
				RainYear = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(ryr) : ConvertUnits.RainMMToUser(ryr);
			}
			else
			{
				Program.LogMessage("  Error parsing field 16 (rain year");
				Program.LogConsole("  Error parsing field 16 (rain year)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[16], CultureInfo.InvariantCulture, out double hi))
			{
				HeatIndex = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(hi) : ConvertUnits.TempFToUser(hi);
			}
			else
			{
				Program.LogMessage("  Error parsing field 17 (heat index)");
				Program.LogConsole("  Error parsing field 17 (heat index)", ConsoleColor.Red);
			}
		}
	}
}
