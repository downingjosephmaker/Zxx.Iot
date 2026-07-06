using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using DynamicExpresso.Reflection;
using DynamicExpresso.Resources;

namespace DynamicExpresso
{
	/// <summary>
	/// Represents a lambda expression that can be invoked. This class is thread safe.
	/// </summary>
	public class Lambda
	{
		private readonly Expression _expression;
		private readonly ParserArguments _parserArguments;
		private readonly Lazy<Delegate> _delegate;

			internal Lambda(Expression expression, ParserArguments parserArguments)
	{
		_expression = expression ?? throw new ArgumentNullException(nameof(expression));
		_parserArguments = parserArguments ?? throw new ArgumentNullException(nameof(parserArguments));

		// 为了避免委托缓存导致的状态污染，每次都重新编译
		// 不使用Lazy<Delegate>缓存机制，确保每次调用都生成新的委托
		_delegate = new Lazy<Delegate>(() => CompileUniqueDelegate());
	}

	/// <summary>
	/// 编译唯一的委托实例，避免缓存导致的状态污染
	/// </summary>
	/// <returns></returns>
	private Delegate CompileUniqueDelegate()
	{
		try
		{
			// 创建参数表达式的副本，确保每次都是新的实例
			var parameterExpressions = _parserArguments.UsedParameters
				.Select(p => Expression.Parameter(p.Expression.Type, $"{p.Name}_{Guid.NewGuid():N}"))
				.ToArray();

			// 创建参数映射，用于替换表达式中的参数引用
			var parameterMap = new Dictionary<ParameterExpression, ParameterExpression>();
			var originalParameters = _parserArguments.UsedParameters.Select(p => p.Expression).ToArray();
			
			for (int i = 0; i < originalParameters.Length && i < parameterExpressions.Length; i++)
			{
				parameterMap[originalParameters[i]] = parameterExpressions[i];
			}

			// 使用参数替换访问器重写表达式
			var rewrittenExpression = new ParameterRewriteVisitor(parameterMap).Visit(_expression);
			
			// 编译重写后的表达式
			return Expression.Lambda(rewrittenExpression, parameterExpressions).Compile();
		}
		catch
		{
			// 如果重写失败，使用原始方法
			return Expression.Lambda(_expression, _parserArguments.UsedParameters.Select(p => p.Expression).ToArray()).Compile();
		}
	}

	/// <summary>
	/// 参数重写访问器，用于替换表达式中的参数引用
	/// </summary>
	private class ParameterRewriteVisitor : ExpressionVisitor
	{
		private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterMap;

		public ParameterRewriteVisitor(Dictionary<ParameterExpression, ParameterExpression> parameterMap)
		{
			_parameterMap = parameterMap ?? throw new ArgumentNullException(nameof(parameterMap));
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			return _parameterMap.TryGetValue(node, out var replacement) ? replacement : node;
		}
	}

		public Expression Expression { get { return _expression; } }
		public bool CaseInsensitive { get { return _parserArguments.Settings.CaseInsensitive; } }
		public string ExpressionText { get { return _parserArguments.ExpressionText; } }
		public Type ReturnType { get { return Expression.Type; } }

		/// <summary>
		/// Gets the parameters actually used in the expression parsed.
		/// </summary>
		/// <value>The used parameters.</value>
		[Obsolete("Use UsedParameters or DeclaredParameters")]
		public IEnumerable<Parameter> Parameters { get { return _parserArguments.UsedParameters; } }

		/// <summary>
		/// Gets the parameters actually used in the expression parsed.
		/// </summary>
		/// <value>The used parameters.</value>
		public IEnumerable<Parameter> UsedParameters { get { return _parserArguments.UsedParameters; } }
		/// <summary>
		/// Gets the parameters declared when parsing the expression.
		/// </summary>
		/// <value>The declared parameters.</value>
		public IEnumerable<Parameter> DeclaredParameters { get { return _parserArguments.DeclaredParameters; } }

