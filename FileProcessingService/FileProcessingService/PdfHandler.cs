using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using ZXing;

namespace FileProcessingService
{
	class PdfHandler
	{

		private Document _document;
		private readonly PdfDocumentRenderer _renderer;

		public IList<string> GetFileNames { get; private set; } = new List<string>();

		public bool IsPdfReady { get; set; }

		public PdfHandler()
		{
			_document = new Document();
			_renderer = new PdfDocumentRenderer();
		}

		public void PushFile(string fullFileName)
		{

			if (!HasBarcode(fullFileName))
			{
				GetFileNames.Add(fullFileName);
			}
			else
			{

				CreatePdfDocument(GetFileNames);

				IsPdfReady = true;
			}
		}

		public void CreatePdfDocument(IList<string> getFileNames)
		{
			_document = new Document();

			foreach (var chunk in GetFileNames)
			{
				var section = _document.AddSection();
				var image = section.AddImage(chunk);

				image.Height = _document.DefaultPageSetup.PageHeight;
				image.Width = _document.DefaultPageSetup.PageWidth;
			}
		}

		private bool HasBarcode(string fullFileName)
		{
			var reader = new BarcodeReader() { AutoRotate = true };

			var bmp = (Bitmap)Bitmap.FromFile(fullFileName);

			var res = reader.Decode(bmp);

			bmp.Dispose();

			return res != null ? true : false;
		}

		public void PdfSave(string dir)
		{
			try
			{
				var randomName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
				_renderer.Document = _document;
				

				if (_document.Sections.Count > 0)
				{
					_renderer.RenderDocument();
					_renderer.Save(dir + "\\" + randomName + ".pdf");
				}
			}
			catch (Exception ex)
			{

				Console.WriteLine(ex.Message);
			}

		}

		public void Reset()
		{
			GetFileNames = new List<string>();
			IsPdfReady = false;
			_document = new Document();
		}
	}
}
