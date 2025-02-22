﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Class for testing AvoidStaticMethodAnalyzer
	/// </summary>
	[TestClass]
	public class AvoidStaticMethodAnalyzerTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidStaticMethodAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidStaticMethodCodeFixProvider();
		}

		protected string CreateFunction(string methodStaticModifier, string classStaticModifier = "", string externKeyword = "", string methodName = "GoodTimes", string returnType = "void", string localMethodModifier = "", string foreignMethodModifier = "", bool isFactoryMethod = false)
		{
			if (!string.IsNullOrWhiteSpace(methodStaticModifier))
			{
				methodStaticModifier += @" ";
			}

			if (!string.IsNullOrWhiteSpace(externKeyword))
			{
				externKeyword += @" ";
			}

			string localMethod = $@"public {localMethodModifier} string BaBaBummmm(string services)
					{{
						return services;
					}}";

			string foreignMethod = $@"public {foreignMethodModifier} string BaBaBa(string services)
					{{
						return services;
					}}";

			string useLocalMethod = (localMethodModifier == "static") ? $@"BaBaBummmm(""testing"")" : string.Empty;
			string useForeignMethod = (foreignMethodModifier == "static") ? $@"BaBaBa(""testing"")" : string.Empty;

			string objectDeclaration = isFactoryMethod ? $@"Caroline caroline = new Caroline();" : string.Empty;

			return $@"
			namespace Sweet {{
				{classStaticModifier} class Caroline
				{{
					{localMethod}

					public {methodStaticModifier}{externKeyword}{returnType} {methodName}()
					{{
						{objectDeclaration}
						{useLocalMethod}
						{useForeignMethod}
						return;
					}}
				}}
				{foreignMethodModifier} class ReachingOut
				{{
					{foreignMethod}
				}}
			}}";
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AllowExternalCode()
		{
			string template = CreateFunction("static", externKeyword: "extern");
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoreIfInStaticClass()
		{
			string template = CreateFunction("static", classStaticModifier: "static");
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void OnlyCatchStaticMethods()
		{
			string template = CreateFunction("");
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AllowStaticMainMethod()
		{
			string template = CreateFunction("static", methodName: "Main");
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void IgnoreIfCallsLocalStaticMethod()
		{
			string template = CreateFunction("static", localMethodModifier: "static");
			// should still catch the local static method being used
			VerifyDiagnostic(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchIfUsesForeignStaticMethod()
		{
			string template = CreateFunction("static", foreignMethodModifier: "static");
			VerifyDiagnostic(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AllowStaticFactoryMethod()
		{
			string template = CreateFunction("static", isFactoryMethod: true);
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void AllowStaticDynamicDataMethod()
		{
			string template = CreateFunction("static", returnType: "IEnumerable<object[]>");
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CatchPlainStaticMethod()
		{
			string template = CreateFunction("static");
			VerifyDiagnostic(template);

			string fixedCode = CreateFunction(@"");
			VerifyFix(template, fixedCode);
		}
	}
}
