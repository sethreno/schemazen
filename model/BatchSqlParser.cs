using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace model {

    enum State {
        Searching,
        InComment,
        InComment2,
        InBracket,
        InQuote
    }
        
    class BatchSqlParser {

        private static bool IsWhitespace(char c) {
            return Regex.Match(c.ToString(), "\\s", RegexOptions.Multiline).Success;
        }
        
        public static string[] SplitBatch(string batchSql) {
            var scripts = new List<string>();

            var state = State.Searching;
            bool foundGO = false;
            var commentDepth = 0; // depth of nested comment
            // next, current & previous chars
            char next = ' ';
            char c = ' ';
            char p = ' ';
            char p2 = ' ';
            int scriptStartIndex = 0;

            for(int i=0; i<batchSql.Length; i++){
                c = Char.ToUpper(batchSql[i]);
                if (batchSql.Length > i + 1) {
                    next = batchSql[i + 1];
                } else next = ' ';

                switch (state) {
                    case State.Searching:                        
                        if (c == '*' && p == '/') state = State.InComment2;
                        else if (c == '-' && p == '-') state = State.InComment;
                        else if (c == '[') state = State.InBracket;
                        else if (c == '\'') state = State.InQuote;
                        else if (c == 'O' && p == 'G') {
                            if (IsWhitespace(p2) && IsWhitespace(next)) foundGO = true;
                        }
                        break;
                        
                    case State.InComment:
                        if (c == '\n') state = State.Searching;
                        break;

                    case State.InComment2:
                        if (c == '*' && p == '/') commentDepth++;
                        else if (c == '/' && p == '*') commentDepth--;
                        if (commentDepth < 0) {
                            commentDepth = 0;
                            state = State.Searching;
                        }
                        break;

                    case State.InBracket:
                        if (c == ']') state = State.Searching;
                        break;

                    case State.InQuote:
                        if (c == '\'') state = State.Searching;
                        break;
                }

                if (foundGO) {
                    // store the current script and continue searching
                    var length = i - scriptStartIndex - 1;
                    scripts.Add(batchSql.Substring(scriptStartIndex, length));
                    scriptStartIndex = i + 1;
                    foundGO = false;
                    
                } else if (i == batchSql.Length - 1) {
                    // end of batch
                    var length = i - scriptStartIndex + 1;
                    scripts.Add(batchSql.Substring(scriptStartIndex, length));
                }

                p2 = p;
                p = c;                
            }

            // return scripts that contain non-whitespace
            var scriptsOut = new List<string>();
            foreach (string s in scripts) {
                if (Regex.Match(s, "\\S", RegexOptions.Multiline).Success) {
                    scriptsOut.Add(s);
                }
            }
            return scriptsOut.ToArray();
        }
    }
}
