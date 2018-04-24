using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Helpers
{
    public static class MathHelpers
    {
		// Utility function used by SolveQuadratic, SolveCubic, and SolveQuartic
		private static bool IsZero(double d) {
			const double eps = 1e-9;
			return d > -eps && d < eps;
		}


		/// <summary>
		/// Solves a*x^2 + b*x + c = 0
		/// </summary>
		/// <returns></returns>
		public static int SolveQuadratic(double a, double b, double c, out double solution0, out double solution1) {
			// https://github.com/erich666/GraphicsGems/blob/240a34f2ad3fa577ef57be74920db6c4b00605e4/gems/Roots3And4.c


			solution0 = double.NaN;
			solution1 = double.NaN;

			double p, q, D;

			/* normal form: x^2 + px + q = 0 */
			p = b / (2 * a);
			q = c / a;

			D = p * p - q;

			if (IsZero(D)) {
				solution0 = -p;
				return 1;
			}
			else if (D < 0) {
				return 0;
			}
			else /* if (D > 0) */ {
				double sqrt_D = System.Math.Sqrt(D);

				solution0 = sqrt_D - p;
				solution1 = -sqrt_D - p;
				return 2;
			}
		}


		//public static int SolveQuadratic1(double a, double b, double c, out double solution0, out double solution1) {
		//	//http://cse.unl.edu/~ylu/raik283/notes/Resoureces/Quadratic_equation.htm#Floating_point_implementation


		//	solution0 = double.NaN;
		//	solution1 = double.NaN;

		//	double D = b * b - 4 * a * c;

		//	if (D < 0) {
		//		return 0;
		//	}
		//	else if (IsZero(D)) {
		//		solution0 = -b / 2 * a;
		//		return 1;
		//	}

		//	double root = Math.Sqrt(D);

		//	if (IsZero(b)) {
		//		solution0 = root / 2 * a;
		//		solution1 = -solution0;
		//		return 2;
		//	}


		//	double q = -1.0 / 2.0 * (b + Math.Sign(b) * root);

		//	solution0 = q / a;
		//	solution1 = c / q;

		//	return 2;

		//}


		//Source https://blog.forrestthewoods.com/solving-ballistic-trajectories-b0165523348c
		public static int SolveCubic(double a, double b, double c, double d, out double solution0, out double solution1, out double solution2) {
			solution0 = double.NaN;
			solution1 = double.NaN;
			solution2 = double.NaN;

			int num;
			double sub;
			double A, B, C;
			double sq_A, p, q;
			double cb_p, D;

			/* normal form: x^3 + Ax^2 + Bx + C = 0 */
			A = b / a;
			B = c / a;
			C = d / a;

			/*  substitute x = y - A/3 to eliminate quadratic term:  x^3 +px + q = 0 */
			sq_A = A * A;
			p = 1.0 / 3.0 * (-1.0 / 3.0 * sq_A + B);
			q = 1.0 / 2.0 * (2.0 / 27.0 * A * sq_A - 1.0 / 3.0 * A * B + C);

			/* use Cardano's formula */
			cb_p = p * p * p;
			D = q * q + cb_p;

			if (IsZero(D)) {
				if (IsZero(q)) /* one triple solution */ {
					solution0 = 0;
					num = 1;
				}
				else /* one single and one double solution */ {
					double u = System.Math.Pow(-q, 1.0 / 3.0);
					solution0 = 2 * u;
					solution1 = -u;
					num = 2;
				}
			}
			else if (D < 0) /* Casus irreducibilis: three real solutions */ {
				double phi = 1.0 / 3 * Math.Acos(-q / Math.Sqrt(-cb_p));
				double t = 2 * Math.Sqrt(-p);

				solution0 = t * Math.Cos(phi);
				solution1 = -t * Math.Cos(phi + Math.PI / 3);
				solution2 = -t * Math.Cos(phi - Math.PI / 3);
				num = 3;
			}
			else /* one real solution */ {
				double sqrt_D = Math.Sqrt(D);
				double u = Math.Pow(sqrt_D - q, 1.0 / 3.0);
				double v = -Math.Pow(sqrt_D + q, 1.0 / 3.0);

				solution0 = u + v;
				num = 1;
			}

			/* resubstitute */
			sub = 1.0 / 3 * A;

			if (num > 0) solution0 -= sub;
			if (num > 1) solution1 -= sub;
			if (num > 2) solution2 -= sub;

			return num;
		}


		/// <summary>
		/// Source https://blog.forrestthewoods.com/solving-ballistic-trajectories-b0165523348c
		///  Solve quartic function: c0*x^4 + c1*x^3 + c2*x^2 + c3*x + c4. 
		///  Returns number of real solutions.
		/// </summary>
		/// <param name="c0"></param>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <param name="c3"></param>
		/// <param name="c4"></param>
		/// <param name="solution0"></param>
		/// <param name="solution1"></param>
		/// <param name="solution2"></param>
		/// <param name="solution3"></param>
		/// <returns></returns>
		public static int SolveQuartic(double c0, double c1, double c2, double c3, double c4, out double solution0, out double solution1, out double solution2, out double solution3) {
			solution0 = double.NaN;
			solution1 = double.NaN;
			solution2 = double.NaN;
			solution3 = double.NaN;

			double[] coeffs = new double[4];
			double z, u, v, sub;
			double A, B, C, D;
			double sq_A, p, q, r;
			int num;

			/* normal form: x^4 + Ax^3 + Bx^2 + Cx + D = 0 */
			A = c1 / c0;
			B = c2 / c0;
			C = c3 / c0;
			D = c4 / c0;

			/*  substitute x = y - A/4 to eliminate cubic term: x^4 + px^2 + qx + r = 0 */
			sq_A = A * A;
			p = -3.0 / 8 * sq_A + B;
			q = 1.0 / 8 * sq_A * A - 1.0 / 2 * A * B + C;
			r = -3.0 / 256 * sq_A * sq_A + 1.0 / 16 * sq_A * B - 1.0 / 4 * A * C + D;

			if (IsZero(r)) {
				/* no absolute term: y(y^3 + py + q) = 0 */

				coeffs[3] = q;
				coeffs[2] = p;
				coeffs[1] = 0;
				coeffs[0] = 1;

				num = SolveCubic(coeffs[0], coeffs[1], coeffs[2], coeffs[3], out solution0, out solution1, out solution2);
			}
			else {
				/* solve the resolvent cubic ... */
				coeffs[3] = 1.0 / 2 * r * p - 1.0 / 8 * q * q;
				coeffs[2] = -r;
				coeffs[1] = -1.0 / 2 * p;
				coeffs[0] = 1;

				SolveCubic(coeffs[0], coeffs[1], coeffs[2], coeffs[3], out solution0, out solution1, out solution2);

				/* ... and take the one real solution ... */
				z = solution0;

				/* ... to build two quadric equations */
				u = z * z - r;
				v = 2 * z - p;

				if (IsZero(u))
					u = 0;
				else if (u > 0)
					u = Math.Sqrt(u);
				else
					return 0;

				if (IsZero(v))
					v = 0;
				else if (v > 0)
					v = Math.Sqrt(v);
				else
					return 0;

				coeffs[2] = z - u;
				coeffs[1] = q < 0 ? -v : v;
				coeffs[0] = 1;

				num = SolveQuadratic(coeffs[0], coeffs[1], coeffs[2], out solution0, out solution1);

				coeffs[2] = z + u;
				coeffs[1] = q < 0 ? v : -v;
				coeffs[0] = 1;

				if (num == 0) num += SolveQuadratic(coeffs[0], coeffs[1], coeffs[2], out solution0, out solution1);
				if (num == 1) num += SolveQuadratic(coeffs[0], coeffs[1], coeffs[2], out solution1, out solution2);
				if (num == 2) num += SolveQuadratic(coeffs[0], coeffs[1], coeffs[2], out solution2, out solution3);
			}

			/* resubstitute */
			sub = 1.0 / 4 * A;

			if (num > 0) solution0 -= sub;
			if (num > 1) solution1 -= sub;
			if (num > 2) solution2 -= sub;
			if (num > 3) solution3 -= sub;

			return num;
		}




	}
}
