﻿// © 2015 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using Sitecore.Pathfinder.Extensions;

namespace Sitecore.Pathfinder.IO
{
    [Export(typeof(IFileSystemService))]
    public class FileSystemService : IFileSystemService
    {
        public virtual void Copy(string sourceFileName, string destinationFileName, bool forceUpdate = true)
        {
            if (!forceUpdate)
            {
                var fileInfo1 = new FileInfo(sourceFileName);
                var fileInfo2 = new FileInfo(destinationFileName);

                if (fileInfo1.Exists && fileInfo2.Exists && fileInfo1.LastWriteTimeUtc == fileInfo2.LastWriteTimeUtc && fileInfo1.Length == fileInfo2.Length)
                {
                    return;
                }
            }

            var directoryName = Path.GetDirectoryName(destinationFileName);
            if (string.IsNullOrEmpty(directoryName))
            {
                throw new DirectoryNotFoundException();
            }

            Directory.CreateDirectory(directoryName);

            File.Copy(sourceFileName, destinationFileName, true);
            File.SetLastWriteTimeUtc(destinationFileName, File.GetLastWriteTimeUtc(sourceFileName));
        }

        public virtual void CreateDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
        }

        public virtual void DeleteDirectory(string directory)
        {
            Directory.Delete(directory, true);
        }

        public virtual void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }

        public virtual bool DirectoryExists(string directory)
        {
            return Directory.Exists(directory);
        }

        public virtual bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        public virtual IEnumerable<string> GetDirectories(string directory)
        {
            return Directory.GetDirectories(directory);
        }

        public virtual IEnumerable<string> GetFiles(string directory, SearchOption searchOptions = SearchOption.TopDirectoryOnly)
        {
            return Directory.GetFiles(directory, "*", searchOptions);
        }

        public virtual IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOptions = SearchOption.TopDirectoryOnly)
        {
            return Directory.GetFiles(directory, pattern, searchOptions);
        }

        public virtual DateTime GetLastWriteTimeUtc(string sourceFileName)
        {
            return File.GetLastWriteTimeUtc(sourceFileName);
        }

        public virtual void Mirror(string sourceDirectory, string destinationDirectory)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    Arguments = $"\"{sourceDirectory.TrimEnd('\\')}\" \"{destinationDirectory.TrimEnd('\\')}\" /mir /njh /njs /ndl /nc /ns /np",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "robocopy"
                }
            };

            proc.Start();
            proc.WaitForExit();
        }

        public virtual string[] ReadAllLines(string fileName)
        {
            return File.ReadAllLines(fileName);
        }

        public virtual string ReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public void Rename(string oldFileName, string newFileName)
        {
            File.Move(oldFileName, newFileName);
        }

        public void Unzip(string zipFileName, string destinationDirectory)
        {
            using (var zip = ZipFile.OpenRead(zipFileName))
            {
                foreach (var entry in zip.Entries)
                {
                    try
                    {
                        if (entry.FullName.EndsWith("/"))
                        {
                            Directory.CreateDirectory(Path.Combine(destinationDirectory, entry.FullName));
                        }
                        else
                        {
                            var fileName = Path.Combine(destinationDirectory, entry.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(fileName) ?? string.Empty);
                            entry.ExtractToFile(fileName, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public virtual void WriteAllText(string fileName, string contents)
        {
            File.WriteAllText(fileName, contents, Encoding.UTF8);
        }

        public virtual void WriteAllText(string fileName, string contents, Encoding encoding)
        {
            File.WriteAllText(fileName, contents, encoding);
        }

        public virtual void XCopy(string sourceDirectory, string destinationDirectory)
        {
            var proc = new Process
            {
                StartInfo =
                {
                    UseShellExecute = true,
                    FileName = "xcopy.exe",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = $"\"{sourceDirectory}\" \"{destinationDirectory}\" /E /I /Y"
                }
            };

            proc.Start();
            proc.WaitForExit();
        }
    }
}
