﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

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
	/// Test class for <see cref="PassSenderToEventHandlerAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class PassSenderToEventHandlerAnalyzerTest : CodeFixVerifier
	{
		private const string Correct = @"
using System;
namespace PassSenderTests {
    public class Number {
        public event EventHandler Clicked;
        private void Method() { 
            Clicked(this, EventArgs.Empty);
        }
    }
}";

		private const string WrongSender = @"
using System;
namespace PassSenderTests {
    public class Number {
        public event EventHandler Clicked;
        private void Method() { 
            Clicked(null, EventArgs.Empty);
        }
    }
}";

		private const string WrongArgs = @"
using System;
namespace PassSenderTests {
    public class Number {
        public event EventHandler Clicked;
        private void Method() { 
            Clicked(this, null);
        }
    }
}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("", DisplayName = "Empty"),
		 DataRow(Correct, DisplayName = nameof(Correct))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// Diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow(WrongSender, Correct, DisplayName = nameof(WrongSender)), 
		 DataRow(WrongArgs, Correct, DisplayName = nameof(WrongArgs))]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenArgumentIsNullDiagnosticIsRaised(string testCode, string fixedCode) 
		{
			VerifyDiagnostic(testCode);
			VerifyFix(testCode, fixedCode, shouldAllowNewCompilerDiagnostics:true);
		}

		/// <summary>
		/// No diagnostics expected to show up 
		/// </summary>
		[DataTestMethod]
		[DataRow("File.g", DisplayName = "OutOfScopeSourceFile")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenSourceFileIsOutOfScopeNoDiagnosticIsTriggered(string filePath)
		{
			VerifySuccessfulCompilation(WrongSender, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() 
		{
			return new PassSenderToEventHandlerAnalyzer();
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new PassSenderToEventHandlerCodeFixProvider();
		}
	}
}
