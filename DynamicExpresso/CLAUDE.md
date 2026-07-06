# DynamicExpresso - 动态表达式计算引擎

[根目录](../CLAUDE.md) > **DynamicExpresso**

---

## 模块职责

DynamicExpresso 是一个 C# 动态表达式解释器，能够在运行时解析、编译和执行字符串形式的 C# 表达式。它基于 .NET Expression Tree 构建，提供了类型安全的动态求值能力，广泛应用于公式计算、规则引擎、动态查询等场景。

---

## 项目背景

本项目是 DynamicExpresso 的**定制修复版本**，专门针对高并发环境下的状态污染问题进行了深度优化。

### 修复问题

在高并发环境下，原版 DynamicExpresso 出现计算结果异常：
- **期望计算**：`Sa*0.0001` (Sa=1340000) → 134
- **实际结果**：出现负数或异常值

### 根本原因

状态污染来源于多个层面：
1. **.NET Expression.Constant 内部缓存机制** - 多线程环境下可能导致常量值混乱
2. **Expression.Compile 委托缓存** - Lambda 表达式编译后的委托可能被缓存
3. **参数表达式重用** - ParameterExpression 实例在多次调用间可能被重用

### 修复方案

1. **修改 `Interpreter.SetVariable` 方法**：通过反射绕过 Expression.Constant 内部缓存，使用 ValueHolder 包装器确保每次创建独立的表达式
2. **修改 `Lambda` 编译机制**：为每个参数创建唯一名称，使用 ParameterRewriteVisitor 重写表达式中的参数引用
3. **确保线程安全**：每次计算都使用独立的表达式实例，避免线程间的状态共享

---

## 入口与启动

### 模块类型
- **类库项目** (.NET Standard 2.0)
- **无独立入口**：被其他项目引用后使用

### 基本使用

```csharp
// 1. 创建解释器
var interpreter = new Interpreter();

// 2. 设置变量
interpreter.SetVariable("x", 5);
interpreter.SetVariable("y", 10);

// 3. 解析并执行表达式
var result = interpreter.Eval("x + y * 2"); // 返回 25

// 4. 或解析为 Lambda 以复用
var lambda = interpreter.Parse("x + y * 2");
var result1 = lambda.Invoke(new Parameter("x", 5), new Parameter("y", 10));
var result2 = lambda.Invoke(new Parameter("x", 3), new Parameter("y", 7));
```

---

## 核心类与接口

### 1. Interpreter - 解释器核心类

**职责**：解析、编译和执行表达式

#### 主要构造方法

```csharp
// 默认配置（包含基本类型和系统关键字）
public Interpreter()

// 自定义配置
public Interpreter(InterpreterOptions options)
```

#### InterpreterOptions 配置选项

| 选项 | 说明 |
|-----|------|
| `PrimitiveTypes` | 加载基本类型（string, int, double, DateTime, Guid 等） |
| `SystemKeywords` | 加载系统关键字（true, false, null） |
| `CommonTypes` | 加载常用类型（System.Math, System.Convert, System.Linq.Enumerable） |
| `CaseInsensitive` | 变量和参数名称不区分大小写 |
| `LateBindObject` | 将 Object 类型表达式视为动态类型 |
| `LambdaExpressions` | 启用 Lambda 表达式解析（有轻微性能开销） |
| `Default` | 加载所有默认配置（PrimitiveTypes + SystemKeywords + CommonTypes） |

#### 核心方法

##### 1. 变量管理

```csharp
// 设置变量（自动推断类型）
public Interpreter SetVariable(string name, object value)

// 设置变量（指定类型）
public Interpreter SetVariable<T>(string name, T value)
public Interpreter SetVariable(string name, object value, Type type)

// 移除变量
public Interpreter UnsetVariable(string name)
```

**示例**：
```csharp
var interpreter = new Interpreter();
interpreter.SetVariable("name", "John");
interpreter.SetVariable("age", 30);
interpreter.SetVariable("salary", 5000.50);
interpreter.SetVariable("isActive", true);
```

##### 2. 类型引用

