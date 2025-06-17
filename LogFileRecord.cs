using System.Globalization;
using System.Text;


namespace ImportWD
{
	class LogFileRecord(DateTime logTime)
	{
		public DateTime LogTime { get; set; } = logTime;
		public double? Temperature { get; set; }
		public int? Humidity { get; set; }
		public double? Dewpoint { get; set; }
		public double? WindSpeed { get; set; }
		public double? WindGust { get; set; }
		public int? WindBearing { get; set; }
		public double? RainfallRate { get; set; }
		public double? RainfallToday { get; set; }
		public double? Baro { get; set; }
		public double? RainfallCounter { get; set; }
		public double? InsideTemp { get; set; }
		public double? InsideHum { get; set; }
		public double? CurrentGust { get; set; }
		public double? WindChill { get; set; }
		public double? HeatIndex { get; set; }
		public double? UVI { get; set; }
		public int? SolarRad { get; set; }
		public double? ET { get; set; }
		public double? AnnualET { get; set; }
		public double? ApparentTemp { get; set; }
		public int? SolarMax { get; set; }
		public double? SunshineHours { get; set; }
		public int? CurrentBearing { get; set; }
		public double? RG11Rain { get; set; }
		public double? RainSinceMidnight { get; set; }
		public double? FeelsLike { get; set; }
		public double? Humidex { get; set; }

