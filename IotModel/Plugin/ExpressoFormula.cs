using CenBoCommon.Zxx;
using DynamicExpresso;
using NewLife.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IotModel
{
    /// <summary>
    /// 表达式公式计算类
    /// </summary>
    public class ExpressoFormula
    {
        #region  返回布尔值

        /// <summary>
        /// 单参数公式计算（返回布尔值）
        /// </summary>
        /// <param name="formula">公式表达式，如 "a>3"</param>
        /// <param name="paramCode">参数编码</param>
        /// <param name="paramValue">参数值</param>
        /// <returns>计算结果</returns>
        public static bool CalculateSingle(string formula, string paramCode, double paramValue)
        {
            try
            {
                var interpreter = new Interpreter();
                interpreter.SetVariable(paramCode, paramValue);
                formula = formula.Trim();
                var lambda = interpreter.ParseAsDelegate<Func<bool>>(formula);
                return lambda();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return false;
        }

        /// <summary>
        /// 多参数公式计算（返回布尔值）
        /// </summary>
        /// <param name="formula">公式表达式，如 "a<10 && b>5"</param>
        /// <param name="parameters">参数字典，key为参数名，value为参数值</param>
        /// <returns>计算结果</returns>
        public static bool CalculateMultiple(string formula, Dictionary<string, double> parameters)
        {
            try
            {
                var interpreter = new Interpreter();
                foreach (var kv in parameters)
                {
                    interpreter.SetVariable(kv.Key, kv.Value);
                }
                formula = formula.Trim();
                var lambda = interpreter.ParseAsDelegate<Func<bool>>(formula);
                return lambda();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return false;
        }

        #endregion

        #region  返回数值

        /// <summary>
        /// 单参数公式计算（返回数值）
        /// </summary>
        /// <param name="formula">公式表达式，如 "a*0.1"</param>
        /// <param name="paramCode">参数编码</param>
        /// <param name="paramValue">参数值</param>
        /// <param name="decimalPlaces">小数位数，-1表示不处理小数位</param>
        /// <returns>计算结果</returns>
        public static double CalculateSingleValue(string formula, string paramCode, double paramValue, int decimalPlaces = 3)
        {
            try
            {
                var interpreter = new Interpreter();
                interpreter.SetVariable(paramCode, paramValue);
                formula = formula.Trim();
                var lambda = interpreter.ParseAsDelegate<Func<double>>(formula);
                double result = lambda();
                return decimalPlaces > 0 ? Math.Round(result, decimalPlaces) : result;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return 0;
        }

        /// <summary>
        /// 多参数公式计算（返回数值）
        /// </summary>
        /// <param name="formula">公式表达式，如 "a*0.1 + b*0.2"</param>
        /// <param name="parameters">参数字典，key为参数名，value为参数值</param>
        /// <param name="decimalPlaces">小数位数，-1表示不处理小数位</param>
        /// <returns>计算结果</returns>
        public static double CalculateMultipleValue(string formula, Dictionary<string, double> parameters, int decimalPlaces = 3)
        {
            try
            {
                var interpreter = new Interpreter();
                foreach (var kv in parameters)
                {
                    interpreter.SetVariable(kv.Key, kv.Value);
                }
                formula = formula.Trim();
                var lambda = interpreter.ParseAsDelegate<Func<double>>(formula);
                double result = lambda();
                return decimalPlaces > 0 ? Math.Round(result, decimalPlaces) : result;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return 0;
        }

        #endregion

        #region  返回字符串

        /// <summary>
        /// 公式计算（返回字符串）
        /// </summary>
        /// <param name="formula">公式表达式，如 "a*0.1" 或 "a==1 ? '打开' : '关闭'"</param>
        /// <param name="paramCode">参数编码</param>
        /// <param name="paramValue">参数值</param>
        /// <param name="decimalPlaces">小数位数，-1表示不处理小数位</param>
        /// <returns>计算结果</returns>
        public static string CalculateString(string formula, string paramCode, double paramValue, int decimalPlaces = 3)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(formula) || string.IsNullOrWhiteSpace(paramCode))
                    throw new Exception("公式不能为空");
                if (!formula.Contains(paramCode))
                    throw new Exception("公式未包含参数编码");

                formula = formula.Trim();
                var interpreter = new Interpreter();
                interpreter.SetVariable(paramCode, paramValue);

                // 检查是否包含三元运算符
                if (formula.Contains("?"))
                {
                    return interpreter.Eval<string>(formula);
                }
                else
                {
                    double result = interpreter.Eval<double>(formula);
                    // 调试信息：记录异常的计算结果
                    List<string> paramCodes = new List<string> { "P", "Pa", "Pb", "Pc", "Pf", "Pfa", "Pfb", "Pfc", "Q", "Qa", "Qb", "Qc" };
                    if (result < 0 && paramValue > 0 && !paramCodes.Contains(paramCode))
                    {
                        XTrace.WriteLine($"负值参数（公式）：公式：{formula}-编码:{paramCode}-原始值:{paramValue}-计算值:{result}-小数位:{decimalPlaces}");
                    }
                    return decimalPlaces > 0 ? result.ToString($"F{decimalPlaces}") : result.ToString();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"formula:{formula},paramCode:{paramCode},paramValue:{paramValue}@@@{ex.ToString()}");
            }
            return "";
        }

        /// <summary>
        /// 多参数公式计算（返回字符串）
        /// </summary>
        /// <param name="formula">公式表达式，如 "a*0.1 + b*0.2" 或 "a==1 && b>5 ? '打开' : '关闭'"</param>
        /// <param name="parameters">参数字典，key为参数名，value为参数值</param>
        /// <param name="decimalPlaces">小数位数，-1表示不处理小数位</param>
        /// <returns>计算结果</returns>
        public static string CalculateMultipleString(string formula, Dictionary<string, double> parameters, int decimalPlaces = 3)
        {
            try
            {
                var interpreter = new Interpreter();
                foreach (var kv in parameters)
                {
                    interpreter.SetVariable(kv.Key, kv.Value);
                }
                formula = formula.Trim();
                // 检查是否包含三元运算符
                if (formula.Contains("?"))
                {
                    var lambda = interpreter.ParseAsDelegate<Func<string>>(formula);
                    return lambda();
                }
                else
                {
                    var lambda = interpreter.ParseAsDelegate<Func<double>>(formula);
                    double result = lambda();
                    return decimalPlaces > 0 ? result.ToString($"F{decimalPlaces}") : result.ToString();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"错误参数打印：{parameters.ToJson()}-小数{decimalPlaces}");
                XTrace.WriteException(ex);
            }
            return "";
        }

        #endregion

        #region  公用方法

        /// <summary>
        /// 验证公式语法是否正确
        /// </summary>
        /// <param name="formula">公式表达式</param>
        /// <returns>是否有效</returns>
        public static bool ValidateFormula(string formula)
        {
            try
            {
                var interpreter = new Interpreter();
                interpreter.Parse(formula);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取公式中使用的参数编码列表
        /// </summary>
        /// <param name="formula">公式表达式</param>
        /// <returns>参数编码列表</returns>
        public static List<string> GetFormulaParameters(string formula)
        {
            try
            {
                var interpreter = new Interpreter();
                var expression = interpreter.Parse(formula);
                return expression.Identifiers.Select(i => i.Name).ToList();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            return new List<string>();
        }

        #endregion
    }
}
