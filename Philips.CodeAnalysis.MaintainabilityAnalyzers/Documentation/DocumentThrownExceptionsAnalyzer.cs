﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Documentation
{
	/// <summary>
	/// Analyzer that checks if the text of the XML code documentation contains a reference to each exception being thrown inside the method or property.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DocumentThrownExceptionsAnalyzer : DiagnosticAnalyzer
	{
		private const string DocumentTitle = @"Document thrown exceptions";
		private const string DocumentMessageFormat = @"Document the fact that this method can potentially throw an exception of type {0}.";
		private const string DocumentDescription = @"Be clear to your callers what exception can be thrown from your method by mentioning each of them in an <exception> element in the documentation of the method.";
		private const string InformationalTitle = @"Throw only informational exceptions";
		private const string InformationalMessageFormat = @"Specify context to the {0}, by using a constructor overload that sets the Message property.";
		private const string InformationalDescription = @"Specify context to a thrown exception, by using a constructor overload that sets the Message property.";
		private const string Category = Categories.Documentation;

		private static readonly DiagnosticDescriptor DocumentRule = new(Helper.ToDiagnosticId(DiagnosticId.DocumentThrownExceptions), DocumentTitle, DocumentMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DocumentDescription);
		private static readonly DiagnosticDescriptor InformationalRule = new(Helper.ToDiagnosticId(DiagnosticId.ThrowInformationalExceptions), InformationalTitle, InformationalMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: InformationalDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DocumentRule, InformationalRule);

		private readonly Helper _helper;

		public DocumentThrownExceptionsAnalyzer()
			: this(new Helper())
		{ }

		public DocumentThrownExceptionsAnalyzer(Helper helper)
		{
			_helper = helper;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ThrowStatement);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var throwStatement = (ThrowStatementSyntax)context.Node;

			string thrownExceptionName = null;
			IReadOnlyDictionary<string, string> aliases;
			if (throwStatement.Expression is ObjectCreationExpressionSyntax exceptionCreation)
			{
				// Search of string arguments in the constructor invocation.
				aliases = _helper.GetUsingAliases(throwStatement);
				thrownExceptionName = exceptionCreation.Type.GetFullName(aliases);
				if (!HasStringArgument(context, exceptionCreation.ArgumentList))
				{
					var loc = exceptionCreation.GetLocation();
					Diagnostic diagnostic = Diagnostic.Create(InformationalRule, loc, thrownExceptionName);
					context.ReportDiagnostic(diagnostic);
				}
			}
			else
			{
				// Rethrowing an existing Exception instance.
				if (throwStatement.Expression is IdentifierNameSyntax localVar)
				{
					thrownExceptionName = context.SemanticModel.GetTypeInfo(localVar).Type?.Name;
				}
			}

			if (string.IsNullOrEmpty(thrownExceptionName))
			{
				return;
			}

			aliases = _helper.GetUsingAliases(throwStatement);
			if (aliases.TryGetValue(thrownExceptionName, out string aliasedName))
			{
				thrownExceptionName = aliasedName;
			}

			// Determine our parent.
			var nodeWithDoc = DocumentationHelper.FindAncestorThatCanHaveDocumentation(throwStatement);

			// Check if our parent has proper documentation.
			var docHelper = new DocumentationHelper(nodeWithDoc);
			var mentionedExceptions = docHelper.GetExceptionCrefs();
			if (mentionedExceptions.Any() && !mentionedExceptions.Contains(thrownExceptionName, new NamespaceIgnoringComparer()))
			{
				var loc = throwStatement.ThrowKeyword.GetLocation();
				var properties = ImmutableDictionary<string, string>.Empty.Add(StringConstants.ThrownExceptionPropertyKey, thrownExceptionName);
				Diagnostic diagnostic = Diagnostic.Create(DocumentRule, loc, properties, thrownExceptionName);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool HasStringArgument(SyntaxNodeAnalysisContext context, ArgumentListSyntax attributeList)
		{
			const string stringTypeName = "String";
			return attributeList.Arguments.Any(a =>
			{
				SyntaxNode node = null;
				if (a.Expression is LiteralExpressionSyntax literal)
				{
					node = literal;
				}
				else if (a.Expression is IdentifierNameSyntax identifierName)
				{
					node = identifierName;
				} 
				else if (a.Expression is InvocationExpressionSyntax invocation)
				{
					node = invocation;
				}
				else if (a.Expression is InterpolatedStringExpressionSyntax interpolatedString)
				{
					node = interpolatedString;
				} else if (a.Expression is BinaryExpressionSyntax binaryExpression)
				{
					// Assume the returning type is the same as the left type.
					node = binaryExpression.Left;
				}
				else
				{
					return false;
				}

				return context.SemanticModel.GetTypeInfo(node).Type?.Name == stringTypeName;
			});
		}
	}
}
