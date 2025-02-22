﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Philips.CodeAnalysis.Common;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MergeIfStatementsAnalyzer : SingleDiagnosticAnalyzer<IfStatementSyntax, MergeIfStatementsSyntaxNodeAction>
	{
		private const string Title = "Merge If Statements";
		private const string MessageFormat = Title;
		private const string Description = "Merging If statement with outer If statement to reduce cognitive load";

		public MergeIfStatementsAnalyzer()
			: base(DiagnosticId.MergeIfStatements, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class MergeIfStatementsSyntaxNodeAction : SyntaxNodeAction<IfStatementSyntax>
	{
		public override void Analyze()
		{
			// Node has an else clause
			if (Node.Else != null)
			{
				return;
			}

			var parent = Node.Parent;

			if (parent is BlockSyntax parentBlockSyntax)
			{
				// Has multiple statements in the block
				if (parentBlockSyntax.Statements.Count > 1)
				{
					return;
				}

				parent = parentBlockSyntax.Parent;
			}

			// Parent is not an If statement
			if (parent is not IfStatementSyntax parentIfSyntax)
			{
				return;
			}

			// Parent has an else clause
			if (parentIfSyntax.Else != null)
			{
				return;
			}

			// Has ||
			if (IfConditionHasLogicalAnd(Node))
			{
				return;
			}

			// Parent has ||
			if (IfConditionHasLogicalAnd(parentIfSyntax))
			{
				return;
			}

			var location = Node.IfKeyword.GetLocation();
			ReportDiagnostic(location);
		}

		private bool IfConditionHasLogicalAnd(IfStatementSyntax ifStatement)
		{
			return ifStatement.Condition.DescendantTokens().Any((token) => { return token.Kind() == SyntaxKind.BarBarToken; });
		}
	}
}
