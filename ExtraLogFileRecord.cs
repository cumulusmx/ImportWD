using System.Globalization;
using System.Text;


namespace ImportWD
{
	class ExtraLogFileRecord(DateTime logTime)
	{

		public DateTime LogTime { get; set; } = logTime;
		public double?[] Temperature { get; set; } = [null, null, null, null, null, null, null, null, null, null];
		public int?[] Humidity { get; set; } = [null, null, null, null, null, null, null, null, null, null];
		public double?[] Dewpoint { get; set; } = [null, null, null, null, null, null, null, null, null, null];
		public double?[] SoilTemp { get; set; } = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
		public int?[] SoilMoisture { get; set; } = [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null];
		public double?[] LeafTemp { get; set; } = [null, null];
		public int?[] LeafWetness { get; set; } = [null, null];


		public string RecToCsv()
		{
			// Writes an entry to the n-minute extralogfile. Fields are comma-separated:
			// 0  Date in the form dd/mm/yy (the slash may be replaced by a dash in some cases)
			// 1  Current time - hh:mm
			// 2-11  Temperature 1-10
			// 12-21 Humidity 1-10
			// 22-31 Dew point 1-10
			// 32-35 Soil temp 1-4
			// 36-39 Soil moisture 1-4
			// 40-41 Leaf temp 1-2
			// 42-43 Leaf wetness 1-2
			// 44-55 Soil temp 5-16
			// 56-67 Soil moisture 5-16
			// 68-71 Air quality 1-4
			// 72-75 Air quality avg 1-4
			// 76-83 User temperature 1-8
			// 84  CO2
			// 85  CO2 avg
			// 86  CO2 pm2.5
			// 87  CO2 pm2.5 avg
			// 88  CO2 pm10
			// 89  CO2 pm10 avg
			// 90  CO2 temp
			// 91  CO2 hum

			Program.LogDebugMessage("DoExtraLogFile: Writing log entry for " + LogTime);
			var inv = CultureInfo.InvariantCulture;
			var sep = ',';

			var sb = new StringBuilder(256);
			sb.Append(LogTime.ToString("dd/MM/yy", inv) + sep);
			sb.Append(LogTime.ToString("HH:mm", inv) + sep);
			// Extra Temp 1-10
			for (int i = 0; i < 10; i++)
			{
				sb.Append((Temperature[i] ?? 0).ToString(Program.Cumulus.TempFormat, inv) + sep);
			}
			// Extra Hum 1-10
			for (int i = 0; i < 10; i++)
			{
				sb.Append((Humidity[i] ?? 0).ToString() + sep);
			}
			// Extra Dewpoint 1-10
			for (int i = 0; i < 10; i++)
			{
				sb.Append((Dewpoint[i] ?? 0).ToString(Program.Cumulus.TempFormat, inv) + sep);
			}
			// Extra Soil Temp 1-4
			for (int i = 0; i < 4; i++)
			{
				sb.Append((SoilTemp[i] ?? 0).ToString(Program.Cumulus.TempFormat, inv) + sep);
			}
			// Extra Soil Moisture 1-4
			for (int i = 0; i < 4; i++)
			{
				sb.Append((SoilMoisture[i] ?? 0).ToString() + sep);
			}
			// Leaf temp - not used
			sb.Append("0,0,0,0,");
			// Extra Leaf wetness 1-2
			sb.Append((LeafWetness[0] ?? 0).ToString() + sep);
			sb.Append((LeafWetness[1] ?? 0).ToString() + sep);
			// Soil Temp 5-16
			for (int i = 4; i < 16; i++)
			{
				sb.Append((SoilTemp[i] ?? 0).ToString(Program.Cumulus.TempFormat, inv) + sep);
			}
			// Soil Moisture 5-16
			for (int i = 4; i < 16; i++)
			{
				sb.Append((SoilMoisture[i] ?? 0).ToString() + sep);
			}
			// Air quality 1-4
			for (int i = 0; i < 4; i++)
			{
				sb.Append("0" + sep);
			}
			// Air quality avg 1-4
			for (int i = 0; i < 4; i++)
			{
				sb.Append("0" + sep);
			}
			// User temp 1-8
			for (int i = 0; i < 8; i++)
			{
				sb.Append("0" + sep);
			}
			// CO2
			sb.Append("0,0,0,0,0,0,0,0");

			return sb.ToString();
		}
	}
}
