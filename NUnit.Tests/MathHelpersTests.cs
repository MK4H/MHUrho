using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using MHUrho.Helpers;

namespace NUnit.Tests {

	[TestFixture]
	public class MathHelpersTests {

		private const double eps = 1e-9;

		[Test]
		public void QuarticFourReal() {
			int numSolutions = MathHelpers.SolveQuartic(3, 6, -123, -126, 1080, out double s0, out double s1, out double s2, out double s3);

			Assert.That(numSolutions == 4, $"Wrong number of solutions: {numSolutions}");
			Assert.That(s0,
						Is.InRange(5 - eps, 5 + eps).Or.InRange(3 - eps, 3 + eps).Or.InRange(-4 - eps, -4 + eps).Or
						.InRange(-6 - eps, -6 + eps));
			Assert.That(s1,
						Is.InRange(5 - eps, 5 + eps).Or.InRange(3 - eps, 3 + eps).Or.InRange(-4 - eps, -4 + eps).Or
						.InRange(-6 - eps, -6 + eps));
			Assert.That(s2,
						Is.InRange(5 - eps, 5 + eps).Or.InRange(3 - eps, 3 + eps).Or.InRange(-4 - eps, -4 + eps).Or
						.InRange(-6 - eps, -6 + eps));
			Assert.That(s3,
						Is.InRange(5 - eps, 5 + eps).Or.InRange(3 - eps, 3 + eps).Or.InRange(-4 - eps, -4 + eps).Or
						.InRange(-6 - eps, -6 + eps));
			Assert.That(s0, Is.Not.InRange(s1 - eps, s1 + eps).And.Not.InRange(s2 - eps, s2 + eps).And.Not.InRange(s3 - eps, s3 + eps));
			Assert.That(s1, Is.Not.InRange(s0 - eps, s0 + eps).And.Not.InRange(s2 - eps, s2 + eps).And.Not.InRange(s3 - eps, s3 + eps));
			Assert.That(s2, Is.Not.InRange(s1 - eps, s1 + eps).And.Not.InRange(s0 - eps, s0 + eps).And.Not.InRange(s3 - eps, s3 + eps));
			Assert.That(s3, Is.Not.InRange(s1 - eps, s1 + eps).And.Not.InRange(s2 - eps, s2 + eps).And.Not.InRange(s0 - eps, s0 + eps));
			Assert.Pass();
		}

		[Test]
		public void QuarticTwoReal() {
			int numSolutions = MathHelpers.SolveQuartic(-20, 5, 17, -29, 87, out double s0, out double s1, out double s2, out double s3);

			Assert.That(numSolutions == 2, $"Wrong number of solutions: {numSolutions}");
			Assert.That(s0,
						Is.InRange(1.487583110336911846655680 - eps, 1.487583110336911846655680 + eps).Or.InRange(-1.682003926585349242265360 - eps, -1.682003926585349242265360 + eps));
			Assert.That(s1,
						Is.InRange(1.487583110336911846655680 - eps, 1.487583110336911846655680 + eps).Or.InRange(-1.682003926585349242265360 - eps, -1.682003926585349242265360 + eps));
			Assert.That(s0, Is.Not.InRange(s1 - eps, s1 + eps));
			Assert.IsNaN(s2);
			Assert.IsNaN(s3);
			Assert.Pass();
		}

		[Test]
		public void QuadraticBasic() {
			int numSolutions = MathHelpers.SolveQuadratic(1, -3, -4, out var s0, out var s1);

			Assert.That(numSolutions == 2, $"Wrong number of solutions: {numSolutions}");
			const double eps = 1e-9;
			Assert.That(s0, Is.InRange(-1 - eps, -1 + eps).Or.InRange(4 - eps, 4 + eps));
			Assert.That(s1, Is.InRange(-1 - eps, -1 + eps).Or.InRange(4 - eps, 4 + eps));
			Assert.That(s0, Is.Not.InRange(s1 - eps, s1 + eps));
			Assert.Pass();
		}

		[Test]
		public void QuadraticWithoutLinear() {
			int numSolutions = MathHelpers.SolveQuadratic(1, 0, -4, out var s0, out var s1);

			Assert.That(numSolutions == 2, $"Wrong number of solutions: {numSolutions}");
			const double eps = 1e-9;
			Assert.That(s0, Is.InRange(2 - eps, 2 + eps).Or.InRange(-2 - eps, -2 + eps));
			Assert.That(s1, Is.InRange(2 - eps, 2 + eps).Or.InRange(-2 - eps, -2 + eps));
			Assert.That(s0, Is.Not.InRange(s1 - eps, s1 + eps));
			Assert.Pass();
		}



	}
}
