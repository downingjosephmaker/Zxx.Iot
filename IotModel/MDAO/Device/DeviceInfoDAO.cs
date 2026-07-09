using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IotModel
{
    public sealed partial class DeviceInfoDAO : FullEntityContext<DeviceInfoEntity>
    {
        private static DeviceInfoDAO instance;
        public static DeviceInfoDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DeviceInfoDAO();
                }
                return instance;
            }
        }

        public override bool Update(DeviceInfoEntity info)
        {
            bool isok = false;
            try
            {
                bool ischangetype = false;
                var entity = GetOneBy(t => t.DeviceId == info.DeviceId);
                if (entity != null && info.DeviceTypeCode != entity.DeviceTypeCode) ischangetype = true;
                var old_FullCode = entity.FullCode;
                var old_FullName = entity.FullName;
                var old_ParentId = entity.ParentId;
                if (info.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.DeviceId == info.ParentId);
                    if (parent != null)
                    {
                        info.FullCode = parent.FullCode + info.DeviceId + "|";
                        info.FullName = parent.FullName + "|" + info.DeviceName;
                        info.TreeLevel = info.FullCode.Count(c => c == '|') - 1;
                        if (!parent.HasChild)
                        {
                            parent.HasChild = true;
                            UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                else
                {
                    info.FullCode = "|" + info.DeviceId + "|";
                    info.FullName = info.DeviceName;
                    info.TreeLevel = 1;
                }

                isok = UpdateIgnoreColumns(info, it => new
                {
                    it.CreateId,
                    it.CreateTime,
                    it.CreateName,
                });

                if (isok)
                {
                    if (old_ParentId > 0 && old_ParentId != info.ParentId)
                    {
                        var oldParent = GetOneBy(t => t.DeviceId == old_ParentId);
                        if (oldParent != null)
                        {
                            oldParent.HasChild = GetListBy(t => t.ParentId == old_ParentId).IsZxxAny();
                            UpdateColumns(oldParent, it => new { it.HasChild });
                        }
                    }

                    //如果当前节点的FullCode和FullName变更后，批量更新所有子节点的FullCode和FullName
                    if (old_FullCode != info.FullCode || old_FullName != info.FullName)
                    {
                        var childList = GetListBy(t => t.FullCode.StartsWith(old_FullCode) && t.DeviceId != info.DeviceId);
                        if (childList.IsZxxAny())
                        {
                            var childMap = childList.ToDictionary(t => t.DeviceId);
                            foreach (var child in childList.OrderBy(t => t.TreeLevel))
                            {
                                var parentFullCode = info.FullCode;
                                var parentFullName = info.FullName;
                                if (child.ParentId != info.DeviceId && childMap.TryGetValue(child.ParentId, out var parentChild))
                                {
                                    parentFullCode = parentChild.FullCode;
                                    parentFullName = parentChild.FullName;
                                }

                                child.FullCode = parentFullCode + child.DeviceId + "|";
                                child.FullName = parentFullName + "|" + child.DeviceName;
                                child.TreeLevel = child.FullCode.Count(c => c == '|') - 1;
                            }
                            // 批量更新子节点
                            UpdateColumns(childList, it => new
                            {
                                it.FullCode,
                                it.FullName,
                                it.TreeLevel,
                            });
                        }
                    }
                    if (ischangetype)
                    {
                        var typelist = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == info.DeviceTypeCode);
                        if (typelist.IsZxxAny())
                        {
                            List<Expand_DeviceParam> paramlist = new List<Expand_DeviceParam>();
                            foreach (var item in typelist)
                            {
                                Expand_DeviceParam param = new Expand_DeviceParam();
                                item.CopyTypeValue(param);
                                param.StatusValues = item.ExpandStatusValues;
                                paramlist.Add(param);
                            }
                            var deviceParam = DeviceParamDAO.Instance.GetOneBy(t => t.DeviceId == info.DeviceId);
                            if (deviceParam != null)
                            {
                                var _deviceParam = new DeviceParam();
                                deviceParam.CopyTypeValue(_deviceParam);
                                _deviceParam.ExpandJson = paramlist.ToJson();
                                _deviceParam.DeviceTypeCode = info.DeviceTypeCode;
                                SysCommonDAO<DeviceParam>.Instance.Update(_deviceParam);
                            }
                            else
                            {
                                var _deviceParam = new DeviceParam();
                                info.CopyTypeValue(_deviceParam);
                                _deviceParam.ExpandJson = paramlist.ToJson();
                                SysCommonDAO<DeviceParam>.Instance.Insert(_deviceParam);
                            }
                        }
                        else
                        {
                            DeviceParamDAO.Instance.DeleteBy(t => t.DeviceId == info.DeviceId);
                        }
                    }
                }

                var aa = sqlSugar;
                return isok;
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        public override bool Insert(DeviceInfoEntity info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();

                var upinfo = InsertReturnEntity(info);
                if (upinfo.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.DeviceId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.DeviceId + "|";
                        upinfo.FullName = parent.FullName + "|" + upinfo.DeviceName;
                        upinfo.TreeLevel = upinfo.FullCode.Count(c => c == '|') - 1;
                        if (!parent.HasChild)
                        {
                            parent.HasChild = true;
                            UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                else
                {
                    upinfo.FullCode = "|" + upinfo.DeviceId + "|";
                    upinfo.FullName = upinfo.DeviceName;
                    upinfo.TreeLevel = 1;
                }
                if (upinfo.SortBorder.IsZxxNullOrEmpty())
                {
                    var list = GetListBy(t => t.TenantId == upinfo.TenantId);
                    if (list.IsZxxAny())
                    {
                        string first = ObjLevel[upinfo.TreeLevel];
                        var levellist = list.FindAll(t => t.TreeLevel == upinfo.TreeLevel && t.ParentId == upinfo.ParentId);
                        if (levellist.IsZxxAny())
                        {
                            int max = levellist.Select(t => (t.SortBorder ?? "").Replace(first, "").ToZxxInt()).Max();
                            if (max > 0) upinfo.SortBorder = $"{first}{(max + 3).ToString().PadLeft(3, '0')}";
                        }
                        else
                        {
                            upinfo.SortBorder = $"{first}001";
                        }
                    }
                    else
                    {
                        upinfo.SortBorder = "A001";
                    }
                }

                isok = UpdateColumns(upinfo, it => new
                {
                    it.FullCode,
                    it.FullName,
                    it.TreeLevel,
                    it.SortBorder,
                });

                if (isok)
                {
                    var typelist = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == upinfo.DeviceTypeCode);
                    if (typelist.IsZxxAny())
                    {
                        List<Expand_DeviceParam> paramlist = new List<Expand_DeviceParam>();
                        foreach (var item in typelist)
                        {
                            Expand_DeviceParam param = new Expand_DeviceParam();
                            item.CopyTypeValue(param);
                            param.StatusValues = item.ExpandStatusValues;
                            paramlist.Add(param);
                        }
                        DeviceParamEntity deviceParam = new DeviceParamEntity();
                        upinfo.CopyTypeValue(deviceParam);
                        deviceParam.ExpandJson = "";
                        deviceParam.ExpandObjects.AddRange(paramlist);
                        DeviceParamDAO.Instance.Insert(deviceParam);
                    }

                    //判断现有设备大类
                    var trlist = DeviceTypeRunDAO.Instance.GetListBy(t => t.TenantId == upinfo.TenantId);
                    Match match = Regex.Match(upinfo.DeviceTypeFullCode, @"\|([^|]+)\|");
                    string firstDeviceTypeCode = match.Groups[1].Value; // 匹配第一个 `|...|` 之间的内容
                    var tp = DeviceTypeDAO.Instance.GetOneBy(t => t.TypeCode == firstDeviceTypeCode);
                    if (tp != null && !trlist.Any(t => t.DeviceTypeCode == tp.TypeCode))
                    {
                        DeviceTypeRunDAO.Instance.Insert(new DeviceTypeRun()
                        {
                            TenantId = upinfo.TenantId,
                            DeviceTypeCode = tp.TypeCode,
                            DeviceTypeName = tp.TypeName,
                            MenuCode = "otherCollect"
                        });
                    }
                }

                Db.CommitTran();

                var aa = sqlSugar;
                return isok;
            }
            catch (Exception ex)
            {
                Db.RollbackTran();
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        public List<DeviceInfoEntity> GetChildList(List<DeviceInfoEntity> alllist, int pid)
        {
            var childlist = new List<DeviceInfoEntity>();

            var sublist = alllist.FindAll(t => t.ParentId == pid);
            if (sublist.Count == 0)
            {
                childlist.Add(alllist.Find(t => t.DeviceId == pid));
            }
            else
            {
                foreach (var sub in sublist)
                {
                    childlist.AddRange(GetChildList(alllist, sub.DeviceId));
                }
            }

            return childlist;
        }

        public bool DeleteById(int deviceid)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();
                var list = GetListBy(t => t.FullCode.Contains($"|{deviceid}|"));
                if (list.IsZxxAny())
                {
                    var devids = list.Select(t => t.DeviceId).Distinct().ToList();
                    DeleteBy(t => devids.Contains(t.DeviceId));
                    DeviceParamDAO.Instance.DeleteBy(t => devids.Contains(t.DeviceId));
                    DeviceAlarmConfigDAO.Instance.DeleteBy(t => devids.Contains(t.DeviceId));
                    isok = true;
                }
                Db.CommitTran();
                return isok;
            }
            catch (Exception ex)
            {
                isok = false;
                Db.RollbackTran();
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

        public DeviceInfoEntity InsertReEntity(DeviceInfoEntity info)
        {
            bool isok = false;
            try
            {
                Db.BeginTran();

                var upinfo = InsertReturnEntity(info);
                if (upinfo.ParentId > 0)
                {
                    var parent = GetOneBy(t => t.DeviceId == upinfo.ParentId);
                    if (parent != null)
                    {
                        upinfo.FullCode = parent.FullCode + upinfo.DeviceId + "|";
                        upinfo.FullName = parent.FullName + "|" + upinfo.DeviceName;
                        upinfo.TreeLevel = upinfo.FullCode.Count(c => c == '|') - 1;
                        if (!parent.HasChild)
                        {
                            parent.HasChild = true;
                            UpdateColumns(parent, it => new { it.HasChild });
                        }
                    }
                }
                else
                {
                    upinfo.FullCode = "|" + upinfo.DeviceId + "|";
                    upinfo.FullName = upinfo.DeviceName;
                    upinfo.TreeLevel = 1;
                }
                if (upinfo.SortBorder.IsZxxNullOrEmpty())
                {
                    var list = GetListBy(t => t.TenantId == upinfo.TenantId);
                    if (list.IsZxxAny())
                    {
                        string first = ObjLevel[upinfo.TreeLevel];
                        var levellist = list.FindAll(t => t.TreeLevel == upinfo.TreeLevel && t.ParentId == upinfo.ParentId);
                        if (levellist.IsZxxAny())
                        {
                            int max = levellist.Select(t => (t.SortBorder ?? "").Replace(first, "").ToZxxInt()).Max();
                            if (max > 0) upinfo.SortBorder = $"{first}{(max + 3).ToString().PadLeft(3, '0')}";
                        }
                        else
                        {
                            upinfo.SortBorder = $"{first}001";
                        }
                    }
                    else
                    {
                        upinfo.SortBorder = "A001";
                    }
                }

                isok = UpdateColumns(upinfo, it => new
                {
                    it.FullCode,
                    it.FullName,
                    it.TreeLevel,
                    it.SortBorder,
                });

                if (isok)
                {
                    var typelist = DeviceTypeParamDAO.Instance.GetListBy(t => t.DeviceTypeCode == upinfo.DeviceTypeCode);
                    if (typelist.IsZxxAny())
                    {
                        List<Expand_DeviceParam> paramlist = new List<Expand_DeviceParam>();
                        foreach (var item in typelist)
                        {
                            Expand_DeviceParam param = new Expand_DeviceParam();
                            item.CopyTypeValue(param);
                            param.StatusValues = item.ExpandStatusValues;
                            paramlist.Add(param);
                        }
                        DeviceParamEntity deviceParam = new DeviceParamEntity();
                        upinfo.CopyTypeValue(deviceParam);
                        deviceParam.ExpandJson = "";
                        deviceParam.ExpandObjects.AddRange(paramlist);
                        DeviceParamDAO.Instance.Insert(deviceParam);
                    }

                    //判断现有设备大类
                    var trlist = DeviceTypeRunDAO.Instance.GetListBy(t => t.TenantId == upinfo.TenantId);
                    Match match = Regex.Match(upinfo.DeviceTypeFullCode, @"\|([^|]+)\|");
                    string firstDeviceTypeCode = match.Groups[1].Value; // 匹配第一个 `|...|` 之间的内容
                    var tp = DeviceTypeDAO.Instance.GetOneBy(t => t.TypeCode == firstDeviceTypeCode);
                    if (tp != null && !trlist.Any(t => t.DeviceTypeCode == tp.TypeCode))
                    {
                        DeviceTypeRunDAO.Instance.Insert(new DeviceTypeRun()
                        {
                            TenantId = upinfo.TenantId,
                            DeviceTypeCode = tp.TypeCode,
                            DeviceTypeName = tp.TypeName,
                            MenuCode = "otherCollect"
                        });
                    }
                }

                Db.CommitTran();

                var aa = sqlSugar;
                var entity = GetOneBy(t => t.DeviceId == upinfo.DeviceId);
                return entity;
            }
            catch (Exception ex)
            {
                Db.RollbackTran();
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
            }
        }

    }
}
