using System;
using System.Collections.Generic;

namespace IotModel
{
    public sealed partial class SysMenuBtnDAO : DbContext<SysMenuBtn>
    {
        private static SysMenuBtnDAO instance;
        public static SysMenuBtnDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysMenuBtnDAO();
                }
                return instance;
            }
        }

        public override void Init()
        {
            try
            {
                string createTime = "2024-03-05 08:43:18";
                string updateTime = "2024-03-13 08:53:29";
                // 菜单-按钮关系种子数据(menu_id/button_id 组合为业务主键,值与原始种子完全一致)
                var list = new List<SysMenuBtn>
                {
                    new SysMenuBtn { SnowId = 7238808208716861445, MenuId = "10010", ButtonId = 10, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808208716861441, MenuId = "10010", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808208716861442, MenuId = "10010", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808208716861443, MenuId = "10010", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808208716861444, MenuId = "10010", ButtonId = 11, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808225443745796, MenuId = "10011", ButtonId = 10, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808225443745793, MenuId = "10011", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808225443745794, MenuId = "10011", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808225443745795, MenuId = "10011", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808225443745797, MenuId = "10011", ButtonId = 11, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7254696332256874498, MenuId = "10012", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7254696332256874497, MenuId = "10012", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7254696332256874496, MenuId = "10012", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7256959027433705474, MenuId = "10015", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7256959027433705475, MenuId = "10015", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7256959027433705473, MenuId = "10015", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238804990708420611, MenuId = "10021", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238804990708420610, MenuId = "10021", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238804990708420609, MenuId = "10021", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238804990708420612, MenuId = "10021", ButtonId = 9, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238784873417150465, MenuId = "10022", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238784873417150466, MenuId = "10022", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238784873417150467, MenuId = "10022", ButtonId = 10, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238784873417150468, MenuId = "10022", ButtonId = 11, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238784873417150464, MenuId = "10022", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808256125079554, MenuId = "10025", ButtonId = 10, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808256125079555, MenuId = "10025", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808256125079556, MenuId = "10025", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808256125079557, MenuId = "10025", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808256125079553, MenuId = "10025", ButtonId = 11, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808272222818305, MenuId = "10026", ButtonId = 4, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808272222818306, MenuId = "10026", ButtonId = 2, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808272222818307, MenuId = "10026", ButtonId = 3, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808272222818308, MenuId = "10026", ButtonId = 11, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" },
                    new SysMenuBtn { SnowId = 7238808272222818309, MenuId = "10026", ButtonId = 10, MbSort = 0, CreateId = 1, CreateTime = createTime, CreateName = "开发管理员", UpdateId = 1, UpdateTime = updateTime, UpdateName = "开发管理员" }
                };
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
