using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ImportWD
{
	static partial class Program
	{
		public static Cumulus Cumulus { get; set; } = new Cumulus();
		public static string Location { get; set; } = string.Empty;

		private static ConsoleColor defConsoleColour;

		public static string WdDataPath { get; set; } = string.Empty;
		public static string WdConfigTemp { get; set; } = string.Empty;
		public static string WdConfigWind { get; set; } = string.Empty;
		public static string WdConfigPress { get; set; } = string.Empty;
		public static string WdConfigRain { get; set; } = string.Empty;


		private static readonly SortedList<DateTime, LogFileRecord> LogFileRecords = [];
		private static readonly SortedList<DateTime, ExtraLogFileRecord> ExtraLogFileRecords = [];

		private static int CurrMonth;
		private static int CurrYear;

		static void Main()
		{
			// Tell the user what is happening

			TextWriterTraceListener myTextListener = new TextWriterTraceListener($"MXdiags{Path.DirectorySeparatorChar}ImportWD-{DateTime.Now:yyyyMMdd-HHmmss}.txt", "WDlog");
			Trace.Listeners.Add(myTextListener);
			Trace.AutoFlush = true;

			defConsoleColour = Console.ForegroundColor;

			var fullVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
			var version = $"{fullVer.Major}.{fullVer.Minor}.{fullVer.Build}";
			LogMessage("ImportWD v." + version);
			Console.WriteLine("ImportWD v." + version);

			// Read the config file
			ReadWeatherDisplayConfig();

			LogMessage("Processing started");
			Console.WriteLine();
			Console.WriteLine($"Processing started: {DateTime.Now:U}");
			Console.WriteLine();

			// get the location of the exe - we will assume this is in the Cumulus root folder
			Location = AppDomain.CurrentDomain.BaseDirectory;

			// Read the Cumulus.ini file
			Cumulus = new Cumulus();

			// Check meteo day
			if (Cumulus.RolloverHour != 0)
			{
				LogMessage("Cumulus is not configured for a midnight rollover, so Import cannot create any day file entries");
				LogConsole("Cumulus is not configured for a midnight rollover, so no day file entries will be created", ConsoleColor.DarkYellow);
				LogConsole("You must run CreateMissing after this Import to create the day file entries", ConsoleColor.DarkYellow);
			}
			else
			{
				LogMessage("Cumulus is configured for a midnight rollover, Import will create day file entries");
				LogConsole("Cumulus is configured for a midnight rollover, so day file entries will be created", ConsoleColor.Cyan);
				LogConsole("You must still run CreateMissing after this Import to add missing details to those day file entries", ConsoleColor.Cyan);
			}
			Console.WriteLine();

			// Find all the WD log files
			// naming convention YYYY-MM.wlk, eg 2024-05.wlk
			LogMessage("Searching for data files");
			Console.WriteLine("Searching for data log files...");

			if (!Directory.Exists(WdDataPath))
			{
				LogMessage($"The source directory '{WdDataPath}' does not exist, aborting");
				LogConsole($"The source directory '{WdDataPath}' does not exist, aborting", ConsoleColor.Red);
				Environment.Exit(1);
			}

			// Weather Display monthly log files have a patterns of:

			// MMYYYYlg.txt - general
			// MMYYYYvantagelog.txt - solar & soil
			// MMYYYYvantageextrasensorslog.txt - extra temp & humidity
			// MMYYYYextralog.csv - extra sensors
			// MMYYYYlg2.txt - temperature highs/lows
			// MMYYYYlgsun.txt - sunshine hours
			// MMYYYindoorlog.txt - indoor values
			// MMYYYY1wirelog.txt - 1-wire sensors ???
			// MMYYYYextralog.csv - extra sensors

			// BUT the month is a single digit for months 1-9, so to sort them we need to extract the month/year and swap them around, and also pad the month to two digits

			//var fileDict = new SortedDictionary<int, FileInfo>(); // <year, month>
			var fileDict = new SortedTupleBag<int, FileInfo>(); // <year, month>

			var dirInfo = new DirectoryInfo(WdDataPath);
			var wdFiles = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
					.Where(f => FileNamesRegex().IsMatch(f.Name));

			// add valid files to a sorted dictionary
			foreach (var file in wdFiles)
			{
				var ind = GetYearMonthFromFileName(file.Name);
				if( ind > 0)
					fileDict.Add(ind, file);
			}

			LogMessage($"Found {fileDict.Count} log files");
			LogConsole($"Found {fileDict.Count} log files", defConsoleColour);

			int lastyearmonth = 0;

			foreach (var file in fileDict)
			{
				if (lastyearmonth != file.Item1)
				{
					if (lastyearmonth == 0)
					{
						// starting up
						lastyearmonth = file.Item1;
						CurrMonth = file.Item1 % 100;
						CurrYear = file.Item1 / 100;
					}
					else
					{
						// new month, close off the previous month

						// if monthlog.length > 0 then process it
						if (LogFileRecords.Count > 0)
						{
							WriteLogFile();

							// WD logs have the first record of the next month as the last record of the previous month
							var logTim = LogFileRecords.Last().Key;
							var logRec = LogFileRecords.Last().Value;

							// clear the list
							LogFileRecords.Clear();

							// if the last record is for this next month then add it to the next month
							if (file.Item1 == logTim.Year * 100 + logTim.Month)
							{
								LogFileRecords.Add(logTim, logRec);
							}
						}

						// if extramonthlog.length > 0 then process it
						if (ExtraLogFileRecords.Count > 0)
						{
							WriteExtraLogFile();

							// WD logs have the first record of the next month as the last record of the previous month
							var logTim = ExtraLogFileRecords.Last().Key;
							var logRec = ExtraLogFileRecords.Last().Value;

							// clear the list
							ExtraLogFileRecords.Clear();

							// if the last record is for this next month then add it to the next month
							if (file.Item1 == logTim.Year * 100 + logTim.Month)
							{
								ExtraLogFileRecords.Add(logTim, logRec);
							}
						}

						lastyearmonth = file.Item1;
						CurrMonth = file.Item1 % 100;
						CurrYear = file.Item1 / 100;
					}
				}

				LogMessage($"Processing file {file.Item2.Name}...");
				LogConsole($"Processing file {file.Item2.Name}...", defConsoleColour);

				// determine the log file type
				var logType = GetLogType(file.Item2.Name);

				// open and read the file
				string[] lines;

				try
				{
					lines = File.ReadAllLines(file.Item2.FullName);
				}
				catch (Exception ex)
				{
					LogMessage($"Error opening file {file.Item2.FullName} - {ex.Message}");
					LogConsole($"Error opening file {file.Item2.FullName} - {ex.Message}", ConsoleColor.Red);
					LogConsole("Skipping to next file", defConsoleColour);
					// abort this file
					continue;
				}

				// foreach line in the file - skip the first line
				foreach (var line in lines[1..])
				{
					// process line according to log type
					switch (logType)
					{
						case "lg":
							var lg = new WdLgRecord(line);
							if (lg.Timestamp.HasValue)
							{
								ProcessLgRecord(lg);
							}
							break;

						case "vantagelog":
							var van = new WdVantageRecord(line);
							if (van.Timestamp.HasValue)
							{
								ProcessVantageRecord(van);
							}
							break;

						case "vantageextrasensorslog":
							var vanex = new WdVantageExtraRecord(line);
							if (vanex.Timestamp.HasValue)
							{
								ProcessVantageExtraRecord(vanex);
							}
							break;

						case "extralog":
							var ex = new WdExtraSensorsRecord(line);
							if (ex.Timestamp.HasValue)
							{
								ProcessExtraSensorsRecord(ex);
							}
							break;

						case "indoorlog":
							var ind = new WdIndoorRecord(line);
							if (ind.Timestamp.HasValue)
							{
								ProcessIndoorRecord(ind);
							}
							break;
					}
				}
			}

			// if monthlog.length > 0 then process it
			if (LogFileRecords.Count > 0)
			{
				WriteLogFile();

				// clear the list
				LogFileRecords.Clear();
			}

			// if extramonthlog.length > 0 then process it
			if (ExtraLogFileRecords.Count > 0)
			{
				WriteExtraLogFile();

				// clear the list
				ExtraLogFileRecords.Clear();
			}
		}



		public static void LogMessage(string message)
		{
			Trace.TraceInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
		}

		public static void LogDebugMessage(string message)
		{
#if DEBUG
			//Trace.TraceInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message)
#endif
		}

		public static void LogConsole(string msg, ConsoleColor colour, bool newLine = true)
		{
			Console.ForegroundColor = colour;

			if (newLine)
			{
				Console.WriteLine(msg);
			}
			else
			{
				Console.Write(msg);
			}

			Console.ForegroundColor = defConsoleColour;
		}


		private static void ReadWeatherDisplayConfig()
		{
			if (!System.IO.File.Exists(Program.Location + "wd_config.ini"))
			{
				Program.LogMessage("Failed to find wd_config.ini file!");
				Console.WriteLine("Failed to find wd_config.ini file!");
				Environment.Exit(1);
			}

			Program.LogMessage("Reading wd_config.ini file");

			IniFile ini = new IniFile("wd_config.ini");

			WdDataPath = ini.GetValue("data", "path", "");
			if (WdDataPath == "")
			{
				Program.LogMessage("Failed to find data path in wd_config.ini");
				Console.WriteLine("Failed to findcdata path in wd_config.ini");
				Environment.Exit(1);
			}

			WdConfigTemp = ini.GetValue("units", "temperature", "").ToLower();
			if (WdConfigTemp == "" || (WdConfigTemp != "c" && WdConfigTemp != "f"))
			{
				Program.LogMessage("Failed to find temperature units in wd_config.ini");
				Console.WriteLine("Failed to find temperature units in wd_config.ini");
				Environment.Exit(1);
			}

			WdConfigPress = ini.GetValue("units", "pressure", "").ToLower();
			if (WdConfigPress == "" || (WdConfigPress != "inhg" && WdConfigPress != "mb" && WdConfigPress != "hpa"))
			{
				Program.LogMessage("Failed to find pressure units in wd_config.ini");
				Console.WriteLine("Failed to find pressure units in wd_config.ini");
				Environment.Exit(1);
			}

			WdConfigWind = ini.GetValue("units", "wind", "");
			if (WdConfigWind == "" || (WdConfigWind != "kph" && WdConfigWind != "mps" && WdConfigWind != "mph" && WdConfigWind != "knots"))
			{
				Program.LogMessage("Failed to find wind units in wd_config.ini");
				Console.WriteLine("Failed to find wind units in wd_config.ini");
				Environment.Exit(1);
			}

			WdConfigRain = ini.GetValue("units", "rain", "");
			if (WdConfigRain == "" || (WdConfigRain != "mm" && WdConfigRain != "in"))
			{
				Program.LogMessage("Failed to find rain units in wd_config.ini");
				Console.WriteLine("Failed to find rain units in wd_config.ini");
				Environment.Exit(1);
			}
		}


		private static int GetYearMonthFromFileName(string fileName)
		{
			var match = ExtractMonthYearRegex().Match(fileName);
			if (match.Success)
			{
				var year = int.Parse(match.Groups[2].Value);
				var month = int.Parse(match.Groups[1].Value);
				return year * 100 + month;
			}
			return 0;
		}

		private static string GetLogType(string fileName)
		{
			string pattern = @"^\d{5,6}(\w+)";
			Match match = Regex.Match(fileName, pattern);
			if (match.Success)
			{
				return match.Groups[1].Value;
			}
			return "";
		}

		private static void ProcessLgRecord(WdLgRecord rec)
		{
			LogFileRecord? logRec;

			if (!LogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out logRec))
			{
				if (!rec.Timestamp.HasValue)
					return;

				if (rec.Timestamp.Value.Month != CurrMonth && CurrMonth == 12 && rec.Timestamp.Value.Month == 1 && CurrYear == rec.Timestamp.Value.Year)
				{
					rec.Timestamp = new DateTime(CurrYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local);
				}

				logRec = new LogFileRecord(rec.Timestamp.Value);
				LogFileRecords.Add(rec.Timestamp.Value, logRec);
			}

			logRec.Temperature = rec.OutsideTemp;
			logRec.Humidity = rec.OutsideHumidity;
			logRec.Dewpoint = rec.Dewpoint;
			logRec.Baro = rec.Baro;
			logRec.WindSpeed = rec.WindSpeed;
			logRec.WindGust = rec.WindGust;
			logRec.WindBearing = rec.WindDir;
			logRec.Baro = rec.Baro;
			logRec.RainfallRate = rec.RainRate;
			logRec.RainfallToday = rec.RainDay;
			logRec.RainfallCounter = rec.RainYear;
			logRec.HeatIndex = rec.HeatIndex;
		}

		private static void ProcessVantageRecord(WdVantageRecord rec)
		{
			LogFileRecord? logRec;

			if (!LogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out logRec))
			{
				if (!rec.Timestamp.HasValue)
					return;

				if (rec.Timestamp.Value.Month != CurrMonth && CurrMonth == 12 && rec.Timestamp.Value.Month == 1 && CurrYear == rec.Timestamp.Value.Year)
				{
					rec.Timestamp = new DateTime(CurrYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local);
				}

				logRec = new LogFileRecord(rec.Timestamp.Value);
				LogFileRecords.Add(rec.Timestamp.Value, logRec);
			}

			logRec.SolarRad = rec.SolarRad;
			logRec.UVI = rec.UVI;
			logRec.ET = rec.ET;

			ExtraLogFileRecord? extraLogRec;

			if (!ExtraLogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out extraLogRec))
			{
				if (!rec.Timestamp.HasValue)
				{
					return;
				}

				extraLogRec = new ExtraLogFileRecord(rec.Timestamp.Value);
				ExtraLogFileRecords.Add(rec.Timestamp.Value, extraLogRec);
			}

			extraLogRec.SoilMoisture[0] = rec.SoilMoisture;
			extraLogRec.SoilTemp[0] = rec.SoilTemp;
		}

		private static void ProcessVantageExtraRecord(WdVantageExtraRecord rec)
		{
			ExtraLogFileRecord? extraLogRec;

			if (!ExtraLogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out extraLogRec))
			{
				if (!rec.Timestamp.HasValue)
				{
					return;
				}

				if (rec.Timestamp.Value.Month != CurrMonth && CurrMonth == 12 && rec.Timestamp.Value.Month == 1 && CurrYear == rec.Timestamp.Value.Year)
				{
					rec.Timestamp = new DateTime(CurrYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local);
				}

				extraLogRec = new ExtraLogFileRecord(rec.Timestamp.Value);
				ExtraLogFileRecords.Add(rec.Timestamp.Value, extraLogRec);
			}

			for (var i = 0; i < 8; i++)
			{
				extraLogRec.Temperature[i+1] = rec.Temp[i];
				extraLogRec.Humidity[i+1] = rec.Hum[i];

				if (rec.Temp[i].HasValue && rec.Hum[i].HasValue)
				{
					// calculate the dew point
					extraLogRec.Dewpoint[i+1] = ConvertUnits.TempCToUser(MeteoLib.DewPoint(ConvertUnits.UserTempToC(rec.Temp[i].Value), rec.Hum[i].Value));
				}
			}
		}


		private static void ProcessExtraSensorsRecord(WdExtraSensorsRecord rec)
		{
			ExtraLogFileRecord? extraLogRec;

			if (!ExtraLogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out extraLogRec))
			{
				if (!rec.Timestamp.HasValue)
				{
					return;
				}

				if (rec.Timestamp.Value.Month != CurrMonth && CurrMonth == 12 && rec.Timestamp.Value.Month == 1 && CurrYear == rec.Timestamp.Value.Year)
				{
					rec.Timestamp = new DateTime(CurrYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Local);
				}

				extraLogRec = new ExtraLogFileRecord(rec.Timestamp.Value);
				ExtraLogFileRecords.Add(rec.Timestamp.Value, extraLogRec);
			}

			for (var i = 0; i < 9; i++)
			{
				extraLogRec.Temperature[i] = rec.Temp[i];
				extraLogRec.Humidity[i] = rec.Hum[i];

				if (rec.Temp[i].HasValue && rec.Hum[i].HasValue)
				{
					// calculate the dew point
					extraLogRec.Dewpoint[i] = ConvertUnits.TempCToUser(MeteoLib.DewPoint(ConvertUnits.UserTempToC(rec.Temp[i].Value), rec.Hum[i].Value));
				}
			}
		}


		private static void ProcessIndoorRecord(WdIndoorRecord rec)
		{
			LogFileRecord? logRec;

			if (!LogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out logRec))
			{
				if (!rec.Timestamp.HasValue)
				{
					return;
				}

				if (rec.Timestamp.Value.Month != CurrMonth && CurrMonth == 12 && rec.Timestamp.Value.Month == 1 && CurrYear == rec.Timestamp.Value.Year)
				{
					rec.Timestamp = new DateTime(CurrYear + 1, 1, 1, 0,	0, 0, DateTimeKind.Local);
				}

				logRec = new LogFileRecord(rec.Timestamp.Value);
				LogFileRecords.Add(rec.Timestamp.Value, logRec);
			}

			logRec.InsideTemp = rec.Temp;
			logRec.InsideHum = rec.Hum;
		}

		private static void WriteLogFile()
		{
			if (LogFileRecords.First().Key.Month != LogFileRecords.GetKeyAtIndex(1).Month)
			{
				LogFileRecords.Remove(LogFileRecords.First().Key);
			}

			var logfilename = "data" + Path.DirectorySeparatorChar + Cumulus.GetLogFileName(LogFileRecords.First().Key);
			var currYear = LogFileRecords.First().Key.Year;
			var currMonth = LogFileRecords.First().Key.Month;


			Program.LogMessage($"Writing {LogFileRecords.Count} records to {logfilename}");
			Program.LogConsole($"  Writing to {logfilename}", ConsoleColor.Gray);

			// backup old log file
			if (File.Exists(logfilename))
			{
				if (!File.Exists(logfilename + ".sav"))
				{
					File.Move(logfilename, logfilename + ".sav");
				}
				else
				{
					var i = 1;
					do
					{
						if (!File.Exists(logfilename + ".sav" + i))
						{
							File.Move(logfilename, logfilename + ".sav" + i);
							break;
						}
						else
						{
							i++;
						}
					} while (true);
				}
			}


			try
			{
				using FileStream fs = new FileStream(logfilename, FileMode.Append, FileAccess.Write, FileShare.Read);
				using StreamWriter file = new StreamWriter(fs);
				Program.LogMessage($"{logfilename} opened for writing {LogFileRecords.Count} records");

				foreach (var rec in LogFileRecords)
				{
					try
					{
						if (rec.Key.Month == currMonth && rec.Key.Year == currYear)
						{
							var line = rec.Value.RecToCsv();
							if (!string.IsNullOrEmpty(line))
							{
								file.WriteLine(line);
							}
						}
					}
					catch (Exception ex)
					{
						Program.LogMessage($"Error writing to {logfilename}: {rec.Key} - {ex.Message}");
					}
				}

				file.Close();
				Program.LogMessage($"{logfilename} write complete");
			}
			catch (Exception ex)
			{
				Program.LogMessage($"Error writing to {logfilename}: {ex.Message}");
			}
		}

		private static void WriteExtraLogFile()
		{
			if (ExtraLogFileRecords.First().Key.Month != ExtraLogFileRecords.GetKeyAtIndex(1).Month)
			{
				ExtraLogFileRecords.Remove(ExtraLogFileRecords.First().Key);
			}

			var logfilename = "data" + Path.DirectorySeparatorChar + Cumulus.GetExtraLogFileName(ExtraLogFileRecords.First().Key);
			Program.LogMessage($"Writing {ExtraLogFileRecords.Count} records to {logfilename}");
			Program.LogConsole($"  Writing to {logfilename}", ConsoleColor.Gray);

			// backup old logfile
			if (File.Exists(logfilename))
			{
				if (!File.Exists(logfilename + ".sav"))
				{
					File.Move(logfilename, logfilename + ".sav");
				}
				else
				{
					var i = 1;
					do
					{
						if (!File.Exists(logfilename + ".sav" + i))
						{
							File.Move(logfilename, logfilename + ".sav" + i);
							break;
						}
						else
						{
							i++;
						}
					} while (true);
				}
			}


			try
			{
				using FileStream fs = new FileStream(logfilename, FileMode.Append, FileAccess.Write, FileShare.Read);
				using StreamWriter file = new StreamWriter(fs);
				Program.LogMessage($"{logfilename} opened for writing {ExtraLogFileRecords.Count} records");

				foreach (var rec in ExtraLogFileRecords)
				{
					var line = rec.Value.RecToCsv();
					if (!string.IsNullOrEmpty(line))
					{
						file.WriteLine(line);
					}
				}

				file.Close();
				Program.LogMessage($"{logfilename} write complete");
			}
			catch (Exception ex)
			{
				Program.LogMessage($"Error writing to {logfilename}: {ex.Message}");
			}
		}


		[GeneratedRegex(@"^\d{5,6}((lg|vantagelog|vantageextrasensorslog|indoorlog)\.txt$|extralog\.csv$)")]
		private static partial Regex FileNamesRegex();

		[GeneratedRegex(@"(\d{1,2})(\d{4})")]
		private static partial Regex ExtractMonthYearRegex();
	}
}
