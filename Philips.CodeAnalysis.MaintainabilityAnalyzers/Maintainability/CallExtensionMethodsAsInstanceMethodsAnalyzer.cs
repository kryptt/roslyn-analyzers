﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CallExtensionMethodsAsInstanceMethodsAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Call extension methods as if they were instance methods";
		private const string MessageFormat = @"Call extension method {0} as an instance method";
		private const string Description = Title;

		public CallExtensionMethodsAsInstanceMethodsAnalyzer()
			: base(DiagnosticId.ExtensionMethodsCalledLikeInstanceMethods, Title, MessageFormat, Description, Categories.Maintainability)
		{ }


		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterOperationAction(OnInvoke, OperationKind.Invocation);
		}

		private void OnInvoke(OperationAnalysisContext context)
		{
			IInvocationOperation invocationOperation = (IInvocationOperation)context.Operation;

			if (!invocationOperation.TargetMethod.IsExtensionMethod || invocationOperation.Arguments.Length < 1)
			{
				return;
			}

			var thisArgument = invocationOperation.Arguments.First();

			if (thisArgument.IsImplicit)
			{
				return;
			}

			if (HasMatchingMethod(thisArgument, invocationOperation.TargetMethod))
			{
				return;
			}

			var semantic = context.Operation.SemanticModel;

			var statics = semantic.LookupStaticMembers(context.Operation.Syntax.SpanStart);

			if (HasMultipleCallables(context.Operation.Syntax.SpanStart, semantic, statics, invocationOperation.TargetMethod))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation(), invocationOperation.TargetMethod.Name));
		}

		private bool HasMultipleCallables(int position, SemanticModel semanticModel, ImmutableArray<ISymbol> statics, IMethodSymbol currentMethod)
		{
			HashSet<ISymbol> didCheck = new();
			Queue<ISymbol> toCheck = new(statics);

			int count = 0;
			while (toCheck.Count != 0)
			{
				var symbol = toCheck.Dequeue();
				if (!didCheck.Add(symbol))
				{
					continue;
				}

				if (symbol is INamedTypeSymbol namedType && namedType.IsStatic)
				{
					var result = semanticModel.LookupStaticMembers(position, namedType, currentMethod.Name);

					count += result.OfType<IMethodSymbol>().Count(x => x.IsExtensionMethod && x.Parameters.Length == currentMethod.Parameters.Length);
				}
				else if (symbol is INamespaceSymbol namespaceSymbol)
				{
					var result = semanticModel.LookupStaticMembers(position, namespaceSymbol);

					foreach (var r in result.OfType<ITypeSymbol>())
					{
						toCheck.Enqueue(r);
					}
				}

				if (count > 1)
				{
					return true;
				}
			}

			return count > 1;
		}

		private bool HasMatchingMethod(IArgumentOperation thisArgument, IMethodSymbol targetMethod)
		{
			var value = thisArgument.Value;
			var type = value.Type;

			if (value is IConversionOperation conversion)
			{
				type = conversion.Operand.Type;
			}

			return HasMatchingMethod(type, targetMethod);
		}

		private bool HasMatchingMethod(ITypeSymbol type, IMethodSymbol targetMethod)
		{
			foreach (var member in type.GetMembers())
			{
				if (member.Kind != SymbolKind.Method)
				{
					continue;
				}

				if (member.Name == targetMethod.Name)
				{
					return true;
				}
			}

			return type.BaseType is not null && HasMatchingMethod(type.BaseType, targetMethod);
		}
	}
}
