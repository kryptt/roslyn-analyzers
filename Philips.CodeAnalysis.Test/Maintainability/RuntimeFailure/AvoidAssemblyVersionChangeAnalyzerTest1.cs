﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.RuntimeFailure
{
	[TestClass]
	public class AvoidAssemblyVersionChangeAnalyzerTest1 : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new TestableAvoidAssemblyVersionChangeAnalyzer(WrongReturnedVersion);
		}

		private const string TestCode = @"
class Foo 
{
}
";

		private const string ConfiguredVersion = "1.2.3.4";
		private const string WrongReturnedVersion = "2.0.0.0";

		protected override Dictionary<string, string> GetAdditionalAnalyzerConfigOptions()
		{
			Dictionary<string, string> options = new()
			{
				{ $@"dotnet_code_quality.{ Helper.ToDiagnosticId(DiagnosticId.AvoidAssemblyVersionChange) }.assembly_version", ConfiguredVersion }
			};
			return options;
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void HasWrongVersionTriggersDiagnostics()
		{
			VerifyDiagnostic(TestCode, DiagnosticId.AvoidAssemblyVersionChange);
		}
	}
}
