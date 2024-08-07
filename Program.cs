using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ImportWD
{
	static partial class Program
	{
		public static Cumulus Cumulus { get; set; }
		public static string Location { get; set; }

		private static ConsoleColor defConsoleColour;

		public static string WdDataPath { get; set; }
		public static string WdConfigTemp { get; set; }
		public static string WdConfigWind { get; set; }
		public static string WdConfigPress { get; set; }
		public static string WdConfigRain { get; set; }

		static void Main(string[] args)
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

			// Weather Display log files have a patterns of MMYYYYlg.txt, MMYYYYvantagelog.txt, MMYYYYvantageextrasensorslog.txt
			// BUT the month is a single digit for months 1-9, so to sort them we need to extract the mont/year and swap them around, and also pad the month to two digits

			var dirInfo = new DirectoryInfo(WdDataPath);
			var wdFiles = dirInfo.EnumerateFiles("*.txt", SearchOption.AllDirectories)
					.Where(f => Regex.Match(f.Name, @"[0-9]{5,6}lg\.txt|[0-9]{5,6}vantagelog\.txt|[0-9]{5,6}vantageextrasensorslog\.txt").Success)
					.OrderBy(f => GetYearMonthFromFileName(f.Name)) // Swap MonthYear portion
					.ToList();

			LogMessage($"Found {wdFiles.Count} log files");
			LogConsole($"Found {wdFiles.Count} log files", defConsoleColour);


		}



		public static void LogMessage(string message)
		{
			Trace.TraceInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
		}

		public static void LogDebugMessage(string message)
		{
#if DEBUG
			Trace.TraceInformation(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message);
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

		private static string GetYearMonthFromFileName(string fileName)
		{
			// Assuming the format is MonthYearSomeText.extension
			string pattern = @"$(\d{1,2})(\d{4})"; // Matches MonthYear
			Match match = Regex.Match(fileName, pattern);
			if (match.Success)
			{
				return match.Groups[1].Value  + (match.Groups[0].Length == 1 ? "0" + match.Groups[0].Value : match.Groups[0].Value);
			}
			else
			{
				return string.Empty;
			}
		}
	}
}
