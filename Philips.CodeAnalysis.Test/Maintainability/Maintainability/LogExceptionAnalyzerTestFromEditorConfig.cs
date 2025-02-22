﻿// © 2020 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	/// <summary>
	/// Test class for <see cref="LogExceptionAnalyzer"/>, for using .editorconfig to configure.
	/// </summary>
	[TestClass]
	public class LogExceptionAnalyzerTestFromEditorConfig : DiagnosticVerifier
	{
		private const string configuredLogMethods = "EditorTestTrace,EditorTestLog";

		private const string CorrectCode = @"
using System;

namespace LogExceptionUnitTests {
public class Program {
    public static void Main(string[] args) {
        try {
            Console.WriteLine('Hello world!');
        } catch {
            Logger.EditorTestLog('Goodbye');            
        }
    }

    private class Logger {
        public static void EditorTestLog(string message) {
        }
    }
}
}";
		/// <summary>
		/// Diagnostics expected to show up.
		/// </summary>
		[DataTestMethod]
		[DataRow(CorrectCode, DisplayName = "CorrectCode")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenExceptionIsLoggedNoDiagnosticShouldBeTriggered(string testCode)
		{
			VerifySuccessfulCompilation(testCode);
		}

		/// <summary>
		/// <inheritdoc cref="DiagnosticVerifier"/>
		/// </summary>
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new LogExceptionAnalyzer();
		}

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticId.LogException) }.log_method_names", configuredLogMethods }
			};
			return options;
		}
	}
}