```csharp
// 引用类型，使其可在表达式中使用
public Interpreter Reference(Type type)
public Interpreter Reference(Type type, string typeName)
public Interpreter Reference(ReferenceType type)
```

**示例**：
```csharp
var interpreter = new Interpreter();
interpreter.Reference(typeof(Math)); // 可在表达式中使用 Math.Pow(2, 3)
interpreter.Reference(typeof(DateTime), "DT"); // 使用别名

var result = interpreter.Eval("Math.Pow(2, 3)"); // 8
var dateResult = interpreter.Eval("DT.Now"); // 当前时间
```

##### 3. 函数注册

```csharp
// 注册自定义函数
public Interpreter SetFunction(string name, Delegate value)
```

**示例**：
```csharp
var interpreter = new Interpreter();

// 注册单参数函数
interpreter.SetFunction("Square", (Func<double, double>)(x => x * x));
var result1 = interpreter.Eval("Square(5)"); // 25

// 注册多参数函数
interpreter.SetFunction("Add", (Func<double, double, double>)((x, y) => x + y));
var result2 = interpreter.Eval("Add(3, 4)"); // 7

// 注册重载函数
interpreter.SetFunction("Calculate", (Func<int, int>)(x => x * 2));
interpreter.SetFunction("Calculate", (Func<double, double>)(x => x * 2.0));
var result3a = interpreter.Eval("Calculate(5)"); // 10 (int)
var result3b = interpreter.Eval("Calculate(5.5)"); // 11.0 (double)
```

##### 4. 表达式解析与执行

```csharp
// 解析为 Lambda（可复用）
public Lambda Parse(string expressionText, params Parameter[] parameters)
public Lambda Parse(string expressionText, Type expressionType, params Parameter[] parameters)

// 直接执行表达式
public object Eval(string expressionText, params Parameter[] parameters)
public T Eval<T>(string expressionText, params Parameter[] parameters)

// 解析为委托
public TDelegate ParseAsDelegate<TDelegate>(string expressionText, params string[] parametersNames)

// 解析为表达式树
public Expression<TDelegate> ParseAsExpression<TDelegate>(string expressionText, params string[] parametersNames)
```

**示例**：
```csharp
var interpreter = new Interpreter();
interpreter.SetVariable("x", 10);
interpreter.SetVariable("y", 20);

// 方式 1：直接执行
var result1 = interpreter.Eval("x + y"); // 30
var result2 = interpreter.Eval<double>("x * 1.5"); // 15.0

// 方式 2：解析为 Lambda（可复用）
var lambda = interpreter.Parse("x + y");
var result3 = lambda.Invoke(); // 30

// 方式 3：解析为强类型委托
var addFunc = interpreter.ParseAsDelegate<Func<int, int, int>>("x + y", "x", "y");
var result4 = addFunc(10, 20); // 30

// 方式 4：解析为表达式树（可进一步分析或修改）
var expr = interpreter.ParseAsExpression<Func<int, int, int>>("x + y", "x", "y");
var result5 = expr.Compile()(10, 20); // 30
```

##### 5. 标识符检测

```csharp
// 检测表达式中使用的标识符
public IdentifiersInfo DetectIdentifiers(string expression)
public IdentifiersInfo DetectIdentifiers(string expression, DetectorOptions options)
```

**示例**：
```csharp
var interpreter = new Interpreter();
interpreter.SetVariable("x", 1);

var info = interpreter.DetectIdentifiers("x + y + Math.Sqrt(z)");

Console.WriteLine($"未知标识符: {string.Join(", ", info.UnknownIdentifiers)}");
// 输出: 未知标识符: y, z

Console.WriteLine($"已知标识符: {string.Join(", ", info.Identifiers.Select(i => i.Name))}");
// 输出: 已知标识符: x

Console.WriteLine($"已知类型: {string.Join(", ", info.Types.Select(t => t.Name))}");
// 输出: 已知类型: Math
```

---

### 2. Lambda - Lambda 表达式封装类

**职责**：封装已解析的表达式，支持多次调用

#### 核心属性

