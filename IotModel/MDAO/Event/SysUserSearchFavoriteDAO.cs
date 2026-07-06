using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotModel
{
    /// <summary>
    /// 用户搜索收藏数据访问实现
    /// </summary>
    public class SysUserSearchFavoriteDAO : DbContext<SysUserSearchFavorite>
    {
        private static SysUserSearchFavoriteDAO instance;
        public static SysUserSearchFavoriteDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysUserSearchFavoriteDAO();
                }
                return instance;
            }
        }

    }


}
