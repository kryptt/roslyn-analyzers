﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class NoRegionsInMethodAnalyzerTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new NoRegionsInMethodAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoRegionNoMethodTest()
		{
			VerifySuccessfulCompilation(@"Class C{C(){}}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void NoRegionTest()
		{
			VerifySuccessfulCompilation(@"Class C{C(){}public void foo(){}}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyClassWithRegionTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region testRegion	#endregion	}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionOutsideMethodTest()
		{
			VerifySuccessfulCompilation(@"Class C{#region testRegion	public void foo() {int x = 2; }	#endregion}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionStartsAndEndsInMethodTest()
		{
			VerifyDiagnostic(@"Class C{	public void foo(){#region testRegion int x = 2;	#endregion }}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionStartsInMethodTest()
		{
			VerifyDiagnostic(@"Class C{ public void foo(){ #region testRegion int x = 2;}	#endregion

	}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionEndsInMethodTest()
		{
			VerifyDiagnostic(@"Class C{
	#region testRegion
	public void foo(){
		int x = 2;
		#endregion
	}
	}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionCoversMultipleMethodsTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region testRegion	public void foo(){	return;	} public void bar(){	}	#endregion	}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionBeforeClassTest()
		{
			VerifySuccessfulCompilation(@"	#region testRegion	#endregion Class C{	public void foo(){	return; }	public void bar(){	}	}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void UnnamedRegionTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region #endregion	public void foo(){	return; }public void bar(){}	}");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void RegionStartsInOneMethodEndsInAnotherTest()
		{
			VerifyDiagnostic(@"
Class C{
	#region 
	public void foo(){
		return;
	}
	public void bar(){
			#endregion

	}

	}");
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void MalformedCodeTest()
		{
			VerifySuccessfulCompilation(@"Class C{	#region 	public void foo(){		return;	}	#endregion	public void bar(){	}	");
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void EmptyStringTest()
		{
			VerifySuccessfulCompilation("");
		}
	}
}
