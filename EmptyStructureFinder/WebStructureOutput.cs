using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EmptyStructureFinder
{
    public class WebStructureOutput : IDisposable
    {
        private string _webTitle;
        private bool _createWebFolder;
        private MemoryStream _memoryStream;
        private HashSet<string> _entries;
        private ZipOutputStream _zipStream;
        private readonly ZipEntryFactory _entryFactory;
        private bool _hasData;

        public WebStructureOutput(string webTitle, bool createWebFolder = false)
        {
            _webTitle = webTitle;
            _createWebFolder = createWebFolder;
            _memoryStream = new MemoryStream();
            _entries = new HashSet<string>();
            _entryFactory = new ZipEntryFactory();
            _zipStream = new ZipOutputStream(_memoryStream);
            _zipStream.IsStreamOwner = false;
            _hasData = false;
        }

        public void AddWeb()
        {
            if (!_createWebFolder) return;
            EnsureDirectoryEntry(_webTitle);
        }

        public void AddList(string listTitle)
        {
            AddWeb();
            string localListPath = _createWebFolder ? string.Format("{0}/{1}", _webTitle, listTitle) : listTitle;
            EnsureDirectoryEntry(localListPath);
        }

        public void AddStructure(string listTitle, string itemName, int itemId, string structureId, byte[] structureImage)
        {
            AddList(listTitle);
            string localFileName = _createWebFolder? string.Format("{0}/{1}/{2}_{3}_{4}.png", _webTitle, listTitle, itemName, itemId, structureId):
                string.Format("{0}/{1}_{2}_{3}.png", listTitle, itemName, itemId, structureId);
            if (!_entries.Contains(localFileName))
            {
                _entries.Add(localFileName);
                var fileEntry = _entryFactory.MakeFileEntry(localFileName);
                _zipStream.PutNextEntry(fileEntry);
                _zipStream.Write(structureImage, 0, structureImage.Length);
                _zipStream.CloseEntry();
                _hasData = true;
            }
        }

        public void Save()
        {
            if (_hasData)
            {
                _zipStream.Close();
                _zipStream.Dispose();
                _memoryStream.Position = 0;
                using (var fs = new FileStream(_webTitle + ".zip", FileMode.Create))
                {
                    byte[] buffer = new byte[16 * 1024];
                    int bytesRead;

                    while ((bytesRead = _memoryStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        private void EnsureDirectoryEntry(string localtPath)
        {
            if (!_entries.Contains(localtPath))
            {
                _entries.Add(localtPath);
                _zipStream.PutNextEntry(_entryFactory.MakeDirectoryEntry(localtPath));
                _zipStream.CloseEntry();
            }
        }

        public void Dispose()
        {
            _memoryStream.Close();
            _memoryStream.Dispose();
        }
    }
}
