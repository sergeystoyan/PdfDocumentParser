﻿//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Cliver.PdfDocumentParser
{
    internal class BooleanEngine
    {
        static public List<int> GetAnchorIds(string expression)
        {
            if (expression == null)
                return new List<int>();
            List<int> anchorIds = new List<int>();
            foreach (Match m in Regex.Matches(expression, @"\d+"))
                anchorIds.Add(int.Parse(m.Value));
            return anchorIds;
        }

        static public string GetFormatted(string expression)
        {
            if (expression == null)
                return null;
            expression = Regex.Replace(expression, @"\s", "", RegexOptions.Singleline);
            return Regex.Replace(expression, @"[\&\|]", " $0 ");
        }

        static public void Check(string expression, IEnumerable<int> anchorIds)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new Exception("Expression is empty.");
            {
                Match m = Regex.Match(expression, @"[^\s\d\(\)\&\|\!]", RegexOptions.IgnoreCase);
                if (m.Success)
                    throw new Exception("Expression contains unacceptable symbol: '" + m.Value + "'. Expected symbols: <anchor id>, '!', '&', '|', '(', ')'");
            }
            expression = Regex.Replace(expression, @"\d+", (Match m) =>
            {
                int ai = int.Parse(m.Value);
                if (!anchorIds.Contains(ai))
                    throw new Exception("Anchor[id=" + ai + "] does not exist.");
                return "T";
            });
            parseWithSubstitutedAnchorIds(expression);
        }

        //Sample expression: "1 | (2 & 3)"
        static public bool Parse(string expression, Page p)
        {
            expression = Regex.Replace(expression, @"\d+", (Match m) =>
            {
                return p.GetAnchorPoint0(int.Parse(m.Value)) != null ? "T" : "F";
            });
            return parseWithSubstitutedAnchorIds(expression);
        }

        static bool parseWithSubstitutedAnchorIds(string expression)
        {
            BooleanEngine be = new BooleanEngine();
            be.expression = Regex.Replace(expression, @"\s", "", RegexOptions.Singleline);
            be.move2NextToken();
            bool r = be.parse();
            if(!be.isEOS)
                throw new Exception("Expression could not be parsed to the end.");
            return r;
        }
        int position = -1;
        string expression;
        char currentToken;
        bool isEOS { get { return currentToken == '_'; } }

        void move2NextToken()
        {
            position++;
            if (position >= expression.Length)
            {
                currentToken = '_';
                return;
            }
            currentToken = expression[position];
        }

        bool parse()
        {
            while (!isEOS)
            {
                var isNegated = false;
                while (currentToken == '!')
                {
                    isNegated = !isNegated;
                    move2NextToken();
                }

                var boolean = parseBoolean();
                if (isNegated)
                    boolean = !boolean;

                while (currentToken == '|' || currentToken == '&')
                {
                    var operand = currentToken;
                    move2NextToken();
                    if (isEOS)
                        throw new Exception("Missing expression after operand");
                    var nextBoolean = parseBoolean();

                    if (operand == '&')
                        boolean = boolean && nextBoolean;
                    else
                        boolean = boolean || nextBoolean;
                }
                return boolean;
            }
            throw new Exception("Empty expression");
        }

        private bool parseBoolean()
        {
            if (currentToken == 'T' || currentToken == 'F')
            {
                bool value = currentToken == 'T';
                move2NextToken();
                return value;
            }
            if (currentToken == '(')
            {
                move2NextToken();
                bool value = parse();
                if (currentToken != ')')
                    throw new Exception("Closing parenthesis expected.");
                move2NextToken();
                return value;
            }
            if (currentToken == ')')
                throw new Exception("Unexpected closing parenthesis.");

            // since its not a BooleanConstant or Expression in parenthesis, it must be an expression again
            return parse();
        }
    }
}