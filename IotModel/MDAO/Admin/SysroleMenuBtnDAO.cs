using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace IotModel
{
	public sealed partial class SysRoleMenuBtnDAO : DbContext<SysRoleMenuBtn>
    {
		private static SysRoleMenuBtnDAO instance;
		public static SysRoleMenuBtnDAO Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new SysRoleMenuBtnDAO();
				}
				return instance;
			}
		}
		
		
	}
}