using System.IO;



using System;

namespace NeoCoco {



public partial class Parser
{
	public const int MaxTerminal = 41;
	public const int _ddtSym = 42;
	public const int _optionSym = 43;


const int IdentifierKind = 0;
	const int StringKind = 1;

	public TextWriter trace;    // other NeoCoco objects referenced in this ATG
	public Tab tab;
	public DFA dfa;
	public ParserGenerator pgen;

	bool   genScanner;
	string tokenString;         // used in declarations of literal tokens
	string noString = "-none-"; // used in declarations of literal tokens

/*-------------------------------------------------------------------------*/



	void Pragmas ()
	{
		if (lookaheadToken.kind == 42) {
				tab.SetDDT(lookaheadToken.val); 
		}
		if (lookaheadToken.kind == 43) {
				tab.SetOption(lookaheadToken.val); 
		}

	}

	void NeoCoco() {
		Symbol sym; Graph g, g1, g2; string gramName; CharSet s; int beg, line; 
		if (StartOf(1)) {
			Get();
			beg = lastToken.pos; line = lastToken.line; 
			while (StartOf(2)) {
				Get();
			}
			pgen.usingPos = new Position(beg, lookaheadToken.pos, 0, line); 
		}
		Expect(6 /* COMPILER */);
		genScanner = true; 
		Expect(1 /* ident */);
		gramName = lastToken.val;
		beg = lookaheadToken.pos; line = lookaheadToken.line;
		
		while (StartOf(3)) {
			Get();
		}
		tab.semDeclPos = new Position(beg, lookaheadToken.pos, 0, line); 
		if (lookaheadToken.kind == 7 /* IGNORECASE */) {
			Get();
			dfa.ignoreCase = true; 
		}
		if (lookaheadToken.kind == 8 /* CHARACTERS */) {
			Get();
			while (lookaheadToken.kind == 1 /* ident */) {
				SetDecl();
			}
		}
		if (lookaheadToken.kind == 9 /* TOKENS */) {
			Get();
			while (lookaheadToken.kind == 1 /* ident */ || lookaheadToken.kind == 3 /* string */ || lookaheadToken.kind == 5 /* char */) {
				TokenDecl(NodeType.Terminal);
			}
		}
		if (lookaheadToken.kind == 10 /* PRAGMAS */) {
			Get();
			while (lookaheadToken.kind == 1 /* ident */ || lookaheadToken.kind == 3 /* string */ || lookaheadToken.kind == 5 /* char */) {
				TokenDecl(NodeType.Pragma);
			}
		}
		while (lookaheadToken.kind == 11 /* COMMENTS */) {
			Get();
			bool nested = false; 
			Expect(12 /* FROM */);
			TokenExpr(out g1);
			Expect(13 /* TO */);
			TokenExpr(out g2);
			if (lookaheadToken.kind == 14 /* NESTED */) {
				Get();
				nested = true; 
			}
			dfa.NewComment(g1.Left, g2.Left, nested); 
		}
		while (lookaheadToken.kind == 15 /* IGNORE */) {
			Get();
			Set(out s);
			tab.Ignored.Or(s); 
		}
		while (!(lookaheadToken.kind == 0 /* EOF */ || lookaheadToken.kind == 16 /* PRODUCTIONS */)) {SynErr(42); Get();}
		Expect(16 /* PRODUCTIONS */);
		if (genScanner) dfa.MakeDeterministic();
		tab.DeleteNodes();
		
		while (lookaheadToken.kind == 1 /* ident */) {
			Get();
			var undef = !tab.TryFindSymbol(lastToken.val, out sym);
			if (undef)
			{
			  sym = tab.NewSym(NodeType.NonTerminal, lastToken.val, lastToken.line);
			}
			else
			{
			  if (sym.Type == NodeType.NonTerminal)
			  {
			    if (sym.graph != null) SemErr("name declared twice");
			  }
			  else
			  {
			    SemErr("this symbol kind not allowed on left side of production");
			  }
			  sym.line = lastToken.line;
			}
			bool noAttrs = sym.attrPos == null;
			sym.attrPos = null;
			
			if (lookaheadToken.kind == 24 /* < */ || lookaheadToken.kind == 26 /* <. */) {
				AttrDecl(sym);
			}
			if (!undef)
			 if (noAttrs != (sym.attrPos == null))
			   SemErr("attribute mismatch between declaration and use of this symbol");
			
			if (lookaheadToken.kind == 39 /* (. */) {
				SemText(out sym.semPos);
			}
			ExpectWeak(17 /* = */, 4);
			Expression(out g);
			sym.graph = g.Left;
			tab.Finish(g);
			
			ExpectWeak(18 /* . */, 5);
		}
		Expect(19 /* END */);
		Expect(1 /* ident */);
		if (gramName != lastToken.val)
		 SemErr("name does not match grammar name");
		
		if (!tab.TryFindSymbol(gramName, out tab.gramSy))
		 SemErr("missing production for grammar name");
		else {
		 sym = tab.gramSy;
		 if (sym.attrPos != null)
		   SemErr("grammar symbol must not have attributes");
		}
		tab.noSym = tab.NewSym(NodeType.Terminal, "???", 0); // noSym gets highest number
		tab.SetupAnys();
		tab.RenumberPragmas();
		if (tab.ddt[2]) tab.PrintNodes();
		if (errors.count == 0) {
		 tab.CompSymbolSets();
		 if (tab.ddt[7]) tab.XRef();
		 if (tab.GrammarOk()) {
		   pgen.WriteParser();
		   if (genScanner) {
		     dfa.WriteScanner();
		     if (tab.ddt[0]) dfa.PrintStates();
		   }
		   if (tab.ddt[8]) pgen.WriteStatistics();
		 }
		}
		if (tab.ddt[6]) tab.PrintSymbolTable();
		
		Expect(18 /* . */);
	}

