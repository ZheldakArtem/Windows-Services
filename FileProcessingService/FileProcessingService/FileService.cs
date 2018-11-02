using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileProcessingService
{
	public class FileService
	{
		private FileSystemWatcher watcher;
		private string inDir;
		private string outDir;
		private string outWrongDir;
		private Thread workThread;
		private ManualResetEvent stopWork;
		private AutoResetEvent newFileEvent;
		private PdfHandler pdfHandler;


		public FileService(string inDir, string outDir, string outWrongDir)
		{
			this.inDir = inDir;
			this.outDir = outDir;
			this.outWrongDir = outWrongDir;

			if (!Directory.Exists(this.inDir))
			{
				Directory.CreateDirectory(this.inDir);
			}

			if (!Directory.Exists(this.outDir))
			{
				Directory.CreateDirectory(this.outDir);
			}

			if (!Directory.Exists(this.outWrongDir))
			{
				Directory.CreateDirectory(this.outWrongDir);
			}

			watcher = new FileSystemWatcher(this.inDir);
			watcher.Created += Watcher_Created;
			workThread = new Thread(WorkProcedure);
			stopWork = new ManualResetEvent(false);
			newFileEvent = new AutoResetEvent(false);
			pdfHandler = new PdfHandler();
		}

		private void WorkProcedure(object obj)
		{

			List<string> fileNameValidList;
			List<string> sortedFileList;
			int counter = 0;
			do
			{
				fileNameValidList = CatchWrongFilesAndMoveInWrongDir(inDir, outWrongDir);

				sortedFileList = fileNameValidList.OrderBy(s => int.Parse(Path.GetFileNameWithoutExtension(s).Split('_')[1])).ToList();

				foreach (var file in sortedFileList)
				{
					counter++;
					if (stopWork.WaitOne(TimeSpan.Zero))
					{
						return;
					}

					var outFile = Path.Combine(outDir, Path.GetFileName(file));

					if (TryOpen(file, 5))
					{
						pdfHandler.PushFile(file);

						if (pdfHandler.IsPdfReady)
						{
							pdfHandler.PdfSave(outDir);

							foreach (var fName in pdfHandler.GetFileNames)
							{
								if (TryOpen(file, 5))
								{
									File.Delete(fName);
								}
							}

							if (TryOpen(file, 5))
							{
								File.Delete(file);
							}

							pdfHandler.Reset();
						}
						else
						{
							if (counter == sortedFileList.Count && pdfHandler.GetFileNames.Count > 0)
							{
								pdfHandler.CreatePdfDocument(pdfHandler.GetFileNames);
								pdfHandler.PdfSave(outDir);

								foreach (var fName in pdfHandler.GetFileNames)
								{
									if (TryOpen(file, 5))
									{
										File.Delete(fName);
									}
								}

								pdfHandler.Reset();
							}
						}

					}
				}

			} while (WaitHandle.WaitAny(new WaitHandle[] { stopWork, newFileEvent }, 1000) != 0);

		}

		private List<string> CatchWrongFilesAndMoveInWrongDir(string targetDir, string outWrongDir)
		{
			Regex regex = new Regex(@".*_\d+\.(png|jpg)");

			List<string> result = new List<string>();

			foreach (var file in Directory.EnumerateFiles(targetDir))
			{
				if (!regex.Match(Path.GetFileName(file)).Success)
				{
					MoveToWrongFileDirectory(outWrongDir, file);
				}
				else if (Path.GetFileName(file).Split('_').Length > 2)
				{
					MoveToWrongFileDirectory(outWrongDir, file);
				}
				else
				{
					result.Add(file);
				}
			}

			return result;
		}

		private void MoveToWrongFileDirectory(string outWrongDir, string file)
		{
			if (TryOpen(file, 10))
			{
				File.Move(file, Path.Combine(outWrongDir, Path.GetFileName(file)));
			}
		}

		private void Watcher_Created(object sender, FileSystemEventArgs e)
		{
			newFileEvent.Set();
		}

		public void Start()
		{
			workThread.Start();
			watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			watcher.EnableRaisingEvents = false;
			stopWork.Set();
			workThread.Join();

		}

		public bool TryOpen(string fileNmae, int attemptCount)
		{
			for (int i = 0; i < attemptCount; i++)
			{
				try
				{
					FileStream file = File.Open(fileNmae, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

					file.Close();

					return true;
				}
				catch (IOException)
				{
					Thread.Sleep(2000);
				}
			}

			return false;
		}
	}
}
