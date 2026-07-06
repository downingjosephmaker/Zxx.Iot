using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CenBoCommon.Zxx
{
    public static class TableToList
    {
        /// <summary>
        /// datatable转换为list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<T> GetList<T>(this DataTable table)
        {
            List<T> list = new List<T>();
            T t = default(T);
            PropertyInfo[] propertypes = null;
            string tempName = string.Empty;
            foreach (DataRow row in table.Rows)
            {
                t = Activator.CreateInstance<T>();
                propertypes = t.GetType().GetProperties();
                foreach (PropertyInfo pro in propertypes)
                {
                    tempName = pro.Name;
                    if (table.Columns.Contains(tempName))
                    {
                        object value = row[tempName];
                        if (!value.ToString().Equals(""))
                        {
                            pro.SetValue(t, value, null);
                        }
                    }
                }
                list.Add(t);
            }
            return list.Count == 0 ? null : list;
        }

        public static DataSet ToDataSetList<T>(this IList<T> list)
        {
            if (list == null || list.Count <= 0)
            {
                return null;
            }

            DataSet ds = new DataSet();
            DataTable dt = new DataTable(typeof(T).Name);
            DataColumn column;
            DataRow row;

            PropertyInfo[] myPropertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (T t in list)
            {
                if (t == null)
                {
                    continue;
                }

                row = dt.NewRow();

                for (int i = 0, j = myPropertyInfo.Length; i < j; i++)
                {
                    PropertyInfo pi = myPropertyInfo[i];

                    string name = pi.Name;

                    if (dt.Columns[name] == null)
                    {
                        column = new DataColumn(name, pi.PropertyType);
                        dt.Columns.Add(column);
                    }

                    row[name] = pi.GetValue(t, null);
                }

                dt.Rows.Add(row);
            }

            ds.Tables.Add(dt);

            return ds;
        }

        public static DataTable ToDataTableList<T>(this IList<T> list)
        {
            if (list == null || list.Count <= 0)
            {
                return null;
            }

            DataTable dt = new DataTable(typeof(T).Name);
            DataColumn column = null;
            DataRow row;

            PropertyInfo[] myPropertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (T t in list)
            {
                if (t == null)
                {
                    continue;
                }

                row = dt.NewRow();

                for (int i = 0, j = myPropertyInfo.Length; i < j; i++)
                {
                    PropertyInfo pi = myPropertyInfo[i];

                    string name = pi.Name;

                    if (dt.Columns[name] == null)
                    {
                        var colType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
                        column = new DataColumn(name, colType);
                        dt.Columns.Add(column);
                    }

                    row[name] = pi.GetValue(t, null) ?? DBNull.Value;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }

        public static DataTable ToDataTable<T>(this T t)
        {
            if (t == null)
            {
                return null;
            }

            PropertyInfo[] myPropertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            DataTable dt = new DataTable(typeof(T).Name);
            DataColumn column;
            DataRow row = dt.NewRow();

            for (int i = 0, j = myPropertyInfo.Length; i < j; i++)
            {
                PropertyInfo pi = myPropertyInfo[i];

                string name = pi.Name;
                if (dt.Columns[name] == null)
                {
                    column = new DataColumn(name, pi.PropertyType);
                    dt.Columns.Add(column);
                }

                row[name] = pi.GetValue(t, null);
            }

            dt.Rows.Add(row);

            return dt;
        }

        /// <summary>
        /// DataTable的行转类对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T TableRowToEntity<T>(this DataRow dr)
        {
            Type type = typeof(T);
            var t = Activator.CreateInstance(type);
            try
            {
                PropertyInfo[] prolist = type.GetProperties();
                if (prolist != null && prolist.Length > 0)
                {
                    foreach (var property in prolist)
                    {
                        if (dr.Table.Columns.Contains(property.Name))
                        {
                            var cellValue = dr[property.Name];
                            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                            if (cellValue == DBNull.Value || cellValue == null)
                            {
                                if (Nullable.GetUnderlyingType(property.PropertyType) != null)
                                    property.SetValue(t, null);
                            }
                            else if (underlyingType == typeof(String))
                            {
                                property.SetValue(t, cellValue.ToString());
                            }
                            else if (underlyingType == typeof(Decimal))
                            {
                                property.SetValue(t, cellValue.ToZxxDecimal());
                            }
                            else if (underlyingType == typeof(DateTime))
                            {
                                property.SetValue(t, cellValue.ToZxxDateTime());
                            }
                            else if (underlyingType == typeof(Int32))
                            {
                                property.SetValue(t, cellValue.ToZxxInt());
                            }
                            else if (underlyingType == typeof(Int64))
                            {
                                property.SetValue(t, cellValue.ToZxxLong());
                            }
                            else if (underlyingType == typeof(Double))
                            {
                                property.SetValue(t, cellValue.ToZxxDouble());
                            }
                            else
                            {
                                property.SetValue(t, cellValue);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return (T)t;
        }

        public static DataRow EntityToRow<T>(this T t, DataRow dr)
        {
            try
            {
                PropertyInfo[] prolist = t.GetType().GetProperties();
                if (prolist != null && prolist.Length > 0)
                {
                    foreach (var property in prolist)
                    {
                        if (dr.Table.Columns.Contains(property.Name))
                        {
                            dr[property.Name] = property.GetValue(t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dr;
        }

        public static DataRow RowToRow(this DataRow olddr, DataRow newdr)
        {
            try
            {
                foreach (DataColumn col in olddr.Table.Columns)
                {
                    if (newdr.Table.Columns.Contains(col.ColumnName))
                    {
                        newdr[col.ColumnName] = olddr[col.ColumnName];
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return newdr;
        }


    }
}
