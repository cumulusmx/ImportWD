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
			//var fileDict = new SortedTupleBag<int, FileInfo>(); // <year, month>

			var dirInfo = new DirectoryInfo(WdDataPath);

			//var wdFiles = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
			//		.Where(f => FileNamesRegex().IsMatch(f.Name));

			// add valid files to a sorted dictionary
			//foreach (var file in wdFiles)
			//{
			//	var ind = GetYearMonthFromFileName(file.Name);
			//	if (ind > 0)
			//	{
			//		fileDict.Add(ind, file);
			//	}
			//}

			var files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
				//.Select(f => f.Name)
				.Where(name => FileNamesRegex().IsMatch(name.Name))
				.ToList();

			var groupedByMonth = files
				.Select(name =>
				{
					// Extract MMYYYY from the beginning of the filename (1–2 digit month + 4 digit year)
					var match = ExtractMonthYearRegex().Match(name.Name);
					if (!match.Success)
						return null;

					int month = int.Parse(match.Groups[1].Value);
					int year = int.Parse(match.Groups[2].Value);
					return new
					{
						FileName = name,
						Year = year,
						Month = month,
						SortKey = new DateTime(year, month, 1),
						MonthKey = $"{month}{year}"
					};
				})
				.Where(x => x != null)
				.GroupBy(x => x.MonthKey)
				.OrderBy(g => g.First().SortKey);

			LogMessage($"Found {files.Count} log files");
			LogConsole($"Found {files.Count} log files", defConsoleColour);


			foreach (var group in groupedByMonth)
			{
				// first separate the lg file from all the others so we can procress it first

				var monthFiles = group.Select(f => f.FileName).ToList();
				var lgFile = monthFiles.FirstOrDefault(f => f.Name.Equals($"{group.Key}lg.txt", StringComparison.OrdinalIgnoreCase));

				// if monthlog.length > 0 then process it
				if (LogFileRecords.Count > 0)
				{
					WriteLogFile();

					/*
					// WD logs have the first record of the next month as the last record of the previous month
					var logTim = LogFileRecords.Last().Key;
					var logRec = LogFileRecords.Last().Value;
					*/

					// clear the list
					LogFileRecords.Clear();

					/*
					// if the last record is for this next month then add it to the next month
					if (group.Key == logTim.Month.ToString("D2") + logTim.Year.ToString())
					{
						LogFileRecords.Add(logTim, logRec);
					}
					*/
				}


				if (lgFile != null)
				{
					string[] lines;

					LogMessage($"Processing file {lgFile.FullName}...");
					LogConsole($"Processing file {lgFile.Name}...", defConsoleColour);

					try
					{
						lines = File.ReadAllLines(lgFile.FullName);
					}
					catch (Exception ex)
					{
						LogMessage($"Error opening file {lgFile.FullName} - {ex.Message}");
						LogConsole($"Error opening file {lgFile.Name} - {ex.Message}", ConsoleColor.Red);
						LogConsole("Skipping to next file", defConsoleColour);
						// abort this file
						continue;
					}

					CurrMonth = GetMonthFromFileName(lgFile.Name);

					for (var i = 1; i < lines.Length; i++)
					{
						var lg = new WdLgRecord(lines[i], i+1);
						if (lg.Timestamp.HasValue)
						{
							ProcessLgRecord(lg, i+1);
						}
					}

					monthFiles.Remove(lgFile);
				}
				else
				{
					// no Lg file found for this month, so skipping to next month
					LogMessage($"No lg file found for month {group.Key}, skipping to next month");
					continue; // skip processing if no Lg file
				}


				// do all the extra files
				foreach (var file in monthFiles)
				{
					// new month, close off the previous month

					// if extramonthlog.length > 0 then process it
					if (ExtraLogFileRecords.Count > 0)
					{
						WriteExtraLogFile();
						/*
						// WD logs have the first record of the next month as the last record of the previous month
						var logTim = ExtraLogFileRecords.Last().Key;
						var logRec = ExtraLogFileRecords.Last().Value;
						*/

						// clear the list
						ExtraLogFileRecords.Clear();

						/*
						// if the last record is for this next month then add it to the next month
						if (group.Key == logTim.Month.ToString("D2") + logTim.Year)
						{
							ExtraLogFileRecords.Add(logTim, logRec);
						}
						*/
					}

					LogMessage($"Processing file {file.FullName}...");
					LogConsole($"Processing file {file.Name}...", defConsoleColour);

					// determine the log file type
					var logType = GetLogType(file.Name);

					// open and read the file
					string[] lines;

					try
					{
						lines = File.ReadAllLines(file.FullName);
					}
					catch (Exception ex)
					{
						LogMessage($"Error opening file {file.FullName} - {ex.Message}");
						LogConsole($"Error opening file {file.FullName} - {ex.Message}", ConsoleColor.Red);
						LogConsole("Skipping to next file", defConsoleColour);
						// abort this file
						continue;
					}

					// foreach line in the file - skip the first line
					for (var i = 1; i < lines.Length; i++)
					{
						var line = lines[i];
						// process line according to log type
						switch (logType)
						{
							case "vantagelog":
								var van = new WdVantageRecord(line, i + 1);
								if (van.Timestamp.HasValue)
								{
									ProcessVantageRecord(van);
								}
								break;

							case "vantageextrasensorslog":
								var vanex = new WdVantageExtraRecord(line, i + 1);
								if (vanex.Timestamp.HasValue)
								{
									ProcessVantageExtraRecord(vanex);
								}
								break;

							case "extralog":
								var ex = new WdExtraSensorsRecord(line, i + 1);
								if (ex.Timestamp.HasValue)
								{
									ProcessExtraSensorsRecord(ex);
								}
								break;

							case "indoorlog":
								var ind = new WdIndoorRecord(line, i + 1);
								if (ind.Timestamp.HasValue)
								{
									ProcessIndoorRecord(ind);
								}
								break;
						}
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
			if (WdConfigWind == "" || (WdConfigWind != "kph" && WdConfigWind != @"m/s" && WdConfigWind != "mph" && WdConfigWind != "knots"))
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

		private static int GetMonthFromFileName(string fileName)
		{
			var match = ExtractMonthYearRegex().Match(fileName);
			if (match.Success)
			{
				return int.Parse(match.Groups[1].Value);
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

		private static void ProcessLgRecord(WdLgRecord rec, int lineNo)
		{
			LogFileRecord? logRec;

			if (!rec.Timestamp.HasValue)
			{
				return;
			}

			if (!LogFileRecords.ContainsKey(rec.Timestamp ?? DateTime.MinValue))
			{
				if (rec.Timestamp.Value.Month != CurrMonth)
				{
					Program.LogMessage("Skipping record for as it is for the wrong month");
					Program.LogMessage($"Record Date: {rec.Timestamp.Value:yyyy-MM-dd HH:mm}, Current month: {CurrMonth}");
					return; // skip records from the next month if the month has changed
				}

				logRec = new LogFileRecord(rec.Timestamp.Value);
				LogFileRecords.Add(rec.Timestamp.Value, logRec);
				logRec.Temperature = rec.OutsideTemp;
				logRec.Humidity = (int?) rec.OutsideHumidity;
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
			else
			{
				Program.LogMessage($"Line {lineNo}: Duplicate lg file record found for " + (rec.Timestamp ?? DateTime.MinValue).ToString("yyyy-MM-dd HH:mm:ss"));
				Program.LogConsole("Duplicate lg file record found for " + (rec.Timestamp ?? DateTime.MinValue).ToString("yyyy-MM-dd HH:mm:ss"), ConsoleColor.Red);
				var logRecOld = LogFileRecords[rec.Timestamp ?? DateTime.MinValue];
				Program.LogMessage("  Existing record: " + logRecOld.ToString());
				Program.LogMessage("  New WD record : " + rec.ToString());
			}
		}

		private static void ProcessVantageRecord(WdVantageRecord rec)
		{
			LogFileRecord? logRec;

			if (!rec.Timestamp.HasValue)
			{
				return;
			}

			// only update the record if it exists
			if (LogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out logRec))
			{
				logRec.SolarRad = rec.SolarRad;
				logRec.UVI = rec.UVI;
				logRec.ET = rec.ET;
			}

			ExtraLogFileRecord? extraLogRec;

			if (!ExtraLogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out extraLogRec))
			{
				extraLogRec = new ExtraLogFileRecord(rec.Timestamp.Value);
				ExtraLogFileRecords.Add(rec.Timestamp.Value, extraLogRec);
			}

			extraLogRec.SoilMoisture[0] = rec.SoilMoisture;
			extraLogRec.SoilTemp[0] = rec.SoilTemp;
		}

		private static void ProcessVantageExtraRecord(WdVantageExtraRecord rec)
		{
			ExtraLogFileRecord? extraLogRec;

			if (!rec.Timestamp.HasValue || rec.Timestamp.Value.Month != CurrMonth)
			{
				return;
			}

			// if the record does not exist, create it
			if (!ExtraLogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out extraLogRec))
			{
				extraLogRec = new ExtraLogFileRecord(rec.Timestamp.Value);
				ExtraLogFileRecords.Add(rec.Timestamp.Value, extraLogRec);
			}

			// update the existing record
			for (var i = 0; i < 8; i++)
			{
				extraLogRec.Temperature[i + 1] = rec.Temp[i];
				extraLogRec.Humidity[i + 1] = (int?) rec.Hum[i];

				if (rec.Temp[i].HasValue && rec.Hum[i].HasValue)
				{
					// calculate the dew point
					extraLogRec.Dewpoint[i + 1] = ConvertUnits.TempCToUser(MeteoLib.DewPoint(ConvertUnits.UserTempToC(rec.Temp[i].Value), rec.Hum[i].Value));
				}
			}
		}


		private static void ProcessExtraSensorsRecord(WdExtraSensorsRecord rec)
		{
			ExtraLogFileRecord? extraLogRec;

			if (!rec.Timestamp.HasValue || rec.Timestamp.Value.Month != CurrMonth)
			{
				return;
			}

			// if the record does not exist, create it
			if (ExtraLogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out extraLogRec))
			{
				extraLogRec = new ExtraLogFileRecord(rec.Timestamp.Value);
				ExtraLogFileRecords.Add(rec.Timestamp.Value, extraLogRec);
			}

			// update the existing record
			for (var i = 0; i < 9; i++)
			{
				extraLogRec.Temperature[i] = rec.Temp[i];
				extraLogRec.Humidity[i] = (int?) rec.Hum[i];

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

			if (!rec.Timestamp.HasValue)
			{
				return;
			}

			if (LogFileRecords.TryGetValue(rec.Timestamp ?? DateTime.MinValue, out logRec))
			{
				logRec.InsideTemp = rec.Temp;
				logRec.InsideHum = rec.Hum;
			}
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
