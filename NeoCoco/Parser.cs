// Compiler Generator NeoCoco/R,
// Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
// extended by M. Loeberbauer & A. Woess, Univ. of Linz
// with improvements by Pat Terry, Rhodes University
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation; either version 2, or (at your option) any
// later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
// As an exception, it is allowed to write an extension of NeoCoco/R that is
// used as a plugin in non-free software.
//
// If not otherwise stated, any source code generated by NeoCoco/R (other than
// NeoCoco/R itself) does not fall under the GNU General Public License.
// ----------------------------------------------------------------------
using System;

namespace NeoCoco
{
	public partial class Parser
	{
		private const int minErrDist = 2;

		public Scanner scanner;
		public Errors errors;

		public Token lastToken;
		public Token lookaheadToken;
		int errDist = minErrDist;

		public Parser(Scanner scanner)
		{
			this.scanner = scanner;
			errors = new Errors();
		}

		void SynErr(int n)
		{
			if (errDist >= minErrDist)
			{
				errors.SynErr(lookaheadToken.line, lookaheadToken.col, n);
			}
			errDist = 0;
		}

		public void SemErr(string msg)
		{
			if (errDist >= minErrDist)
			{
				errors.SemErr(lastToken.line, lastToken.col, msg);
			}
			errDist = 0;
		}

		public void Parse()
		{
			lookaheadToken = new Token();
			Get();
			ParseRoot();
		}

		void Get()
		{
			for (; ; )
			{
				lastToken = lookaheadToken;
				lookaheadToken = scanner.Scan();
				if (lookaheadToken.kind <= MaxTerminal)
				{
					++errDist;
					break;
				}
				Pragmas();
				lookaheadToken = lastToken;
			}
		}

		void Expect(int n)
		{
			if (lookaheadToken.kind == n)
			{
				Get();
			}
			else
			{
				SynErr(n);
			}
		}

		bool StartOf(int s)
		{
			return set[s, lookaheadToken.kind];
		}

		void ExpectWeak(int n, int follow)
		{
			if (lookaheadToken.kind == n)
			{
				Get();
			}
			else
			{
				SynErr(n);
				while (!StartOf(follow))
				{
					Get();
				}
			}
		}

		bool WeakSeparator(int n, int syFol, int repFol)
		{
			int kind = lookaheadToken.kind;
			if (kind == n)
			{
				Get();
				return true;
			}
			else if (StartOf(repFol))
			{
				return false;
			}
			else
			{
				SynErr(n);
				while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind]))
				{
					Get();
					kind = lookaheadToken.kind;
				}
				return StartOf(syFol);
			}
		}
	}

	public partial class Errors
	{
		public int count = 0;                                    // number of errors detected
		public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
		public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

		public virtual void SemErr(int line, int col, string s)
		{
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}

		public virtual void SemErr(string s)
		{
			errorStream.WriteLine(s);
			count++;
		}

		public virtual void Warning(int line, int col, string s)
		{
			errorStream.WriteLine(errMsgFormat, line, col, s);
		}

		public virtual void Warning(string s)
		{
			errorStream.WriteLine(s);
		}
	}


	public class FatalError : Exception
	{
		public FatalError(string m) : base(m) { }
	}
}