```csharp
public Expression Expression { get; }          // 表达式树
public string ExpressionText { get; }          // 原始表达式文本
public Type ReturnType { get; }                // 返回类型
public IEnumerable<Parameter> UsedParameters { get; }  // 实际使用的参数
public IEnumerable<Parameter> DeclaredParameters { get; }  // 声明的参数
public IEnumerable<Identifier> Identifiers { get; }  // 使用的标识符
public bool CaseInsensitive { get; }           // 是否不区分大小写
```

#### 核心方法

```csharp
// 调用表达式
public object Invoke()
public object Invoke(params Parameter[] parameters)
public object Invoke(params object[] args)

// 编译为委托
public TDelegate Compile<TDelegate>()

// 转换为 Lambda 表达式
public Expression<TDelegate> LambdaExpression<TDelegate>()
```

**示例**：
```csharp
var interpreter = new Interpreter();
var lambda = interpreter.Parse("x > 0 && x < 100", typeof(bool), new Parameter("x", typeof(int)));

// 调用
var result1 = lambda.Invoke(new Parameter("x", 50)); // true
var result2 = lambda.Invoke(new Parameter("x", 150)); // false

// 编译为委托
var validator = lambda.Compile<Func<int, bool>>();
var result3 = validator(50);  // true
var result4 = validator(150); // false

// 查看信息
Console.WriteLine($"表达式: {lambda.ExpressionText}");  // x > 0 && x < 100
Console.WriteLine($"返回类型: {lambda.ReturnType}");     // System.Boolean
Console.WriteLine($"使用的参数: {string.Join(", ", lambda.UsedParameters.Select(p => p.Name))}"); // x
```

---

### 3. Parameter - 参数类

**职责**：定义表达式的参数

#### 构造方法

```csharp
// 自动推断类型
public Parameter(string name, object value)

// 指定类型
public Parameter(string name, Type type, object value = null)

// 从 ParameterExpression 创建
public Parameter(ParameterExpression parameterExpression)

// 泛型创建
public static Parameter Create<T>(string name, T value)
```

#### 属性

```csharp
public string Name { get; }          // 参数名
public Type Type { get; }            // 参数类型
public object Value { get; }         // 参数值
public ParameterExpression Expression { get; }  // 参数表达式
```

**示例**：
```csharp
// 方式 1：自动推断类型
var param1 = new Parameter("x", 10);           // int
var param2 = new Parameter("name", "John");    // string

// 方式 2：指定类型
var param3 = new Parameter("value", typeof(double), 10.5);

// 方式 3：泛型创建
var param4 = Parameter.Create("age", 30);

// 使用参数
var interpreter = new Interpreter();
var lambda = interpreter.Parse("x * y", typeof(int),
    new Parameter("x", typeof(int)),
    new Parameter("y", typeof(int)));
var result = lambda.Invoke(new Parameter("x", 5), new Parameter("y", 3)); // 15
```

---

### 4. Identifier - 标识符类

**职责**：表示表达式中的变量、函数或类型引用

#### 属性

```csharp
public string Name { get; }            // 标识符名称
public Expression Expression { get; }  // 标识符对应的表达式
```

#### 子类

- **FunctionIdentifier**：函数标识符，支持重载
- **MethodGroupExpression**：方法组表达式

---

### 5. 异常类

#### ParseException
**描述**：表达式解析失败

```csharp
public ParseException(string message, int position)
public int Position { get; }  // 错误位置
```

#### 其他异常
- `UnknownIdentifierException` - 未知的标识符
- `NoApplicableMethodException` - 没有适用的方法
- `DuplicateParameterException` - 重复的参数
- `ReflectionNotAllowedException` - 不允许反射操作
- `AssignmentOperatorDisabledException` - 赋值运算符已禁用

---

## 支持的 C# 语法

### 运算符

