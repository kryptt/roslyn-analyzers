﻿// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	[TestClass]
	public class NamespacePrefixAnalyzerTest : DiagnosticVerifier
	{

		private const string ClassString = @"
			using System;
			using System.Globalization;
			namespace {0}Culture.Test
			{{
			class Foo 
			{{
				public void Foo()
				{{
				}}
			}}
			}}
			";

		private const string ConfiguredPrefix = @"Philips.iX";

		private DiagnosticResultLocation GetBaseDiagnosticLocation(int rowOffset = 0, int columnOffset = 0)
		{
			return new DiagnosticResultLocation("Test.cs", 4 + rowOffset, 14 + columnOffset);
		}


		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ NamespacePrefixAnalyzer.RuleForIncorrectNamespace.Id }.namespace_prefix", ConfiguredPrefix  }
			};
			return options;
		}
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NamespacePrefixAnalyzer();
		}

		[DataTestMethod]
		[DataRow("")]
		[DataRow("test")]
		[DataRow("Philips.Test")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void ReportIncorrectNamespacePrefix(string prefix)
		{

			string code = string.Format(ClassString, prefix);
			DiagnosticResult expected = new()
			{
				Id = Helper.ToDiagnosticId(DiagnosticId.NamespacePrefix),
				Message = new Regex(".+ "),
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					GetBaseDiagnosticLocation(0,0)
				}
			};

			VerifyDiagnostic(code, expected);
		}

		[DataRow(ConfiguredPrefix + ".")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoNotReportANamespacePrefixError(string ns)
		{
			string code = string.Format(ClassString, ns);
			VerifySuccessfulCompilation(code);
		}

		[DataRow("System.Runtime.CompilerServices")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void DoNotReportANamespaceOnExemptList(string ns)
		{
			string template = @"namespace {0} {{ class Foo {{ }} }}";
			string code = string.Format(template, ns);
			VerifySuccessfulCompilation(code);
		}
	}
}
