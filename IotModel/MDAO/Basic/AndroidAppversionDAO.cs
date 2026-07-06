using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IotModel
{
	public sealed partial class AndroidAppversionDAO : DbContext<AndroidAppversion>
    {
		private static AndroidAppversionDAO instance;
		public static AndroidAppversionDAO Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new AndroidAppversionDAO();
				}
				return instance;
			}
		}			
		
	}
}