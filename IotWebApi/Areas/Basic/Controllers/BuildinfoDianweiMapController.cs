using CenBoCommon.Zxx;
using IotLog;
using Microsoft.AspNetCore.Mvc;
using IotModel;
using IotWebApi.Areas.Basic.Models;

namespace IotWebApi.Controllers
{
    /// <summary> 
    /// 建筑布点信息
    /// </summary>
    [ApiController]
    [ControllSort("5-14")]
    public class BuildDianweiMapController : ControllerBaseApi
    {
        /// <summary>
        /// 根据建筑ID上传机房瓦片图
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="_BuildId">建筑ID</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public MetaData BuildDianWeiZipUpload(IFormFile file, int _BuildId)
        {
            MetaData data = new()
            {
                Status = false,
                Message = "上传Zip文件失败"
            };
            if (file == null)
            {
                data.Status = false;
                data.Message = "上传文件不能为空";
                return data;
            }
            var oldmap = SysCommonDAO<BuildDianweiMap>.Instance.GetOneBy(t => t.BuildId == _BuildId);
            if (oldmap != null)
            {
                data.Status = false;
                data.Message = "此建筑已经上传文件，不能重复上传。";
                return data;
            }
            var optmdl = Request.GetToken();
            BuildDianweiMap map = new BuildDianweiMap
            {
                BuildId = _BuildId,
                CreateId = optmdl.UserID,
                CreateTime = DateTime.Now.ToDateTimeString(),
                CreateName = optmdl.UserName,
                UpdateId = optmdl.UserID,
                UpdateTime = DateTime.Now.ToDateTimeString(),
                UpdateName = optmdl.UserName,
            };
            string dirpath = Path.Combine(OperatorCommon.NetLocalfile, "builddianweimap", map.BuildId.ToString());
            if (!Directory.Exists(dirpath))
            {
                var dir = Directory.CreateDirectory(dirpath);
            }
            var filePath = Path.Combine(dirpath, file.FileName);
            using (var fs = System.IO.File.Create(filePath))
            {
                file.CopyTo(fs);
                fs.Flush();
            }

            ZipSharpHelper zip = new ZipSharpHelper();
            zip.UnZip(filePath, dirpath);
            System.IO.File.Delete(filePath);

            var tempSplit = file.FileName.Split('.');
            map.FileName = tempSplit[0];
            map.FileLength = file.Length;
            map.FilePath = Path.Combine(OperatorCommon.NetYingShefile, "builddianweimap", map.BuildId.ToString());

            data.Status = SysCommonDAO<BuildDianweiMap>.Instance.Insert(map);
            if (data.Status)
            {
                data.Message = "上传Zip文件成功";
            }
            return data;
        }

        /// <summary>
        /// 建筑布点信息保存
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string UpdateMapConfig(DianweiMapInfo model)
        {
            Status = false;
            Message = "建筑布点信息保存失败。";
            var optmdl = Request.GetToken();

            var oldmap = SysCommonDAO<BuildDianweiMap>.Instance.GetOneBy(t => t.BuildId == model.BuildId);
            if (oldmap == null) return Message;
            BuildDianweiMapEntity map = new BuildDianweiMapEntity
            {
                BuildId = model.BuildId,
                MapConfig = model.MapConfig,
                UpdateId = optmdl.UserID,
                UpdateTime = DateTime.Now.ToDateTimeString(),
                UpdateName = optmdl.UserName,
            };
            Status = SysCommonDAO<BuildDianweiMap>.Instance.UpdateColumns(map, it => new { it.MapConfig, it.UpdateId, it.UpdateName, it.UpdateTime });
            if (Status)
            {
                Message = "建筑布点信息保存成功。";
            }

            return Message;
        }

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="_BuildId">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public string DeleteByPk(int _BuildId)
        {
            Status = false;
            Message = "建筑点位图删除失败。";

            string oldpath = "";
            var olddata = BuildDianweiMapDAO.Instance.GetOneBy(t => t.BuildId == _BuildId);
            if (olddata != null && !olddata.FilePath.IsZxxNullOrEmpty()) oldpath = olddata.FilePath.Trim();
            Status = BuildDianweiMapDAO.Instance.DeleteBy(t => t.BuildId == _BuildId);
            if (Status)
            {
                if (!oldpath.IsZxxNullOrEmpty())
                {
                    if (OperatorCommon.NetYingShefile.IsZxxNullOrEmpty())
                    {
                        oldpath = Path.Combine(OperatorCommon.NetLocalfile, oldpath);
                    }
                    else
                    {
                        oldpath = oldpath.Replace(OperatorCommon.NetYingShefile, OperatorCommon.NetLocalfile);
                    }
                    if (Directory.Exists(oldpath))
                        Directory.Delete(oldpath, true);
                }
                Message = "建筑点位图信息删除成功。";
            }
            return Message;
        }

        /// <summary>
        /// 根据建筑ID查询数据
        /// </summary>
        /// <param name="_BuildId">建筑ID</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public BuildDianweiMapEntity GetDetailByBuild(int _BuildId)
        {
            var entity = BuildDianweiMapDAO.Instance.GetOneBy(t => t.BuildId == _BuildId);
            if (entity != null)
            {
                var deviceids = entity.ExpandObjects.Select(t => t.DeviceId).Distinct().ToList();
                var paramList = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));
                if (paramList.IsZxxAny())
                {
                    entity.ExpandObjects.ForEach(it =>
                    {
                        var device = paramList.Find(t => t.DeviceId == it.DeviceId);
                        if (device != null && device.ExpandObjects.Count > 0)
                        {
                            foreach (var param in it.DisplayParams)
                            {
                                var _param = device.ExpandObjects.Find(p => p.ParamCode.ToLower() == param.ParamCode.ToLower());
                                if (_param != null)
                                {
                                    param.ParamName = _param.ParamName;
                                    param.ParamValue = $"{_param.ParamValue}{_param.ValueUnit}";
                                    if (param.ParamName == "用电量") param.ParamName = "累计用电量";
                                    if (param.ParamName == "总有功功率") param.ParamName = "总功率";
                                }
                            }
                        }
                    });
                }
            }
            return entity ?? new BuildDianweiMapEntity();
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="model">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Api/[controller]/[action]")]
        [Token]
        [ApiGroup(ApiGroupNames.Basic)]
        public List<BuildDianweiMapEntity> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = BuildDianweiMapDAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber.ToZxxInt();
            return list;
        }


    }
}