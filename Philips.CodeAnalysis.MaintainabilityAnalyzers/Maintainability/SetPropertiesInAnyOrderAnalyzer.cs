﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SetPropertiesInAnyOrderAnalyzer : SingleDiagnosticAnalyzer<AccessorDeclarationSyntax, SetPropertiesInAnyOrderSyntaxNodeAction>
	{
		private const string Title = @"Set properties in any order";
		private const string MessageFormat = @"Avoid getting other properties when setting property {0}.";
		private const string Description = @"Getting other properties in a setter makes this setter dependent on the order in which these properties are set.";

		public SetPropertiesInAnyOrderAnalyzer()
			: base(DiagnosticId.SetPropertiesInAnyOrder, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class SetPropertiesInAnyOrderSyntaxNodeAction : SyntaxNodeAction<AccessorDeclarationSyntax>
	{
	
		public override void Analyze()
		{
			var prop = Node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
			if (Node.Body == null || prop == null)
			{
				return;
			}

			var type = prop.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (type == null)
			{
				return;
			}

			var propertiesInType = GetProperties(type);
			var otherProperties = propertiesInType.Except(new[] { prop.Identifier.Text });

			if (Node.Body.Statements.Any(s => ReferencesOtherProperties(s, otherProperties)))
			{
				var propertyName = prop.Identifier.Text;
				var loc = Node.GetLocation();
				ReportDiagnostic(loc, propertyName);
			}
		}

		private static IEnumerable<string> GetProperties(BaseTypeDeclarationSyntax type)
		{
			return type.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsAssignable).Select(prop => prop.Identifier.Text);
		}

		private static bool IsAssignable(PropertyDeclarationSyntax property)
		{
			return property.Initializer != null || property.AccessorList.Accessors.Any(IsPublicSetter);
		}

		private static bool IsPublicSetter(AccessorDeclarationSyntax accessor)
		{
			return accessor.Keyword.Text == StringConstants.Set && !accessor.Modifiers.Any(SyntaxKind.PrivateKeyword);
		}

		private static bool ReferencesOtherProperties(StatementSyntax statement, IEnumerable<string> otherProperties)
		{

			return statement.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
				.Any(name => otherProperties.Contains(name.Identifier.Text));
		}
	}
}