| 类别 | 运算符 |
|-----|-------|
| 算术运算符 | `+`, `-`, `*`, `/`, `%` |
| 关系运算符 | `==`, `!=`, `<`, `>`, `<=`, `>=` |
| 逻辑运算符 | `&&`, `\|\|`, `!` |
| 位运算符 | `&`, `\|`, `^`, `~`, `<<`, `>>` |
| 条件运算符 | `? :` |
| 赋值运算符 | `=`, `+=`, `-=`, `*=`, `/=` 等（需显式启用） |
| 空值合并运算符 | `??` |
| 空值条件运算符 | `?.` |
| 成员访问运算符 | `.` |

### 数据类型

- **基本类型**：`int`, `double`, `float`, `decimal`, `long`, `short`, `byte`, `bool`, `char`, `string`
- **日期时间**：`DateTime`, `TimeSpan`
- **其他**：`Guid`, `object`

### 表达式类型

```csharp
// 算术表达式
"x + y * 2"

// 逻辑表达式
"x > 0 && y < 100"

// 条件表达式
"x > 0 ? \"positive\" : \"negative\""

// 成员访问
"person.Name.Length"

// 方法调用
"Math.Pow(x, 2)"

// Lambda 表达式（需启用 LambdaExpressions 选项）
"x => x > 0"
"(x, y) => x + y"

// 数组访问
"array[0]"

// 类型转换
"(double)x / 2"
```

---

## 在主项目中的使用场景

### 1. ExpressoFormula - 公式计算封装类

**文件位置**：`ZhjngkModel/Common/ExpressoFormula.cs`

**职责**：为能源平台提供公式计算功能，支持设备能耗计算、告警判断等

#### 核心方法

##### 返回布尔值的计算

```csharp
// 单参数公式计算
public static bool CalculateSingle(string formula, string paramCode, double paramValue)

// 示例：判断设备是否过载
var isOverload = ExpressoFormula.CalculateSingle("a > 100", "current", 150); // true

// 多参数公式计算
public static bool CalculateMultiple(string formula, Dictionary<string, double> parameters)

// 示例：判断多个条件
var parameters = new Dictionary<string, double>
{
    { "temperature", 85 },
    { "pressure", 2.5 }
};
var isAlarm = ExpressoFormula.CalculateMultiple("temperature > 80 || pressure > 3", parameters);
// true
```

##### 返回数值的计算

```csharp
// 单参数公式计算
public static double CalculateSingleValue(string formula, string paramCode, double paramValue, int decimalPlaces = 3)

// 示例：计算能耗
var energy = ExpressoFormula.CalculateSingleValue("a * 0.0001", "Sa", 1340000);
// 返回 134.0

// 多参数公式计算
public static double CalculateMultipleValue(string formula, Dictionary<string, double> parameters, int decimalPlaces = 3)

// 示例：加权计算
var parameters = new Dictionary<string, double>
{
    { "P", 100 },
    { "Q", 50 },
    { "factor", 0.8 }
};
var result = ExpressoFormula.CalculateMultipleValue("(P + Q) * factor", parameters);
// 返回 120.0
```

##### 返回字符串的计算

```csharp
// 单参数公式计算（支持三元运算符）
public static string CalculateString(string formula, string paramCode, double paramValue, int decimalPlaces = 3)

// 示例：设备状态判断
var status = ExpressoFormula.CalculateString("a == 1 ? \"打开\" : \"关闭\"", "state", 1);
// 返回 "打开"

// 示例：数值格式化
var value = ExpressoFormula.CalculateString("a * 0.01", "value", 12345, 2);
// 返回 "123.45"

// 多参数公式计算
public static string CalculateMultipleString(string formula, Dictionary<string, double> parameters, int decimalPlaces = 3)

// 示例：复杂条件判断
var parameters = new Dictionary<string, double>
{
    { "temp", 25 },
    { "humidity", 60 }
};
var result = ExpressoFormula.CalculateMultipleString(
    "temp > 30 && humidity > 70 ? \"高温高湿\" : temp > 30 ? \"高温\" : humidity > 70 ? \"高湿\" : \"正常\"",
    parameters);
// 返回 "正常"
```

##### 公式验证与参数检测

