﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability;
using System.CodeDom.Compiler;
using Philips.CodeAnalysis.Test.Verifiers;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Maintainability.Readability
{
	[TestClass]
	public class AvoidRedundantSwitchStatementAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidRedundantSwitchStatementAnalyzer();
		}

		[DataRow("byte")]
		[DataRow("int")]
		[DataRow("string")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SwitchWithOnlyDefaultCaseIsFlagged(string type)
		{
			string input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    switch(data)
    {{
      default:
        System.Console.WriteLine(data);
        break;
    }}
  }}
}}
";

			VerifyDiagnostic(input, DiagnosticId.AvoidSwitchStatementsWithNoCases);
		}


		private const string SampleMethodWithSwitches = @"
public class Foo
{
  public void Method(int data)
  {
    switch(data)
    {
      default:
        System.Console.WriteLine(data);
        break;
    }
    int a = data switch
    {
      _ => 1
    }
  }
}
";

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void GeneratedSwitchWithOnlyDefaultCaseIsNotFlagged()
		{
			string input = @"[System.CodeDom.Compiler.GeneratedCodeAttribute(""protoc"", null)]" + SampleMethodWithSwitches;
			VerifySuccessfulCompilation(input);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void GeneratedFileSwitchWithOnlyDefaultCaseIsNotFlagged()
		{
			VerifySuccessfulCompilation(SampleMethodWithSwitches, @"Foo.designer");
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SwitchWithMultipleCasesIsFlagged(string type, string value)
		{
			string input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    switch(data)
    {{
      case {value}:
      default:
        System.Console.WriteLine(data);
        break;
    }}
  }}
}}
";

			VerifyDiagnostic(input, DiagnosticId.AvoidSwitchStatementsWithNoCases);
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SwitchWithMultipleCasesIsIgnored(string type, string value)
		{
			string input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    switch(data)
    {{
      case {value}:
        break;
      default:
        System.Console.WriteLine(data);
        break;
    }}
  }}
}}
";

			VerifySuccessfulCompilation(input);
		}


		[DataRow("byte")]
		[DataRow("int")]
		[DataRow("string")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SwitchExpressionWithOnlyDefaultCaseIsFlagged(string type)
		{
			string input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    int a = data switch
    {{
      _ => 1
    }}
  }}
}}
";

			VerifyDiagnostic(input, DiagnosticId.AvoidSwitchStatementsWithNoCases);
		}

		[DataRow("byte", "1")]
		[DataRow("int", "1")]
		[DataRow("string", "\"foo\"")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void SwitchExpressionWithMultipleCasesIsIgnored(string type, string value)
		{
			string input = $@"
public static class Foo
{{
  public static void Method({type} data)
  {{
    int a = data switch
    {{
      {value} => 2,
      _ => 1
    }}
  }}
}}
";

			VerifySuccessfulCompilation(input);
		}

	}

	[TestClass]
	public class AvoidRedundantSwitchStatementAnalyzerGeneratedCodeTest : AvoidRedundantSwitchStatementAnalyzerTest
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidRedundantSwitchStatementAnalyzer(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		}
	}
}
