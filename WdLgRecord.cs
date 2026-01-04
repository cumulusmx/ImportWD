using System.Globalization;
using System.Text;

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

		public DateTime? Timestamp { get; set; }
		public double? OutsideTemp { get; private set; }
		public double? OutsideHumidity { get; private set; }
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

		public WdLgRecord(string entry, int lineNo)
		{
			var arr = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (arr.Length < 17)
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing entry: {entry}");
				Program.LogConsole("  Error parsing entry: " + entry, ConsoleColor.Red);
				return;
			}

			try
			{
				Timestamp = new DateTime(int.Parse(arr[2]), int.Parse(arr[1]), int.Parse(arr[0]), int.Parse(arr[3]), int.Parse(arr[4]), 0, DateTimeKind.Local);
			}
			catch(Exception ex)
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing date/time fields: {ex.Message}");
				Program.LogMessage("  Error line: " + entry);
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
				Program.LogMessage($"  Line {lineNo}: Error parsing field 6 (temperature)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 6 (temperature)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[6], CultureInfo.InvariantCulture, out double hum))
			{
				OutsideHumidity = hum;
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 7 (humidity)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 7 (humidity)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[7], CultureInfo.InvariantCulture, out double dew))
			{
				Dewpoint = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(dew) : ConvertUnits.TempFToUser(dew);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 8 (temperature)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 8 (temperature)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[8], CultureInfo.InvariantCulture, out double baro))
			{
				Baro = Program.WdConfigPress == "inhg" ? ConvertUnits.PressINHGToUser(baro) : ConvertUnits.PressMBToUser(baro);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 9 (pressure)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 9 (pressure)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[9], CultureInfo.InvariantCulture, out double wind))
			{
				switch (Program.WdConfigWind)
				{
					case "kph": WindSpeed = ConvertUnits.WindKPHToUser(wind); break;
					case "m/s": WindSpeed = ConvertUnits.WindMSToUser(wind); break;
					case "mph": WindSpeed = ConvertUnits.WindMPHToUser(wind); break;
					case "knots": WindSpeed = ConvertUnits.WindKnotsToUser(wind); break;
				}
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 10 (wind speed)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 10 (wind speed)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[10], CultureInfo.InvariantCulture, out double gust))
			{
				switch (Program.WdConfigWind)
				{
					case "kph": WindGust = ConvertUnits.WindKPHToUser(gust); break;
					case "m/s": WindGust = ConvertUnits.WindMSToUser(gust); break;
					case "mph": WindGust = ConvertUnits.WindMPHToUser(gust); break;
					case "knots": WindGust = ConvertUnits.WindKnotsToUser(gust); break;
				}
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 11 (wind gust)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 11 (wind gust)", ConsoleColor.Red);
			}

			if (int.TryParse(arr[11], out int dir))
			{
				WindDir = dir;
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 12 (wind direction)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 12 (wind direction)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[12], CultureInfo.InvariantCulture, out double rrate))
			{
				RainRate = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(rrate) : ConvertUnits.RainMMToUser(rrate);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 13 (rain 1 hr");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 13 (rain 1 hr)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[13], CultureInfo.InvariantCulture, out double rday))
			{
				RainDay = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(rday) : ConvertUnits.RainMMToUser(rday);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 14 (rain day");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 14 (rain day)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[14], CultureInfo.InvariantCulture, out double rmon))
			{
				RainMonth = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(rmon) : ConvertUnits.RainMMToUser(rmon);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 15 (rain month");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 15 (rain month)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[15], CultureInfo.InvariantCulture, out double ryr))
			{
				RainYear = Program.WdConfigRain == "in" ? ConvertUnits.RainINToUser(ryr) : ConvertUnits.RainMMToUser(ryr);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 16 (rain year");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 16 (rain year)", ConsoleColor.Red);
			}

			if (double.TryParse(arr[16], CultureInfo.InvariantCulture, out double hi))
			{
				HeatIndex = Program.WdConfigTemp == "c" ? ConvertUnits.TempCToUser(hi) : ConvertUnits.TempFToUser(hi);
			}
			else
			{
				Program.LogMessage($"  Line {lineNo}: Error parsing field 17 (heat index)");
				Program.LogMessage("  Error line: " + entry);
				Program.LogConsole("  Error parsing field 17 (heat index)", ConsoleColor.Red);
			}
		}

		public override string ToString()
		{
			var inv = CultureInfo.InvariantCulture;
			var sb = new StringBuilder(256);

			sb.Append("LgFileRecord : ");
			sb.Append("LogTime=" + Timestamp?.ToString("yyyy-MM-dd HH:mm:ss", inv) + ", ");
			sb.Append("Temperature=" + OutsideTemp?.ToString(Program.Cumulus.TempFormat, inv)  + ", ");
			sb.Append("Humidity=" + OutsideHumidity?.ToString(Program.Cumulus.TempFormat, inv) + ", ");
			sb.Append("Dewpoint=" + Dewpoint?.ToString(Program.Cumulus.TempFormat, inv) + ", ");
			sb.Append("WindSpeed=" + WindSpeed?.ToString(Program.Cumulus.WindFormat, inv) + ", ");
			sb.Append("WindGust=" + WindGust?.ToString(Program.Cumulus.WindFormat, inv) + ", ");
			sb.Append("WindBearing=" + WindDir + ", ");
			sb.Append("RainfallRate=" + RainRate?.ToString(Program.Cumulus.RainFormat, inv) + ", ");
			sb.Append("RainfallToday=" + RainDay?.ToString(Program.Cumulus.RainFormat, inv) + ", ");
			sb.Append("Baro=" + Baro?.ToString(Program.Cumulus.PressFormat, inv) + ", ");
			sb.Append("RainfallYear=" + RainYear?.ToString(Program.Cumulus.RainFormat, inv));

			return sb.ToString();
		}
	}
}
