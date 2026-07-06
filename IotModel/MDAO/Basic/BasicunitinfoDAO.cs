using System;

namespace IotModel
{
    public sealed partial class BasicunitInfoDAO : FullEntityContext<BasicunitInfoEntity>
    {
        private static BasicunitInfoDAO instance;
        public static BasicunitInfoDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BasicunitInfoDAO();
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
                var entity = new BasicunitInfoEntity
                {
                    UnitName = "开发单位",
                    AreaId = "|330000|330100|330106|",
                    AreaName = "浙江省|杭州市|西湖区",
                    CreateId = 1,
                    CreateTime = time,
                    CreateName = "开发管理员",
                    UpdateId = 1,
                    UpdateTime = time,
                    UpdateName = "开发管理员"
                };
                Insert(entity);
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