	void SetDecl() {
		CharSet s; 
		Expect(1 /* ident */);
		string name = lastToken.val;
		CharClass c = tab.FindCharClass(name);
		if (c != null) SemErr("name declared twice");
		
		Expect(17 /* = */);
		Set(out s);
		if (s.Elements() == 0) SemErr("character set must not be empty");
		tab.NewCharClass(name, s);
		
		Expect(18 /* . */);
	}

	void TokenDecl(NodeType type) {
		string name; int kind; Symbol sym; Graph g; 
		Sym(out name, out kind);
		if (tab.TryFindSymbol(name, out sym))
		{
		 SemErr("name declared twice");
		}
		else
		{
		 sym = tab.NewSym(type, name, lastToken.line);
		 sym.tokenKind = Symbol.fixedToken;
		}
		tokenString = null;
		
		while (!(StartOf(6))) {SynErr(43); Get();}
		if (lookaheadToken.kind == 17 /* = */) {
			Get();
			TokenExpr(out g);
			Expect(18 /* . */);
			if (kind == StringKind) SemErr("a literal must not be declared with a structure");
			tab.Finish(g);
			if (tokenString == null || tokenString.Equals(noString))
			 dfa.ConvertToStates(g.Left, sym);
			else // TokenExpr is a single string
			{
			 if (tab.literals.ContainsKey(tokenString))
			   SemErr("token string declared twice");
			 tab.literals[tokenString] = sym;
			 dfa.MatchLiteral(tokenString, sym);
			}
			
		} else if (StartOf(7)) {
			if (kind == IdentifierKind) genScanner = false;
			else dfa.MatchLiteral(sym.Name, sym);
			
		} else SynErr(44);
		if (lookaheadToken.kind == 39 /* (. */) {
			SemText(out sym.semPos);
			if (type != NodeType.Pragma) SemErr("semantic action not allowed here"); 
		}
	}

	void TokenExpr(out Graph g) {
		Graph g2; 
		TokenTerm(out g);
		bool first = true; 
		while (WeakSeparator(28,8,9) ) {
			TokenTerm(out g2);
			if (first) { tab.MakeFirstAlt(g); first = false; }
			tab.MakeAlternative(g, g2);
			
		}
	}

	void Set(out CharSet s) {
		CharSet s2; 
		SimSet(out s);
		while (lookaheadToken.kind == 20 /* + */ || lookaheadToken.kind == 21 /* - */) {
			if (lookaheadToken.kind == 20 /* + */) {
				Get();
				SimSet(out s2);
				s.Or(s2); 
			} else {
				Get();
				SimSet(out s2);
				s.Subtract(s2); 
			}
		}
	}