```csharp
// 验证公式语法
public static bool ValidateFormula(string formula)

var isValid = ExpressoFormula.ValidateFormula("x + y * 2"); // true
var isInvalid = ExpressoFormula.ValidateFormula("x + ");    // false

// 获取公式中的参数
public static List<string> GetFormulaParameters(string formula)

var params = ExpressoFormula.GetFormulaParameters("a * 0.1 + b * 0.2");
// 返回 ["a", "b"]
```

#### 实际应用场景

1. **设备能耗计算**
   ```csharp
   // 计算有功功率
   var power = ExpressoFormula.CalculateSingleValue("U * I * cosPhi", "U", 220, 2);

   // 计算电能消耗
   var energy = ExpressoFormula.CalculateSingleValue("P * time", "P", 1000);
   ```

2. **告警规则判断**
   ```csharp
   // 温度告警
   var isTempAlarm = ExpressoFormula.CalculateSingle("temp > 80", "temp", device.Temp);

   // 复合条件告警
   var params = new Dictionary<string, double>
   {
       { "temp", device.Temp },
       { "pressure", device.Pressure },
       { "flow", device.Flow }
   };
   var isAlarm = ExpressoFormula.CalculateMultiple(
       "temp > 90 || (pressure > 5 && flow < 10)",
       params);
   ```

3. **数据转换与格式化**
   ```csharp
   // 单位转换
   var kW = ExpressoFormula.CalculateString("W / 1000", "W", device.Power, 2);

   // 状态映射
   var status = ExpressoFormula.CalculateString(
       "state == 1 ? \"运行\" : state == 2 ? \"待机\" : \"停止\"",
       "state", device.State);
   ```

4. **策略执行**
   ```csharp
   // 根据条件计算控制参数
   var params = new Dictionary<string, double>
   {
       { "currentTemp", 25 },
       { "targetTemp", 26 },
       { "delta", 1 }
   };
   var controlValue = ExpressoFormula.CalculateMultipleValue(
       "(targetTemp - currentTemp) * delta",
       params);
   // 根据 controlValue 调整设备输出
   ```

---

## 关键依赖与配置

### NuGet 依赖

```xml
<PackageReference Include="Microsoft.CSharp" Version="4.7.0" />
```

- **Microsoft.CSharp**：提供动态语言运行时支持，用于动态类型绑定

### .NET 版本

- **目标框架**：.NET Standard 2.0
- **兼容性**：可与 .NET Framework 4.6.1+、.NET Core 2.0+、.NET 5/6/7/8+ 项目兼容

---

## 测试与质量

### 当前状态

- **无自动化测试**（在当前项目中）
- **已修复**：高并发状态污染问题
- **验证方式**：通过主项目实际运行验证

### 测试验证方法

1. **单线程稳定性测试**
   ```csharp
   for (int i = 0; i < 1000; i++)
   {
       var result = ExpressoFormula.CalculateSingleValue("Sa * 0.0001", "Sa", 1340000);
       Debug.Assert(result == 134.0);
   }
   ```

2. **多线程并发测试**
   ```csharp
   var tasks = new List<Task>();
   for (int i = 0; i < 100; i++)
   {
       tasks.Add(Task.Run(() =>
       {
           for (int j = 0; j < 1000; j++)
           {
               var result = ExpressoFormula.CalculateSingleValue("Sa * 0.0001", "Sa", 1340000);
               Debug.Assert(result == 134.0);
           }
       }));
   }
   Task.WaitAll(tasks.ToArray());
   ```

3. **高并发压力测试**
   - 在生产环境中监控表达式计算结果
   - 如发现异常值及时报警

---

## 常见问题 (FAQ)

### Q1: 为什么需要修复版本？

**A**: 原版 DynamicExpresso 在高并发环境下存在状态污染问题，可能导致计算结果错误。修复版本通过以下方式解决：
1. 绕过 Expression.Constant 的内部缓存机制
2. 为每次计算创建独立的表达式和委托实例
3. 确保线程间的状态隔离

### Q2: 如何选择使用 `Eval` 还是 `Parse`？

**A**:
- **使用 `Eval`**：一次性计算，不需要复用表达式
  ```csharp
  var result = interpreter.Eval("x + y");
  ```

