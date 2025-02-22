﻿// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MsTestAnalyzers;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.MsTest
{
	[TestClass]
	public class TestContextAnalyzerTest : CodeFixVerifier
	{
		protected override ImmutableArray<MetadataReference> GetMetadataReferences()
		{
			string testContextReference = typeof(TestContext).Assembly.Location;
			MetadataReference reference = MetadataReference.CreateFromFile(testContextReference);
			return base.GetMetadataReferences().Add(reference);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasTestContextPropertyButNoUsageTest()
		{
			string givenText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestContextAnalyzerTest
{
  public class TestClass
  {
    private string x = ""5"";
    [TestMethod]
    public void TestMethod()
    {
    }

    public TestContext TestContext { get {return x;} }
  }
}
";

string fixedText = @"
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace TestContextAnalyzerTest
{
  public class TestClass
  {
    [TestMethod]
    public void TestMethod()
    {
    }
  }
}
";

			VerifyDiagnostic(givenText, DiagnosticId.TestContext);
			VerifyFix(givenText, fixedText);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestContextAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new TestContextCodeFixProvider();
		}
	}
}