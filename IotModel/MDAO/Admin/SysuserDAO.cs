using System;
using System.Collections.Generic;

namespace IotModel
{
    public sealed partial class SysUserDAO : FullEntityContext<SysUserEntity>
    {
        private static SysUserDAO instance;
        public static SysUserDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysUserDAO();
                }
                return instance;
            }
        }

        public override void Init(object[] objs)
        {
            try
            {
                if (_dbContext == null) _dbContext = objs[0];
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                List<SysUserEntity> list = new List<SysUserEntity>();
                list.Add(new SysUserEntity
                {
                    RoleId = 1,
                    UserUid = "superadmin",
                    Password = "ABB7DC685AA5CCE076CC6791DDCFF227551FE274F209F4E5DE44E12ABB07D254", //Admin666
                    PasswordSalt = "B89505C2739D4EB1B6A0F2B995B7B807",
                    TrueName = "开发管理员",
                    UserXb = "未知",
                    IsEnable = 1,
                    UserRemark = "Admin666",
                    TenantId = 1,
                    CreateId = 1,
                    CreateTime = time,
                    CreateName = "开发管理员",
                    UpdateId = 1,
                    UpdateTime = time,
                    UpdateName = "开发管理员"
                });
                InsertRange(list);
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

    }
}