		public string RecToCsv()
		{
			// Writes an entry to the n-minute log file. Fields are comma-separated:
			// 0  Date in the form dd/mm/yy (the slash may be replaced by a dash in some cases)
			// 1  Current time - hh:mm
			// 2  Current temperature
			// 3  Current humidity
			// 4  Current dewpoint
			// 5  Current wind speed
			// 6  Recent (10-minute) high gust
			// 7  Average wind bearing
			// 8  Current rainfall rate
			// 9  Total rainfall today so far
			// 10  Current sea level pressure
			// 11  Total rainfall counter as held by the station
			// 12  Inside temperature
			// 13  Inside humidity
			// 14  Current gust (i.e. 'Latest')
			// 15  Wind chill
			// 16  Heat Index
			// 17  UV Index
			// 18  Solar Radiation
			// 19  Evapotranspiration
			// 20  Annual Evapotranspiration
			// 21  Apparent temperature
			// 22  Current theoretical max solar radiation
			// 23  Hours of sunshine so far today
			// 24  Current wind bearing
			// 25  RG-11 rain total
			// 26  Rain since midnight
			// 27  Feels like
			// 28  Humidex


			Program.LogDebugMessage("DoLogFile: Writing log entry for " + LogTime);
			var inv = CultureInfo.InvariantCulture;
			var sep = ",";

			var sb = new StringBuilder(256);
			sb.Append(LogTime.ToString("dd/MM/yy", inv) + sep);
			sb.Append(LogTime.ToString("HH:mm", inv) + sep);
			if (Temperature.HasValue)
				sb.Append(Temperature.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing temperature");

			if (Humidity.HasValue)
				sb.Append(Humidity.Value.ToString() + sep);
			else
				throw new MissingFieldException("Missing humidity");

			if (Dewpoint.HasValue)
				sb.Append(Dewpoint.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing dewpoint");

			if (WindSpeed.HasValue)
				sb.Append(WindSpeed.Value.ToString(Program.Cumulus.WindAvgFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing wind speed");

			if (WindGust.HasValue)
				sb.Append(WindGust.Value.ToString(Program.Cumulus.WindAvgFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing wind gust");

			if (WindBearing.HasValue)
				sb.Append(WindBearing.Value.ToString() + sep);
			else
				throw new MissingFieldException("Missing wind bearing");

			if (RainfallRate.HasValue)
				sb.Append(RainfallRate.Value.ToString(Program.Cumulus.RainFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing rainfall rate");

			if (RainfallToday.HasValue)
				sb.Append(RainfallToday.Value.ToString(Program.Cumulus.RainFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing rainfall today");

			if (Baro.HasValue)
				sb.Append(Baro.Value.ToString(Program.Cumulus.PressFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing pressure");

			if (RainfallCounter.HasValue)
				sb.Append(RainfallCounter.Value.ToString(Program.Cumulus.RainFormat, inv) + sep);
			else
				throw new MissingFieldException("Missing rainfall counter");

			if (InsideTemp.HasValue)
				sb.Append(InsideTemp.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				sb.Append(sep);

			if (InsideHum.HasValue)
				sb.Append(InsideHum.Value.ToString() + sep);
			else
				sb.Append(sep);

			//if (CurrentGust.HasValue) // no current gust value in WD logs, use the Gust value
			if (WindGust.HasValue)
				sb.Append(WindGust.Value.ToString(Program.Cumulus.WindFormat, inv) + sep);
			else
				sb.Append(sep);

			if (WindChill.HasValue)
				sb.Append(WindChill.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				sb.Append(sep);

			if (HeatIndex.HasValue)
				sb.Append(HeatIndex.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				sb.Append(sep);

			if (UVI.HasValue)
				sb.Append(UVI.Value.ToString(Program.Cumulus.UVFormat, inv) + sep);
			else
				sb.Append(sep);

			if (SolarRad.HasValue)
				sb.Append(SolarRad.Value.ToString() + sep);
			else
				sb.Append(sep);

			if (ET.HasValue)
				sb.Append(ET.Value.ToString(Program.Cumulus.ETFormat, inv) + sep);
			else
				sb.Append(sep);

			if (AnnualET.HasValue)
				sb.Append(AnnualET.Value.ToString(Program.Cumulus.ETFormat, inv) + sep);
			else
				sb.Append(sep);

			if (ApparentTemp.HasValue)
				sb.Append(ApparentTemp.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				sb.Append(sep);

			if (SolarMax.HasValue)
				sb.Append(SolarMax.Value.ToString() + sep);
			else
				sb.Append(sep);

			if (SunshineHours.HasValue)
				sb.Append(SunshineHours.Value.ToString(Program.Cumulus.SunFormat, inv) + sep);
			else
				sb.Append(sep);

			if (WindBearing.HasValue)
				sb.Append(WindBearing.Value.ToString() + sep);
			else
				sb.Append(sep);

			if (RG11Rain.HasValue)
				sb.Append(RG11Rain.Value.ToString(Program.Cumulus.RainFormat, inv) + sep);
			else
				sb.Append(sep);

			if (RainSinceMidnight.HasValue)
				sb.Append(RainSinceMidnight.Value.ToString(Program.Cumulus.RainFormat, inv) + sep);
			else
				sb.Append(sep);

			if (FeelsLike.HasValue)
				sb.Append(FeelsLike.Value.ToString(Program.Cumulus.TempFormat, inv) + sep);
			else
				sb.Append(sep);

			if (Humidex.HasValue)
				sb.Append(Humidex.Value.ToString(Program.Cumulus.TempFormat, inv));

			return sb.ToString();
		}

		public override string ToString()
		{
			var inv = CultureInfo.InvariantCulture;
			var sb = new StringBuilder(256);
			sb.Append("LogFileRecord: ");
			sb.Append("LogTime=" + LogTime.ToString("yyyy-MM-dd HH:mm:ss", inv) + ", ");
			sb.Append("Temperature=" + Temperature?.ToString(Program.Cumulus.TempFormat, inv) + ", ");
			sb.Append("Humidity=" + Humidity?.ToString() + ", ");
			sb.Append("Dewpoint=" + Dewpoint?.ToString(Program.Cumulus.TempFormat, inv) + ", ");
			sb.Append("WindSpeed=" + WindSpeed?.ToString(Program.Cumulus.WindAvgFormat, inv) + ", ");
			sb.Append("WindGust=" + WindGust?.ToString(Program.Cumulus.WindAvgFormat, inv) + ", ");
			sb.Append("WindBearing=" + WindBearing?.ToString() + ", ");
			sb.Append("RainfallRate=" + RainfallRate?.ToString(Program.Cumulus.RainFormat, inv) + ", ");
			sb.Append("RainfallToday=" + RainfallToday?.ToString(Program.Cumulus.RainFormat, inv) + ", ");
			sb.Append("Baro=" + Baro?.ToString(Program.Cumulus.PressFormat, inv) + ", ");
			sb.Append("RainfallCounter=" + RainfallCounter?.ToString(Program.Cumulus.RainFormat, inv) + ", ");

			return sb.ToString();
		}
	}
}
