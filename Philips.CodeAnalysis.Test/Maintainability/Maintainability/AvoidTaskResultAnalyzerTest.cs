﻿// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidTaskResultAnalyzerTest : CodeFixVerifier
	{
		[DataRow("ValueTask", "4")]
		[DataRow("Task", "() => 4")]
		[DataTestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidTaskResultTest(string taskType, string argument)
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async void Foo()
  {{{{
    {taskType}<int> task = new {taskType}<int>({argument});
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"task.Result");
			string after = string.Format(template, @"await task");

			VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult);
			VerifyFix(before, after);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidTaskResultObjectCreationTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async void Foo()
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"new Task<int>(() => 4).Result");
			string after = string.Format(template, @"await new Task<int>(() => 4)");

			VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult);
			VerifyFix(before, after);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidTaskResultCallMethodTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async Task<int> Foo(int x)
  {{{{
    return new Task<int>(() => x);
  }}}}
  public async void MyTest()
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"Foo(1).Result");
			string after = string.Format(template, @"await Foo(1)");

			VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult);
			VerifyFix(before, after);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AvoidTaskResultCallMethodThisTest()
		{
			string template = $@"
using System.Threading.Tasks;
class FooClass
{{{{
  public async Task<int> Foo(int x)
  {{{{
    return new Task<int>(() => x);
  }}}}
  public async void MyTest()
  {{{{
    var data = {{0}};
  }}}}
}}}}
";
			string before = string.Format(template, @"this.Foo(1).Result");
			string after = string.Format(template, @"await this.Foo(1)");

			VerifyDiagnostic(before, DiagnosticId.AvoidTaskResult);
			VerifyFix(before, after);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidTaskResultCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidTaskResultAnalyzer();
		}
	}
}