	void AttrDecl(Symbol sym) {
		if (lookaheadToken.kind == 24 /* < */) {
			Get();
			var beg = lookaheadToken.pos; var col = lookaheadToken.col; var line = lookaheadToken.line; 
			while (StartOf(10)) {
				if (StartOf(11)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
			}
			Expect(25 /* > */);
			if (lastToken.pos > beg)
			 sym.attrPos = new Position(beg, lastToken.pos, col, line); 
		} else if (lookaheadToken.kind == 26 /* <. */) {
			Get();
			var beg = lookaheadToken.pos; var col = lookaheadToken.col; var line = lookaheadToken.line; 
			while (StartOf(12)) {
				if (StartOf(13)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
			}
			Expect(27 /* .> */);
			if (lastToken.pos > beg)
			 sym.attrPos = new Position(beg, lastToken.pos, col, line); 
		} else SynErr(45);
	}

	void SemText(out Position pos) {
		Expect(39 /* (. */);
		var beg = lookaheadToken.pos; var col = lookaheadToken.col; var line = lookaheadToken.line; 
		while (StartOf(14)) {
			if (StartOf(15)) {
				Get();
			} else if (lookaheadToken.kind == 4 /* badString */) {
				Get();
				SemErr("bad string in semantic action"); 
			} else {
				Get();
				SemErr("missing end of previous semantic action"); 
			}
		}
		Expect(40 /* .) */);
		pos = new Position(beg, lastToken.pos, col, line); 
	}

	void Expression(out Graph g) {
		Graph g2; 
		Term(out g);
		bool first = true; 
		while (WeakSeparator(28,16,17) ) {
			Term(out g2);
			if (first) { tab.MakeFirstAlt(g); first = false; }
			tab.MakeAlternative(g, g2);
			
		}
	}

	void SimSet(out CharSet s) {
		int n1, n2; 
		s = new CharSet(); 
		if (lookaheadToken.kind == 1 /* ident */) {
			Get();
			var c = tab.FindCharClass(lastToken.val);
			if (c == null) SemErr("undefined name"); else s.Or(c.Set);
			
		} else if (lookaheadToken.kind == 3 /* string */) {
			Get();
			var name = lastToken.val;
			name = tab.Unescape(name.Substring(1, name.Length-2));
			foreach (char ch in name)
			{
			 s.Set(dfa.ignoreCase ? char.ToLower(ch) : ch);
			}
			
		} else if (lookaheadToken.kind == 5 /* char */) {
			Char(out n1);
			s.Set(n1); 
			if (lookaheadToken.kind == 22 /* .. */) {
				Get();
				Char(out n2);
				for (int i = n1; i <= n2; i++) s.Set(i); 
			}
		} else if (lookaheadToken.kind == 23 /* ANY */) {
			Get();
			s = new CharSet(); s.Fill(); 
		} else SynErr(46);
	}

	void Char(out int n) {
		Expect(5 /* char */);
		string name = lastToken.val; n = 0;
		name = tab.Unescape(name.Substring(1, name.Length-2));
		if (name.Length == 1) n = name[0];
		else SemErr("unacceptable character value");
		if (dfa.ignoreCase && (char)n >= 'A' && (char)n <= 'Z') n += 32;
		
	}

	void Sym(out string name, out int kind) {
		name = "???"; kind = IdentifierKind; 
		if (lookaheadToken.kind == 1 /* ident */) {
			Get();
			kind = IdentifierKind; name = lastToken.val; 
		} else if (lookaheadToken.kind == 3 /* string */ || lookaheadToken.kind == 5 /* char */) {
			if (lookaheadToken.kind == 3 /* string */) {
				Get();
				name = lastToken.val; 
			} else {
				Get();
				name = "\"" + lastToken.val.Substring(1, lastToken.val.Length-2) + "\""; 
			}
			kind = StringKind;
			if (dfa.ignoreCase) name = name.ToLower();
			if (name.IndexOf(' ') >= 0)
			 SemErr("literal tokens must not contain blanks"); 
		} else SynErr(47);
	}

	void Term(out Graph g) {
		Graph g2; Node rslv = null; g = null; 
		if (StartOf(18)) {
			if (lookaheadToken.kind == 37 /* IF */) {
				rslv = tab.NewNode(NodeType.ResolverExpr, null, lookaheadToken.line); 
				Resolver(out rslv.pos);
				g = new Graph(rslv); 
			}
			Factor(out g2);
			if (rslv != null) tab.MakeSequence(g, g2);
			else g = g2;
			
			while (StartOf(19)) {
				Factor(out g2);
				tab.MakeSequence(g, g2); 
			}
		} else if (StartOf(20)) {
			g = new Graph(tab.NewNode(NodeType.Empty, null, 0)); 
		} else SynErr(48);
		if (g == null) // invalid start of Term
		 g = new Graph(tab.NewNode(NodeType.Empty, null, 0));
		
	}

	void Resolver(out Position pos) {
		Expect(37 /* IF */);
		Expect(30 /* ( */);
		int beg = lookaheadToken.pos; int col = lookaheadToken.col; int line = lookaheadToken.line; 
		Condition();
		pos = new Position(beg, lastToken.pos, col, line); 
	}

	void Factor(out Graph g) {
		string name; int kind; Position pos; bool weak = false;
		g = null;
		
		switch (lookaheadToken.kind) {
		case 1: /*  ident */
		case 3: /*  string */
		case 5: /*  char */
		case 29: /*  WEAK */
		{
			if (lookaheadToken.kind == 29 /* WEAK */) {
				Get();
				weak = true; 
			}
			Sym(out name, out kind);
			var undef = !tab.TryFindSymbol(name, out var sym);
			if (undef)
			{
			  if (kind == StringKind)
			  {
			    tab.literals.TryGetValue(name, out sym);
			  }
			  if (kind == IdentifierKind)
			  {
			    sym = tab.NewSym(NodeType.NonTerminal, name, 0);  // forward nt
			  }
			  else if (genScanner)
			  {
			    sym = tab.NewSym(NodeType.Terminal, name, lastToken.line);
			    dfa.MatchLiteral(sym.Name, sym);
			  }
			  else
			  {  // undefined string in production
			    SemErr("undefined string in production");
			    sym = tab.eofSy;  // dummy
			  }
			}
			var type = sym.Type;
			if (type != NodeType.Terminal && type != NodeType.NonTerminal)
			 SemErr("this symbol kind is not allowed in a production");
			if (weak)
			 if (type == NodeType.Terminal) type = NodeType.WeakTerminal;
			 else SemErr("only terminals may be weak");
			Node p = tab.NewNode(type, sym, lastToken.line);
			g = new Graph(p);
			
			if (lookaheadToken.kind == 24 /* < */ || lookaheadToken.kind == 26 /* <. */) {
				Attribs(p);
				if (kind != IdentifierKind) SemErr("a literal must not have attributes"); 
			}
			if (undef)
			 sym.attrPos = p.pos;  // dummy
			else if ((p.pos == null) != (sym.attrPos == null))
			 SemErr("attribute mismatch between declaration and use of this symbol");
			
			break;
		}
		case 30: /*  ( */
		{
			Get();
			Expression(out g);
			Expect(31 /* ) */);
			break;
		}
		case 32: /*  [ */
		{
			Get();
			Expression(out g);
			Expect(33 /* ] */);
			tab.MakeOption(g); 
			break;
		}
		case 34: /*  { */
		{
			Get();
			Expression(out g);
			Expect(35 /* } */);
			tab.MakeIteration(g); 
			break;
		}
		case 39: /*  (. */
		{
			SemText(out pos);
			Node p = tab.NewNode(NodeType.SemanticAction, null, 0);
			p.pos = pos;
			g = new Graph(p);
			
			break;
		}
		case 23: /*  ANY */
		{
			Get();
			Node p = tab.NewNode(NodeType.Any, null, 0);  // p.set is set in tab.SetupAnys
			g = new Graph(p);
			
			break;
		}
		case 36: /*  SYNC */
		{
			Get();
			Node p = tab.NewNode(NodeType.Synchronization, null, 0);
			g = new Graph(p);
			
			break;
		}
		default: SynErr(49); break;
		}
		if (g == null) // invalid start of Factor
		 g = new Graph(tab.NewNode(NodeType.Empty, null, 0));
		
	}

	void Attribs(Node p) {
		if (lookaheadToken.kind == 24 /* < */) {
			Get();
			var beg = lookaheadToken.pos; var col = lookaheadToken.col; var line = lookaheadToken.line; 
			while (StartOf(21)) {
				if (StartOf(22)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
			}
			Expect(25 /* > */);
			if (lastToken.pos > beg) p.pos = new Position(beg, lastToken.pos, col, line); 
		} else if (lookaheadToken.kind == 26 /* <. */) {
			Get();
			var beg = lookaheadToken.pos; var col = lookaheadToken.col; var line = lookaheadToken.line; 
			while (StartOf(23)) {
				if (StartOf(24)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
			}
			Expect(27 /* .> */);
			if (lastToken.pos > beg) p.pos = new Position(beg, lastToken.pos, col, line); 
		} else SynErr(50);
	}

	void Condition() {
		while (StartOf(25)) {
			if (lookaheadToken.kind == 30 /* ( */) {
				Get();
				Condition();
			} else {
				Get();
			}
		}
		Expect(31 /* ) */);
	}

	void TokenTerm(out Graph g) {
		Graph g2; 
		TokenFactor(out g);
		while (StartOf(26)) {
			TokenFactor(out g2);
			tab.MakeSequence(g, g2); 
		}
		if (lookaheadToken.kind == 38 /* CONTEXT */) {
			Get();
			Expect(30 /* ( */);
			TokenExpr(out g2);
			tab.SetContextTrans(g2.Left); dfa.hasCtxMoves = true;
			tab.MakeSequence(g, g2); 
			Expect(31 /* ) */);
		}
	}

	void TokenFactor(out Graph g) {
		string name; int kind; 
		g = null; 
		if (lookaheadToken.kind == 1 /* ident */ || lookaheadToken.kind == 3 /* string */ || lookaheadToken.kind == 5 /* char */) {
			Sym(out name, out kind);
			if (kind == IdentifierKind) {
			 var c = tab.FindCharClass(name);
			 if (c == null) {
			   SemErr("undefined name");
			   c = tab.NewCharClass(name, new CharSet());
			 }
			 var p = tab.NewNode(NodeType.Class, null, 0); p.val = c.N;
			 g = new Graph(p);
			 tokenString = noString;
			} else { // StringKind
			 g = tab.StrToGraph(name);
			 if (tokenString == null) tokenString = name;
			 else tokenString = noString;
			}
			
		} else if (lookaheadToken.kind == 30 /* ( */) {
			Get();
			TokenExpr(out g);
			Expect(31 /* ) */);
		} else if (lookaheadToken.kind == 32 /* [ */) {
			Get();
			TokenExpr(out g);
			Expect(33 /* ] */);
			tab.MakeOption(g); tokenString = noString; 
		} else if (lookaheadToken.kind == 34 /* { */) {
			Get();
			TokenExpr(out g);
			Expect(35 /* } */);
			tab.MakeIteration(g); tokenString = noString; 
		} else SynErr(51);
		if (g == null) // invalid start of TokenFactor
		 g = new Graph(tab.NewNode(NodeType.Empty, null, 0)); 
	}



	private void ParseRoot()
	{
		NeoCoco();
		Expect(0);

	}

	static readonly bool[,] set = {
		{true, true, false, true,  false, true, false, false,  false, false, true, true,  false, false, false, true,  true, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, false, false},
		{false, true, true, true,  true, true, false, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  true, true, false, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  true, true, true, false,  false, false, false, false,  true, true, true, false,  false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{true, true, false, true,  false, true, false, false,  false, false, true, true,  false, false, false, true,  true, true, true, false,  false, false, false, true,  false, false, false, false,  true, true, true, false,  true, false, true, false,  true, true, false, true,  false, false, false},
		{true, true, false, true,  false, true, false, false,  false, false, true, true,  false, false, false, true,  true, true, false, true,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, false, false},
		{true, true, false, true,  false, true, false, false,  false, false, true, true,  false, false, false, true,  true, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, false, false},
		{false, true, false, true,  false, true, false, false,  false, false, true, true,  false, false, false, true,  true, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, false, false},
		{false, true, false, true,  false, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, true, false,  true, false, true, false,  false, false, false, false,  false, false, false},
		{false, false, false, false,  false, false, false, false,  false, false, false, true,  false, true, true, true,  true, false, true, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, true, false, true,  false, false, false, false,  false, false, false},
		{false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, false, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, false, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, false,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, false,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  false, true, false},
		{false, true, true, true,  false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, false,  false, true, false},
		{false, true, false, true,  false, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, true, false,  false, false, false, true,  false, false, false, false,  true, true, true, true,  true, true, true, true,  true, true, false, true,  false, false, false},
		{false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, true, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, true, false, true,  false, false, false, false,  false, false, false},
		{false, true, false, true,  false, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, false, false, false,  false, true, true, false,  true, false, true, false,  true, true, false, true,  false, false, false},
		{false, true, false, true,  false, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, true,  false, false, false, false,  false, true, true, false,  true, false, true, false,  true, false, false, true,  false, false, false},
		{false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, true, false,  false, false, false, false,  false, false, false, false,  true, false, false, true,  false, true, false, true,  false, false, false, false,  false, false, false},
		{false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, false, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, false, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, false,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, false,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, true,  true, true, true, false,  true, true, true, true,  true, true, true, true,  true, true, false},
		{false, true, false, true,  false, true, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, false, false,  false, false, true, false,  true, false, true, false,  false, false, false, false,  false, false, false}
	};
}


public partial class Errors
{
	public virtual void SynErr (int line, int col, int n)
	{
		string s;
		switch (n)
		{
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "number expected"; break;
			case 3: s = "string expected"; break;
			case 4: s = "badString expected"; break;
			case 5: s = "char expected"; break;
			case 6: s = "\"COMPILER\" expected"; break;
			case 7: s = "\"IGNORECASE\" expected"; break;
			case 8: s = "\"CHARACTERS\" expected"; break;
			case 9: s = "\"TOKENS\" expected"; break;
			case 10: s = "\"PRAGMAS\" expected"; break;
			case 11: s = "\"COMMENTS\" expected"; break;
			case 12: s = "\"FROM\" expected"; break;
			case 13: s = "\"TO\" expected"; break;
			case 14: s = "\"NESTED\" expected"; break;
			case 15: s = "\"IGNORE\" expected"; break;
			case 16: s = "\"PRODUCTIONS\" expected"; break;
			case 17: s = "\"=\" expected"; break;
			case 18: s = "\".\" expected"; break;
			case 19: s = "\"END\" expected"; break;
			case 20: s = "\"+\" expected"; break;
			case 21: s = "\"-\" expected"; break;
			case 22: s = "\"..\" expected"; break;
			case 23: s = "\"ANY\" expected"; break;
			case 24: s = "\"<\" expected"; break;
			case 25: s = "\">\" expected"; break;
			case 26: s = "\"<.\" expected"; break;
			case 27: s = "\".>\" expected"; break;
			case 28: s = "\"|\" expected"; break;
			case 29: s = "\"WEAK\" expected"; break;
			case 30: s = "\"(\" expected"; break;
			case 31: s = "\")\" expected"; break;
			case 32: s = "\"[\" expected"; break;
			case 33: s = "\"]\" expected"; break;
			case 34: s = "\"{\" expected"; break;
			case 35: s = "\"}\" expected"; break;
			case 36: s = "\"SYNC\" expected"; break;
			case 37: s = "\"IF\" expected"; break;
			case 38: s = "\"CONTEXT\" expected"; break;
			case 39: s = "\"(.\" expected"; break;
			case 40: s = "\".)\" expected"; break;
			case 41: s = "??? expected"; break;
			case 42: s = "this symbol not expected in NeoCoco"; break;
			case 43: s = "this symbol not expected in TokenDecl"; break;
			case 44: s = "invalid TokenDecl"; break;
			case 45: s = "invalid AttrDecl"; break;
			case 46: s = "invalid SimSet"; break;
			case 47: s = "invalid Sym"; break;
			case 48: s = "invalid Term"; break;
			case 49: s = "invalid Factor"; break;
			case 50: s = "invalid Attribs"; break;
			case 51: s = "invalid TokenFactor"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
}
}