		public IEnumerable<ReferenceType> Types { get { return _parserArguments.UsedTypes; } }
		public IEnumerable<Identifier> Identifiers { get { return _parserArguments.UsedIdentifiers; } }

		public object Invoke()
		{
			return InvokeWithUsedParameters(new object[0]);
		}

		public object Invoke(params Parameter[] parameters)
		{
			return Invoke((IEnumerable<Parameter>)parameters);
		}

		public object Invoke(IEnumerable<Parameter> parameters)
		{
			var args = (from usedParameter in UsedParameters
						from actualParameter in parameters
						where usedParameter.Name.Equals(actualParameter.Name, _parserArguments.Settings.KeyComparison)
						select actualParameter.Value)
				.ToArray();

			return InvokeWithUsedParameters(args);
		}

		/// <summary>
		/// Invoke the expression with the given parameters values.
		/// </summary>
		/// <param name="args">Order of parameters must be the same of the parameters used during parse (DeclaredParameters).</param>
		/// <returns></returns>
		public object Invoke(params object[] args)
		{
			var parameters = new List<Parameter>();
			var declaredParameters = DeclaredParameters.ToArray();

			if (args != null)
			{
				if (declaredParameters.Length != args.Length)
					throw new InvalidOperationException(ErrorMessages.ArgumentCountMismatch);

				for (var i = 0; i < args.Length; i++)
				{
					var parameter = new Parameter(
						declaredParameters[i].Name,
						declaredParameters[i].Type,
						args[i]);

					parameters.Add(parameter);
				}
			}

			return Invoke(parameters);
		}

		private object InvokeWithUsedParameters(object[] orderedArgs)
		{
			try
			{
				return _delegate.Value.DynamicInvoke(orderedArgs);
			}
			catch (TargetInvocationException exc)
			{
				if (exc.InnerException != null)
					ExceptionDispatchInfo.Capture(exc.InnerException).Throw();

				throw;
			}
		}

		public override string ToString()
		{
			return ExpressionText;
		}

		/// <summary>
		/// Generate the given delegate by compiling the lambda expression.
		/// </summary>
		/// <typeparam name="TDelegate">The delegate to generate. Delegate parameters must match the one defined when creating the expression, see UsedParameters.</typeparam>
		public TDelegate Compile<TDelegate>()
		{
			var lambdaExpression = LambdaExpression<TDelegate>();
			return lambdaExpression.Compile();
		}

		[Obsolete("Use Compile<TDelegate>()")]
		public TDelegate Compile<TDelegate>(IEnumerable<Parameter> parameters)
		{
			var lambdaExpression = Expression.Lambda<TDelegate>(_expression, parameters.Select(p => p.Expression).ToArray());
			return lambdaExpression.Compile();
		}

		/// <summary>
		/// Generate a lambda expression.
		/// </summary>
		/// <returns>The lambda expression.</returns>
		/// <typeparam name="TDelegate">The delegate to generate. Delegate parameters must match the one defined when creating the expression, see UsedParameters.</typeparam>
		public Expression<TDelegate> LambdaExpression<TDelegate>()
		{
			return Expression.Lambda<TDelegate>(_expression, DeclaredParameters.Select(p => p.Expression).ToArray());
		}

		internal LambdaExpression LambdaExpression(Type delegateType)
		{
			var parameterExpressions = DeclaredParameters.Select(p => p.Expression).ToArray();
			var types = delegateType.GetGenericArguments();

			// return type
			if (delegateType.GetGenericTypeDefinition() == ReflectionExtensions.GetFuncType(parameterExpressions.Length))
				types[types.Length - 1] = _expression.Type;

			var genericType = delegateType.GetGenericTypeDefinition();
			var inferredDelegateType = genericType.MakeGenericType(types);
			return Expression.Lambda(inferredDelegateType, _expression, parameterExpressions);
		}
	}
}
