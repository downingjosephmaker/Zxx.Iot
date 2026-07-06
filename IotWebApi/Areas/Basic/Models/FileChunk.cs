using System.ComponentModel;

namespace IotWebApi.Areas.Basic.Models
{
    public class FileChunk
    {
        public string MovieName { get; set; }
        public string FileName { get; set; }
        public int PartNumber { get; set; }
        public int Size { get; set; }
        public int Chunks { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Total { get; set; }
    }
    public class FileSort
    {
        public const string PART_NUMBER = ".partNumber-";
        public string FileName { get; set; }
        public int PartNumber { get; set; }
    }

    public class AttachFileDto
    {
        public string Base64String { get; set; }
        public string ImageType { get; set; }
        public string ImageName { get; set; }
    }

    /// <summary>
    /// 文件上传后返回模型
    /// </summary>
    public class AttachFileInfo
    {
        /// <summary>
		/// 文件名称
		///</summary>
		[DisplayName("文件名称")]
        public string FileName { get; set; }
        /// <summary>
        /// 文件路径
        ///</summary>
        [DisplayName("文件路径")]
        public string FilePath { get; set; }
        /// <summary>
        /// 文件长度
        ///</summary>
        [DisplayName("文件长度")]
        public long FileLength { get; set; }
        /// <summary>
        /// 后缀名称
        ///</summary>
        [DisplayName("后缀名称")]
        public string FileSuffix { get; set; }
        /// <summary>
        /// 文件类型
        ///</summary>
        [DisplayName("文件类型")]
        public string ContentType { get; set; }
    }

}
