using CenBoCommon.Zxx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Polly;
using SixLabors.ImageSharp;
using IotWebApi.Areas.Basic.Models;

namespace IotWebApi.Areas.Basic.Controllers
{
    /// <summary> 
    /// 文件上传
    /// </summary>
    [ApiController]
    [ControllSort("5-3")]
    public class AttachFileController : ControllerBaseApi
    {
        /// <summary>
        /// 上传附件
        /// </summary>
        /// <param name="file">附件</param>
        /// <param name="filetype">附件类型(0:普通 1:图片)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public MetaData UploadFile(IFormFile file, int filetype = 0)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "上传文件失败"
            };
            if (file == null)
            {
                data.Message = "上传文件不能为空";
                return data;
            }

            var fileNameFlag = Guid.NewGuid().ToString();
            string path = Path.Combine(OperatorCommon.NetLocalfile, "attach", fileNameFlag);
            if (filetype == 1) path = Path.Combine(OperatorCommon.NetLocalfile, "Images");
            var extension = Path.GetExtension(file.FileName);
            string fileNamestr = $"{SnowModel.Instance.NewId()}{extension}";
            var filePath = Path.Combine(path, fileNamestr);
            filePath.EnsureDirectory(true);
            using (var fs = System.IO.File.Create(filePath))
            {
                file.CopyTo(fs);
                fs.Flush();
            }
            data.Result = Path.Combine(OperatorCommon.NetYingShefile, "attach", fileNameFlag, fileNamestr);
            if (filetype == 1) data.Result = Path.Combine(OperatorCommon.NetYingShefile, "Images", fileNamestr);
            data.Status = true;
            data.Message = "文件上传成功";

            return data;
        }

        /// <summary>
        /// 图片转Base64保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public MetaData UploadBybBase64(AttachFileDto fileInfo)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "上传文件失败"
            };
            if (fileInfo == null || fileInfo.Base64String.IsZxxNullOrEmpty())
            {
                data.Status = false;
                data.Message = "参数不能为空";
                return data;
            }
            try
            {
                var base64 = Convert.FromBase64String(fileInfo.Base64String);
                using (MemoryStream menStream = new(base64))
                {
                    Image mImage = Image.Load(menStream);
                    string path = Path.Combine(OperatorCommon.NetLocalfile, "Images");
                    string fileNamestr = $"{SnowModel.Instance.NewId()}.{fileInfo.ImageType}";
                    string fileName = Path.Combine(path, fileNamestr);
                    fileName.EnsureDirectory(true);
                    mImage.Save(fileName);
                    data.Result = Path.Combine(OperatorCommon.NetYingShefile, "Images", fileNamestr);
                    data.Status = true;
                    data.Message = "文件上传成功";
                }
            }
            catch (Exception ex)
            {
                data.Status = false;
                data.Message = $"上传文件失败{ex.Message}";
            }
            return data;
        }

        /// <summary>
        /// 断点续传大文件(分包上传)
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public async Task<MetaData> UploadBigFile([FromQuery] FileChunk chunk)
        {
            var token = Request.GetToken();
            MetaData data = new()
            {
                Status = false
            };
            try
            {
                FileHelper help = new();
                if (!help.IsMultipartContentType(Request.ContentType))
                    return null;

                var boundary = help.GetBoundary(Request.HttpContext);
                if (string.IsNullOrEmpty(boundary))
                    return null;

                var fileNameFlag = chunk.MovieName.Split('.')[0];
                string filePath = Path.Combine(OperatorCommon.NetLocalfile, "attach", fileNameFlag);
                if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

                var reader = new MultipartReader(boundary, Request.Body);
                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {
                    var buffer = new byte[chunk.Size];
                    var fileName = help.GetFileName(section.ContentDisposition);
                    chunk.FileName = fileName;
                    var path = Path.Combine(filePath, fileName);
                    using (var stream = new FileStream(path, FileMode.Append))
                    {
                        int bytesRead;
                        do
                        {
                            bytesRead = await section.Body.ReadAsync(buffer, 0, buffer.Length);
                            stream.Write(buffer, 0, bytesRead);

                        } while (bytesRead > 0);
                    }

                    section = await reader.ReadNextSectionAsync();
                }
                if (chunk.PartNumber == chunk.Chunks)
                {
                    Thread.Sleep(2000);
                    data = await MergeChunkFile(chunk, filePath, token.UserName, false);
                    data.Status = true;
                    data.Result = Path.Combine(OperatorCommon.NetYingShefile, "attach", fileNameFlag, chunk.MovieName);
                    return data;
                }
                else
                {
                    data.Message = "未完成上传，请等待！";
                }
            }
            catch
            {
                data.Message = "系统繁忙不能登录，请联系管理员。";
            }

            return null;
        }

        private async Task<MetaData> MergeChunkFile(FileChunk chunk, string filePath, string username, bool isMovie = true)
        {
            MetaData data = new()
            {
                Status = false
            };
            var uploadDirectoryName = Path.Combine(filePath, chunk.FileName);

            var partToken = FileSort.PART_NUMBER;

            var baseFileName = chunk.FileName.Substring(0, chunk.FileName.IndexOf(partToken));

            var searchpattern = $"{Path.GetFileName(baseFileName)}{partToken}*";

            var filesList = Directory.GetFiles(Path.GetDirectoryName(uploadDirectoryName), searchpattern);
            if (!filesList.IsZxxAny())
            {
                data.Status = false;
                return data;
            }

            var mergeFiles = new List<FileSort>();

            foreach (string fileName in filesList)
            {
                var fileNameNumber = fileName.Substring(fileName.IndexOf(FileSort.PART_NUMBER)
                    + FileSort.PART_NUMBER.Length);

                int.TryParse(fileNameNumber, out var number);
                if (number <= 0) continue;

                mergeFiles.Add(new FileSort
                {
                    FileName = fileName,
                    PartNumber = number
                });
            }

            var mergeFileSorts = mergeFiles.OrderBy(s => s.PartNumber).ToArray();

            var fileFullPath = Path.Combine(filePath, baseFileName);
            if (System.IO.File.Exists(fileFullPath))
            {
                System.IO.File.Delete(fileFullPath);
            }

            using var fileStream = new FileStream(fileFullPath, FileMode.Create);

            await Policy.Handle<IOException>()
                      .RetryForeverAsync()
                      .ExecuteAsync(async () =>
                      {
                          foreach (FileSort fileSort in mergeFileSorts)
                          {
                              using FileStream fileChunk =
                                  new(fileSort.FileName, FileMode.Open,
                                  FileAccess.Read, FileShare.Read);

                              await fileChunk.CopyToAsync(fileStream);
                          }
                      });

            Parallel.ForEach(mergeFiles, f =>
            {
                System.IO.File.Delete(f.FileName);
            });
            return data;
        }

    }
}
