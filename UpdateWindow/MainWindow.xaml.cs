﻿using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace UpdateWindow
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string logFileName;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			logFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logfile.txt");
			try
			{
				var args = Environment.GetCommandLineArgs();
				if(3 != args.Length)
				{
					Log($"Usage: {nameof(UpdateWindow)} <{nameof(Options.UpdateDataArchive)}> <{nameof(Options.ApplicationDir)}>");
					return;
				}
				var options = new Options { UpdateDataArchive = args[1], ApplicationDir = args[2] };
				Update(options);
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
		}

		private void Log(string message)
		{
			var time = DateTime.Now.ToLongTimeString();
			var entry = $"{time}: {message}{Environment.NewLine}";
			File.AppendAllText(logFileName, entry);
			log.AppendText(entry);
		}

		private void Update(Options options)
		{
			if (!File.Exists(options.UpdateDataArchive)) throw new FileNotFoundException(options.UpdateDataArchive);
			if (!Directory.Exists(options.ApplicationDir)) throw new DirectoryNotFoundException(options.ApplicationDir);
			using (var file = File.OpenRead(options.UpdateDataArchive))
			{
				using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
				{
					foreach (var entry in zip.Entries)
					{
						var destinationFile = Path.Combine(options.ApplicationDir, entry.FullName);
						for (var i = 0; i < 10; ++i)
						{
							try
							{
								Log($"Try {i} delete {destinationFile}");
								// try to delete
								File.Delete(destinationFile);
								// successful, so we can write new version
								break;
							}
							catch
							{
								// unsuccessful -> wait before next try
								Thread.Sleep(1000);
							}
						}
						try
						{
							Log($"Extracting new {destinationFile}");
							entry.ExtractToFile(destinationFile);
						}
						catch
						{
							//file still in use, no permission -> stop
							Log($"Error extracting new {destinationFile}");
							return;
						}
					}
				}
			}
			Log($"Update Finished");
		}
	}
}