- **使用 `Parse`**：需要多次执行同一表达式，性能更好
  ```csharp
  var lambda = interpreter.Parse("x + y");
  var result1 = lambda.Invoke(new Parameter("x", 1), new Parameter("y", 2));
  var result2 = lambda.Invoke(new Parameter("x", 3), new Parameter("y", 4));
  ```

### Q3: 如何处理计算异常？

**A**: ExpressoFormula 内部已捕获异常并记录日志，返回默认值（false 或 0 或空字符串）

```csharp
try
{
    var result = ExpressoFormula.CalculateSingleValue("a * 0.1", "a", 100);
    // 处理结果
}
catch (Exception ex)
{
    // ExpressoFormula 内部已记录日志，这里可以添加额外处理
    XTrace.WriteLine($"公式计算失败: {ex.Message}");
}
```

### Q4: 如何提升性能？

**A**:
1. **复用 Lambda**：对于频繁执行的公式，使用 `Parse` 而非 `Eval`
2. **启用 LambdaExpressions 选项**：如需使用 Lambda 表达式
3. **避免频繁创建 Interpreter**：尽量复用 Interpreter 实例
4. **使用强类型委托**：`ParseAsDelegate<T>` 比 `Parse` 更快

```csharp
// 推荐：复用 Lambda
private static readonly Lazy<Func<double, double, double>> _formulaLambda =
    new Lazy<Func<double, double, double>>(() =>
    {
        var interpreter = new Interpreter();
        return interpreter.ParseAsDelegate<Func<double, double, double>>("x + y * 0.5", "x", "y");
    });

public static double Calculate(double x, double y)
{
    return _formulaLambda.Value(x, y);
}
```

### Q5: 如何调试公式？

**A**:
1. **使用 `ValidateFormula` 验证语法**
   ```csharp
   if (!ExpressoFormula.ValidateFormula(formula))
   {
       XTrace.WriteLine($"公式语法错误: {formula}");
   }
   ```

2. **使用 `GetFormulaParameters` 检查参数**
   ```csharp
   var params = ExpressoFormula.GetFormulaParameters(formula);
   XTrace.WriteLine($"公式需要的参数: {string.Join(", ", params)}");
   ```

3. **查看异常信息**：ExpressoFormula 会输出详细的异常日志
   ```
   formula:Sa * 0.0001,paramCode:Sa,paramValue:1340000@@@异常详情
   ```

### Q6: 支持哪些复杂表达式？

**A**: 支持大部分 C# 表达式，但不支持语句（如 `if`、`for`、`while`）

```csharp
// 支持
"x + y * 2"
"person.Name ?? \"Unknown\""
"array.Length > 0"
"list.Where(x => x > 0).Sum()"

// 不支持
"if (x > 0) return x; else return -x;"  // 语句，不是表达式
"for (int i = 0; i < 10; i++) ..."      // 循环语句
```

### Q7: 如何使用自定义类型？

**A**: 使用 `Reference` 方法引入类型

```csharp
// 定义自定义类型
public class Device
{
    public double Power { get; set; }
    public double Voltage { get; set; }
    public double Current => Power / Voltage;
}

// 引用类型
var interpreter = new Interpreter();
interpreter.Reference(typeof(Device));

// 在表达式中使用
interpreter.SetVariable("device", new Device { Power = 1000, Voltage = 220 });
var current = interpreter.Eval("device.Current"); // 4.545...
```

### Q8: 如何限制表达式功能（安全考虑）？

**A**:
1. **禁用赋值运算符**（默认）
   ```csharp
   var interpreter = new Interpreter();
   // interpreter.EnableAssignment(AssignmentOperators.All); // 不要启用
   ```

2. **禁用反射**（默认）
   ```csharp
   var interpreter = new Interpreter();
   // interpreter.EnableReflection(); // 不要启用
   ```

3. **限制可用的标识符和类型**
   ```csharp
   var interpreter = new Interpreter(InterpreterOptions.None); // 不加载任何默认类型
   // 只添加需要的类型和函数
   interpreter.Reference(typeof(Math));
   interpreter.SetVariable("x", 1);
   ```

