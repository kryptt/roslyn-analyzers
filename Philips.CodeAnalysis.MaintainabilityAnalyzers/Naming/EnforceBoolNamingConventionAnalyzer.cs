﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceBoolNamingConventionAnalyzer : SingleDiagnosticAnalyzer
	{
		private static readonly Regex _privateFieldRegex = new(@"^(_(is|are|should|has|does|was))[A-Z0-9].*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
		private static readonly Regex _publicFieldRegex = new(@"^(Is|Are|Should|Has|Does|Was)[A-Z0-9].*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
		private static readonly Regex _localRegex = new(@"^(is|are|should|has|does|was)[A-Z0-9].*$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		private const string Title = @"Follow variable naming coding guidelines";
		private const string MessageFormat = @"Rename variable '{0}' to fit coding guidelines";
		private const string Description = @"";

		private readonly bool _shouldCheckLocalVariables;
		private readonly bool _shouldCheckFieldVariables;

		public EnforceBoolNamingConventionAnalyzer() : this(true, true) { }

		public EnforceBoolNamingConventionAnalyzer(bool shouldCheckLocalVariables, bool shouldCheckFieldVariables)
			: base(DiagnosticId.EnforceBoolNamingConvention, Title, MessageFormat, Description, Categories.Naming, isEnabled: false)
		{
			_shouldCheckLocalVariables = shouldCheckLocalVariables;
			_shouldCheckFieldVariables = shouldCheckFieldVariables;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
			context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			ForEachStatementSyntax foreachStatement = (ForEachStatementSyntax)context.Node;
			if (!IsTypeBool(foreachStatement.Type, context.SemanticModel))
			{
				return;
			}

			Regex validator = _localRegex;

			if (IsNameValid(validator, foreachStatement.Identifier))
			{
				return;
			}

			var location = foreachStatement.Identifier.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, foreachStatement.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			VariableDeclarationSyntax variableDeclaration = (VariableDeclarationSyntax)context.Node;

			foreach (SyntaxToken identifier in variableDeclaration.Variables.Select(syntax => syntax.Identifier))
			{
				if (!ShouldCheck(variableDeclaration, context, out Regex validator))
				{
					continue;
				}

				if (IsNameValid(validator, identifier))
				{
					continue;
				}

				var location = identifier.GetLocation();
				Diagnostic diagnostic = Diagnostic.Create(Rule, location, identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool ShouldCheck(VariableDeclarationSyntax variableDeclaration, SyntaxNodeAnalysisContext context, out Regex validator)
		{
			validator = null;
			bool shouldCheck;
			switch (variableDeclaration.Parent.Kind())
			{
				case SyntaxKind.LocalDeclarationStatement:
					{
						LocalDeclarationStatementSyntax localDeclaration = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
						if (!IsTypeBool(localDeclaration.Declaration.Type, context.SemanticModel))
						{
							return false;
						}

						shouldCheck = _shouldCheckLocalVariables;
						validator = _localRegex;
						break;
					}
				case SyntaxKind.FieldDeclaration:
					{
						FieldDeclarationSyntax fieldDeclaration = (FieldDeclarationSyntax)variableDeclaration.Parent;

						if (!IsTypeBool(fieldDeclaration.Declaration.Type, context.SemanticModel))
						{
							return false;
						}

						shouldCheck = _shouldCheckFieldVariables;

						if (IsFieldPublic(fieldDeclaration) || IsFieldConst(fieldDeclaration))
						{
							validator = _publicFieldRegex;
						}
						else
						{
							validator = _privateFieldRegex;
						}

						break;
					}
				default:
					shouldCheck = false;
					validator = _privateFieldRegex;
					break;
			}
			return shouldCheck;
		}

		private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			ParameterSyntax parameter = (ParameterSyntax)context.Node;
			if (!IsTypeBool(parameter.Type, context.SemanticModel))
			{
				return;
			}

			IMethodSymbol method = (IMethodSymbol)context.ContainingSymbol;
			if (method.IsOverride)
			{
				return;
			}

			Regex validator = _localRegex;

			if (IsNameValid(validator, parameter.Identifier))
			{
				return;
			}

			var location = parameter.Identifier.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, parameter.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			PropertyDeclarationSyntax property = (PropertyDeclarationSyntax)context.Node;
			if (!IsTypeBool(property.Type, context.SemanticModel))
			{
				return;
			}

			if (property.Modifiers.Any(SyntaxKind.OverrideKeyword))
			{
				return;
			}

			var type = context.SemanticModel.GetDeclaredSymbol(property);
			if (type is IPropertySymbol propertySymbol && propertySymbol.ContainingType.AllInterfaces.Any(x => x.GetMembers(propertySymbol.Name).Any()))
			{
				return;
			}

			Regex validator = _publicFieldRegex;

			if (IsNameValid(validator, property.Identifier))
			{
				return;
			}

			var location = property.Identifier.GetLocation();
			Diagnostic diagnostic = Diagnostic.Create(Rule, location, property.Identifier.ValueText);
			context.ReportDiagnostic(diagnostic);
		}

		private bool IsNameValid(Regex validator, SyntaxToken identifier)
		{
			string name = identifier.ValueText;

			return validator.IsMatch(name);
		}

		private bool IsTypeBool(TypeSyntax typeSyntax, SemanticModel semanticModel)
		{
			if (typeSyntax != null)
			{
				TypeInfo type = semanticModel.GetTypeInfo(typeSyntax);
				if (type.Type != null)
				{
					return type.Type.SpecialType == SpecialType.System_Boolean;
				}
			}

			return false;
		}

		private bool IsFieldPublic(FieldDeclarationSyntax fieldDeclaration)
		{
			return fieldDeclaration.Modifiers.Any(x => x.Kind() == SyntaxKind.PublicKeyword);
		}

		private bool IsFieldConst(FieldDeclarationSyntax fieldDeclaration)
		{
			return fieldDeclaration.Modifiers.Any(x => x.Kind() == SyntaxKind.ConstKeyword);
		}
	}
}
