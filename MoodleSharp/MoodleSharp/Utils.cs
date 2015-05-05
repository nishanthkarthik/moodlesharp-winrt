using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace MoodleSharp
{
    class Utils
    {
        static public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public async static Task<StorageFile> SaveAsync(Uri fileUri, StorageFolder folder, string fileName)
        {
            var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            var downloader = new BackgroundDownloader();
            var download = downloader.CreateDownload(fileUri, file);
            var res = await download.StartAsync();
            return file;
        }

        public async Task SaveFileToStorage(StorageFolder folder, string fileName, IHttpContent content)
        {
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            IBuffer buffer = await content.ReadAsBufferAsync();
            using (var ostream = await file.OpenStreamForWriteAsync())
            {
                ostream.Write(buffer.ToArray(), 0, buffer.ToArray().Length);
            }
        }

    }

    static class Reference
    {
        public const string CourseXPath = "//*[@class=\"coursename\"]";
        public const string EnrolledCourseXPath = "//*[@class=\"courses frontpage-course-list-enrolled\"]";
        public const string EnrolledCourseUrlXPath = @"//@href";
        public const string CourseContentParseXpath = "//*[@class=\"activityinstance\"]";
    }
}