---

## 相关文件清单

### 目录结构
```
DynamicExpresso/
├── Interpreter.cs                    # 解释器核心类
├── Lambda.cs                         # Lambda 表达式封装类（已修复）
├── Parameter.cs                      # 参数类
├── Identifier.cs                     # 标识符类
├── InterpreterOptions.cs             # 解释器配置选项
├── LanguageConstants.cs              # 语言常量定义
├── DefaultNumberType.cs              # 默认数字类型
├── AssignmentOperators.cs            # 赋值运算符
├── Detector.cs                       # 标识符检测器
├── DetectorOptions.cs                # 检测选项
├── IdentifiersInfo.cs                # 标识符信息
├── ParserArguments.cs                # 解析器参数
├── README_修复说明.md                 # 修复说明文档
├── Exceptions/                       # 异常定义
│   ├── DynamicExpressoException.cs  # 基础异常
│   ├── ParseException.cs            # 解析异常
│   ├── UnknownIdentifierException.cs # 未知标识符异常
│   ├── NoApplicableMethodException.cs # 无适用方法异常
│   ├── DuplicateParameterException.cs # 重复参数异常
│   ├── ReflectionNotAllowedException.cs # 不允许反射异常
│   └── AssignmentOperatorDisabledException.cs # 赋值运算符禁用异常
├── Parsing/                          # 解析相关
│   ├── Parser.cs                    # 表达式解析器
│   ├── ParserSettings.cs            # 解析器设置
│   ├── ParserConstants.cs           # 解析器常量
│   ├── ParserArguments.cs           # 解析器参数
│   ├── ParseSignatures.cs           # 解析签名
│   ├── InterpreterExpression.cs     # 解释器表达式
│   ├── Token.cs                     # Token 定义
│   └── TokenId.cs                   # Token ID
├── Reflection/                       # 反射相关
│   ├── MemberFinder.cs              # 成员查找器
│   ├── MethodData.cs                # 方法数据
│   ├── SimpleMethodSignature.cs     # 简单方法签名
│   ├── ReflectionExtensions.cs      # 反射扩展
│   ├── TypeUtils.cs                 # 类型工具
│   └── IndexerData.cs               # 索引器数据
├── Resolution/                       # 解析相关
│   ├── ExpressionUtils.cs           # 表达式工具
│   ├── MethodResolution.cs          # 方法解析
│   └── LateBinders.cs               # 延迟绑定
├── Visitors/                         # 访问者
│   └── DisableReflectionVisitor.cs  # 禁用反射访问者
├── Resources/                        # 资源文件
│   ├── ErrorMessages.resx           # 错误信息资源
│   ├── ErrorMessages.de.resx        # 德语错误信息
│   └── ErrorMessages.Designer.cs    # 错误信息设计器
├── DynamicExpresso.csproj            # 项目文件
└── CLAUDE.md                         # 本文档
```

---

## 变更记录 (Changelog)

### 2026-04-24
- 初始化模块文档
- 完成核心类和使用场景梳理
- 补充常见问题说明
- 记录修复版本的重大改进

### 重大修复（日期不详）
- 修复高并发环境下的状态污染问题
- 修改 `Interpreter.SetVariable` 方法，使用 ValueHolder 包装器
- 修改 `Lambda` 编译机制，使用 ParameterRewriteVisitor 确保独立性
- 每次计算都使用独立的表达式实例，确保线程安全

---

## 参考资料

- **原项目地址**：[DynamicExpresso on GitHub](https://github.com/davideicardi/DynamicExpresso)
- **相关类库**：
  - [System.Linq.Expressions](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions)
  - [Expression Trees](https://docs.microsoft.com/en-us/dotnet/csharp/expression-trees)
- **使用场景**：
  - 公式计算引擎
  - 规则引擎
  - 动态查询
  - 脚本执行

---

**最后更新**：2026-04-24 10:15:30

**版本**：0.0.1（修复版）

**维护者**：能源平台开发团队
