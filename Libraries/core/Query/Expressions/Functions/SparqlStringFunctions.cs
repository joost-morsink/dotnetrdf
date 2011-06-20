﻿/*

Copyright Robert Vesse 2009-10
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

If this license is not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;

namespace VDS.RDF.Query.Expressions.Functions
{
    /// <summary>
    /// Abstract Base Class for SPARQL String Testing functions which take two arguments
    /// </summary>
    public abstract class BaseBinarySparqlStringFunction : BaseBinaryExpression
    {
        /// <summary>
        /// Creates a new Base Binary SPARQL String Function
        /// </summary>
        /// <param name="stringExpr">String Expression</param>
        /// <param name="argExpr">Argument Expression</param>
        public BaseBinarySparqlStringFunction(ISparqlExpression stringExpr, ISparqlExpression argExpr)
            : base(stringExpr, argExpr) { }

        /// <summary>
        /// Calculates the Effective Boolean Value of the Function in the given Context for the given Binding
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public override bool EffectiveBooleanValue(SparqlEvaluationContext context, int bindingID)
        {
            INode x = this._leftExpr.Value(context, bindingID);
            INode y = this._rightExpr.Value(context, bindingID);

            if (x.NodeType == NodeType.Literal && y.NodeType == NodeType.Literal)
            {
                ILiteralNode stringLit = (ILiteralNode)x;
                ILiteralNode argLit = (ILiteralNode)y;

                if (IsValidArgumentPair(stringLit, argLit))
                {
                    return this.ValueInternal(stringLit, argLit);
                }
                else
                {
                    throw new RdfQueryException("The Literals provided as arguments to this SPARQL String function are not of valid forms (see SPARQL spec for acceptable combinations)");
                }
            }
            else
            {
                throw new RdfQueryException("Arguments to a SPARQL String function must both be Literal Nodes");
            }
        }

        /// <summary>
        /// Abstract method that child classes must implement to 
        /// </summary>
        /// <param name="stringLit"></param>
        /// <param name="argLit"></param>
        /// <returns></returns>
        protected abstract bool ValueInternal(ILiteralNode stringLit, ILiteralNode argLit);

        /// <summary>
        /// Determines whether the Arguments are valid
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <param name="argLit">Argument Literal</param>
        /// <returns></returns>
        private bool IsValidArgumentPair(ILiteralNode stringLit, ILiteralNode argLit)
        {
            if (stringLit.DataType != null)
            {
                //If 1st argument has a DataType must be an xsd:string or not valid
                if (!stringLit.DataType.ToString().Equals(XmlSpecsHelper.XmlSchemaDataTypeString)) return false;

                if (argLit.DataType != null)
                {
                    //If 2nd argument also has a DataType must also be an xsd:string or not valid
                    if (!argLit.DataType.ToString().Equals(XmlSpecsHelper.XmlSchemaDataTypeString)) return false;
                    return true;
                }
                else if (argLit.Language.Equals(String.Empty))
                {
                    //If 2nd argument does not have a DataType but 1st does then 2nd argument must have no
                    //Language Tag
                    return true;
                }
                else
                {
                    //2nd argument does not have a DataType but 1st does BUT 2nd has a Language Tag so invalid
                    return false;
                }
            }
            else if (!stringLit.Language.Equals(String.Empty))
            {
                if (argLit.DataType != null)
                {
                    //If 1st argument has a Language Tag and 2nd Argument is typed then must be xsd:string
                    //to be valid
                    return argLit.DataType.ToString().Equals(XmlSpecsHelper.XmlSchemaDataTypeString);
                }
                else if (argLit.Language.Equals(String.Empty) || stringLit.Language.Equals(argLit.Language))
                {
                    //If 1st argument has a Language Tag then 2nd Argument must have same Language Tag 
                    //or no Language Tag in order to be valid
                    return true;
                }
                else
                {
                    //Otherwise Invalid
                    return false;
                }
            }
            else
            {
                if (argLit.DataType != null)
                {
                    //If 1st argument is plain literal then 2nd argument must be xsd:string if typed
                    return argLit.DataType.ToString().Equals(XmlSpecsHelper.XmlSchemaDataTypeString);
                }
                else if (argLit.Language.Equals(String.Empty))
                {
                    //If 1st argument is plain literal then 2nd literal cannot have a language tag to be valid
                    return true;
                }
                else 
                {
                    //If 1st argument is plain literal and 2nd has language tag then invalid
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the Expression Type
        /// </summary>
        public override SparqlExpressionType Type
        {
            get 
            {
                return SparqlExpressionType.Function; 
            }
        }
    }

    /// <summary>
    /// Represents the SPARQL CONCAT function
    /// </summary>
    public class ConcatFunction : ISparqlExpression
    {
        private List<ISparqlExpression> _exprs = new List<ISparqlExpression>();

        /// <summary>
        /// Creates a new SPARQL Concatenation function
        /// </summary>
        /// <param name="expressions">Enumeration of expressions</param>
        public ConcatFunction(IEnumerable<ISparqlExpression> expressions)
        {
            this._exprs.AddRange(expressions);
        }

        /// <summary>
        /// Gets the Value of the function as evaluated in the given Context for the given Binding ID
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public INode Value(SparqlEvaluationContext context, int bindingID)
        {
            String langTag = null;
            bool allString = true;
            bool allSameTag = true;

            StringBuilder output = new StringBuilder();
            foreach (ISparqlExpression expr in this._exprs)
            {
                INode temp = expr.Value(context, bindingID);
                if (temp == null) throw new RdfQueryException("Cannot evaluate the SPARQL CONCAT() function when an argument evaluates to a Null");

                switch (temp.NodeType)
                {
                    case NodeType.Literal:
                        //Check whether the Language Tags and Types are the same
                        //We need to do this so that we can produce the appropriate output
                        ILiteralNode lit = (ILiteralNode)temp;
                        if (langTag == null)
                        {
                            langTag = lit.Language;
                        }
                        else
                        {
                            allSameTag = allSameTag && (langTag.Equals(lit.Language));
                        }

                        //Have to ensure that if Typed is an xsd:string
                        if (lit.DataType != null && !lit.DataType.ToString().Equals(XmlSpecsHelper.XmlSchemaDataTypeString)) throw new RdfQueryException("Cannot evaluate the SPARQL CONCAT() function when an argument is a Typed Literal which is not an xsd:string");
                        allString = allString && lit.DataType != null;

                        output.Append(lit.Value);
                        break;

                    default:
                        throw new RdfQueryException("Cannot evaluate the SPARQL CONCAT() function when an argument is not a Literal Node");
                }
            }

            //Produce the appropriate literal form depending on our inputs
            if (allString)
            {
                return new LiteralNode(null, output.ToString(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeString));
            }
            else if (allSameTag)
            {
                return new LiteralNode(null, output.ToString(), langTag);
            }
            else
            {
                return new LiteralNode(null, output.ToString());
            }
        }

        /// <summary>
        /// Gets the Effective Boolean Value of the function as evaluated in the given Context for the given Binding ID
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public bool EffectiveBooleanValue(SparqlEvaluationContext context, int bindingID)
        {
            return SparqlSpecsHelper.EffectiveBooleanValue(this.Value(context, bindingID));
        }

        /// <summary>
        /// Gets the Arguments the function applies to
        /// </summary>
        public IEnumerable<ISparqlExpression> Arguments
        {
            get 
            {
                return this._exprs;
            }
        }

        /// <summary>
        /// Gets the Variables used in the function
        /// </summary>
        public IEnumerable<string> Variables
        {
            get 
            {
                return (from expr in this._exprs
                        from v in expr.Variables
                        select v);
            }
        }

        /// <summary>
        /// Gets the String representation of the function
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append(SparqlSpecsHelper.SparqlKeywordConcat);
            output.Append('(');
            for (int i = 0; i < this._exprs.Count; i++)
            {
                output.Append(this._exprs[i].ToString());
                if (i < this._exprs.Count - 1) output.Append(", ");
            }
            output.Append(")");
            return output.ToString();
        }

        /// <summary>
        /// Gets the Type of the SPARQL Expression
        /// </summary>
        public SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.Function;
            }
        }

        /// <summary>
        /// Gets the Functor of the expression
        /// </summary>
        public string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordConcat;
            }
        }

        public ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new ConcatFunction(this._exprs.Select(e => transformer.Transform(e)));
        }
    }

    /// <summary>
    /// Represents the SPARQL CONTAINS function
    /// </summary>
    public class ContainsFunction : BaseBinarySparqlStringFunction
    {
        /// <summary>
        /// Creates a new SPARQL CONTAINS function
        /// </summary>
        /// <param name="stringExpr">String Expression</param>
        /// <param name="searchExpr">Search Expression</param>
        public ContainsFunction(ISparqlExpression stringExpr, ISparqlExpression searchExpr)
            : base(stringExpr, searchExpr) { }

        /// <summary>
        /// Determines whether the String contains the given Argument
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <param name="argLit">Argument Literal</param>
        /// <returns></returns>
        protected override bool ValueInternal(ILiteralNode stringLit, ILiteralNode argLit)
        {
            return stringLit.Value.Contains(argLit.Value);
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get 
            {
                return SparqlSpecsHelper.SparqlKeywordContains;
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordContains + "(" + this._leftExpr.ToString() + ", " + this._rightExpr.ToString() + ")";
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new ContainsFunction(transformer.Transform(this._leftExpr), transformer.Transform(this._rightExpr));
        }
    }

    /// <summary>
    /// Represents the SPARQL ENCODE_FOR_URI Function
    /// </summary>
    public class EncodeForUriFunction : BaseUnaryXPathStringFunction
    {
        /// <summary>
        /// Creates a new Encode for URI function
        /// </summary>
        /// <param name="stringExpr">Expression</param>
        public EncodeForUriFunction(ISparqlExpression stringExpr)
            : base(stringExpr) { }

        /// <summary>
        /// Gets the Value of the function as applied to the given String Literal
        /// </summary>
        /// <param name="stringLit">Simple/String typed Literal</param>
        /// <returns></returns>
        protected override INode ValueInternal(ILiteralNode stringLit)
        {
            return new LiteralNode(null, Uri.EscapeUriString(stringLit.Value));
        }

        /// <summary>
        /// Gets the String representation of the function
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordEncodeForUri + "(" + this._expr.ToString() + ")";
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get 
            {
                return SparqlSpecsHelper.SparqlKeywordEncodeForUri;
            }
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new EncodeForUriFunction(transformer.Transform(this._expr));
        }
    }

    /// <summary>
    /// Represents the SPARQL LCASE Function
    /// </summary>
    public class LCaseFunction : BaseUnaryXPathStringFunction
    {
        /// <summary>
        /// Creates a new LCASE function
        /// </summary>
        /// <param name="expr">Argument Expression</param>
        public LCaseFunction(ISparqlExpression expr)
            : base(expr) { }

        /// <summary>
        /// Calculates
        /// </summary>
        /// <param name="stringLit"></param>
        /// <returns></returns>
        protected override INode ValueInternal(ILiteralNode stringLit)
        {
            if (stringLit.DataType != null)
            {
                return new LiteralNode(null, stringLit.Value.ToLower(), stringLit.DataType);
            }
            else
            {
                return new LiteralNode(null, stringLit.Value.ToLower(), stringLit.Language);
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordLCase;
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordLCase + "(" + this._expr.ToString() + ")";
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new LCaseFunction(transformer.Transform(this._expr));
        }
    }

    /// <summary>
    /// Represents the SPARQL STRENDS Function
    /// </summary>
    public class StrEndsFunction : BaseBinarySparqlStringFunction
    {
        /// <summary>
        /// Creates a new STRENDS() function
        /// </summary>
        /// <param name="stringExpr">String Expression</param>
        /// <param name="endsExpr">Argument Expression</param>
        public StrEndsFunction(ISparqlExpression stringExpr, ISparqlExpression endsExpr)
            : base(stringExpr, endsExpr) { }

        /// <summary>
        /// Determines whether the given String Literal ends with the given Argument Literal
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <param name="argLit">Argument Literal</param>
        /// <returns></returns>
        protected override bool ValueInternal(ILiteralNode stringLit, ILiteralNode argLit)
        {
            return stringLit.Value.EndsWith(argLit.Value);
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get 
            {
                return SparqlSpecsHelper.SparqlKeywordStrEnds; 
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordStrEnds + "(" + this._leftExpr.ToString() + ", " + this._rightExpr.ToString() + ")";
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new StrEndsFunction(transformer.Transform(this._leftExpr), transformer.Transform(this._rightExpr));
        }
    }

    /// <summary>
    /// Represents the SPARQL STRLEN Function
    /// </summary>
    public class StrLenFunction : BaseUnaryXPathStringFunction
    {
        /// <summary>
        /// Creates a new STRLEN() function
        /// </summary>
        /// <param name="expr">Argument Expression</param>
        public StrLenFunction(ISparqlExpression expr)
            : base(expr) { }

        /// <summary>
        /// Determines the Length of the given String Literal
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <returns></returns>
        protected override INode ValueInternal(ILiteralNode stringLit)
        {
            return new LiteralNode(null, stringLit.Value.Length.ToString(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeInteger));
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get 
            {
                return SparqlSpecsHelper.SparqlKeywordStrLen; 
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordStrLen + "(" + this._expr.ToString() + ")";
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new StrLenFunction(transformer.Transform(this._expr));
        }
    }

    /// <summary>
    /// Represents the SPARQL STRSTARTS Function
    /// </summary>
    public class StrStartsFunction : BaseBinarySparqlStringFunction
    {
        /// <summary>
        /// Creates a new STRSTARTS() function
        /// </summary>
        /// <param name="stringExpr">String Expression</param>
        /// <param name="startsExpr">Argument Expression</param>
        public StrStartsFunction(ISparqlExpression stringExpr, ISparqlExpression startsExpr)
            : base(stringExpr, startsExpr) { }

        /// <summary>
        /// Determines whether the given String Literal starts with the given Argument Literal
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <param name="argLit">Argument Literal</param>
        /// <returns></returns>
        protected override bool ValueInternal(ILiteralNode stringLit, ILiteralNode argLit)
        {
            return stringLit.Value.StartsWith(argLit.Value);
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordStrStarts;
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordStrStarts + "(" + this._leftExpr.ToString() + ", " + this._rightExpr.ToString() + ")";
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new StrStartsFunction(transformer.Transform(this._leftExpr), transformer.Transform(this._rightExpr));
        }
    }

    /// <summary>
    /// Represents the SPARQL SUBSTR Function
    /// </summary>
    public class SubStrFunction : ISparqlExpression
    {
        private ISparqlExpression _expr, _start, _length;

        /// <summary>
        /// Creates a new XPath Substring function
        /// </summary>
        /// <param name="stringExpr">Expression</param>
        /// <param name="startExpr">Start</param>
        public SubStrFunction(ISparqlExpression stringExpr, ISparqlExpression startExpr)
            : this(stringExpr, startExpr, null) { }

        /// <summary>
        /// Creates a new XPath Substring function
        /// </summary>
        /// <param name="stringExpr">Expression</param>
        /// <param name="startExpr">Start</param>
        /// <param name="lengthExpr">Length</param>
        public SubStrFunction(ISparqlExpression stringExpr, ISparqlExpression startExpr, ISparqlExpression lengthExpr)
        {
            this._expr = stringExpr;
            this._start = startExpr;
            this._length = lengthExpr;
        }

        /// <summary>
        /// Returns the value of the Expression as evaluated for a given Binding as a Literal Node
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public INode Value(SparqlEvaluationContext context, int bindingID)
        {
            ILiteralNode input = this.CheckArgument(this._expr, context, bindingID);
            ILiteralNode start = this.CheckArgument(this._start, context, bindingID, XPathFunctionFactory.AcceptNumericArguments);

            if (this._length != null)
            {
                ILiteralNode length = this.CheckArgument(this._length, context, bindingID, XPathFunctionFactory.AcceptNumericArguments);

                if (input.Value.Equals(String.Empty)) return new LiteralNode(null, String.Empty, new Uri(XmlSpecsHelper.XmlSchemaDataTypeString));

                try
                {
                    int s = Convert.ToInt32(start.Value);
                    int l = Convert.ToInt32(length.Value);

                    if (s < 1) s = 1;
                    if (l < 1)
                    {
                        //If no/negative characters are being selected the empty string is returned
                        if (input.DataType != null)
                        {
                            return new LiteralNode(null, String.Empty, input.DataType);
                        }
                        else 
                        {
                            return new LiteralNode(null, String.Empty, input.Language);
                        }
                    }
                    else if ((s-1) > input.Value.Length)
                    {
                        //If the start is after the end of the string the empty string is returned
                        if (input.DataType != null)
                        {
                            return new LiteralNode(null, String.Empty, input.DataType);
                        }
                        else 
                        {
                            return new LiteralNode(null, String.Empty, input.Language);
                        }                    }
                    else
                    {
                        if (((s - 1) + l) > input.Value.Length)
                        {
                            //If the start plus the length is greater than the length of the string the string from the starts onwards is returned
                            if (input.DataType != null)
                            {
                                return new LiteralNode(null, input.Value.Substring(s - 1), input.DataType);
                            } 
                            else
                            {
                                return new LiteralNode(null, input.Value.Substring(s - 1), input.Language);
                            }
                        }
                        else
                        {
                            //Otherwise do normal substring
                            if (input.DataType != null)
                            {
                                return new LiteralNode(null, input.Value.Substring(s - 1, l), input.DataType);
                            }
                            else
                            {
                                return new LiteralNode(null, input.Value.Substring(s - 1, l), input.Language);
                            }
                        }
                    }
                }
                catch
                {
                    throw new RdfQueryException("Unable to convert the Start/Length argument to an Integer");
                }
            }
            else
            {
                if (input.Value.Equals(String.Empty)) return new LiteralNode(null, String.Empty, new Uri(XmlSpecsHelper.XmlSchemaDataTypeString));

                try
                {
                    int s = Convert.ToInt32(start.Value);
                    if (s < 1) s = 1;

                    if (input.DataType != null)
                    {
                        return new LiteralNode(null, input.Value.Substring(s - 1), input.DataType);
                    }
                    else
                    {
                        return new LiteralNode(null, input.Value.Substring(s - 1), input.Language);
                    }
                }
                catch
                {
                    throw new RdfQueryException("Unable to convert the Start argument to an Integer");
                }
            }
        }

        private ILiteralNode CheckArgument(ISparqlExpression expr, SparqlEvaluationContext context, int bindingID)
        {
            return this.CheckArgument(expr, context, bindingID, XPathFunctionFactory.AcceptStringArguments);
        }

        private ILiteralNode CheckArgument(ISparqlExpression expr, SparqlEvaluationContext context, int bindingID, Func<Uri, bool> argumentTypeValidator)
        {
            INode temp = expr.Value(context, bindingID);
            if (temp != null)
            {
                if (temp.NodeType == NodeType.Literal)
                {
                    ILiteralNode lit = (ILiteralNode)temp;
                    if (lit.DataType != null)
                    {
                        if (argumentTypeValidator(lit.DataType))
                        {
                            //Appropriately typed literals are fine
                            return lit;
                        }
                        else
                        {
                            throw new RdfQueryException("Unable to evaluate a substring as one of the argument expressions returned a typed literal with an invalid type");
                        }
                    }
                    else if (argumentTypeValidator(new Uri(XmlSpecsHelper.XmlSchemaDataTypeString)))
                    {
                        //Untyped Literals are treated as Strings and may be returned when the argument allows strings
                        return lit;
                    }
                    else
                    {
                        throw new RdfQueryException("Unable to evalaute a substring as one of the argument expressions returned an untyped literal");
                    }
                }
                else
                {
                    throw new RdfQueryException("Unable to evaluate a substring as one of the argument expressions returned a non-literal");
                }
            }
            else
            {
                throw new RdfQueryException("Unable to evaluate a substring as one of the argument expressions evaluated to null");
            }
        }

        /// <summary>
        /// Returns the Effective Boolean Value of the Expression as evaluated for a given Binding as a Literal Node
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public bool EffectiveBooleanValue(SparqlEvaluationContext context, int bindingID)
        {
            return SparqlSpecsHelper.EffectiveBooleanValue(this.Value(context, bindingID));
        }

        /// <summary>
        /// Gets the Variables used in the function
        /// </summary>
        public IEnumerable<string> Variables
        {
            get
            {
                if (this._length != null)
                {
                    return this._expr.Variables.Concat(this._start.Variables).Concat(this._length.Variables);
                }
                else
                {
                    return this._expr.Variables.Concat(this._start.Variables);
                }
            }
        }

        /// <summary>
        /// Gets the String representation of the function
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this._length != null)
            {
                return SparqlSpecsHelper.SparqlKeywordSubStr + "(" + this._expr.ToString() + "," + this._start.ToString() + "," + this._length.ToString() + ")";
            }
            else
            {
                return SparqlSpecsHelper.SparqlKeywordSubStr + "(" + this._expr.ToString() + "," + this._start.ToString() + ")";
            }
        }

        /// <summary>
        /// Gets the Type of the Expression
        /// </summary>
        public SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.Function;
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordSubStr;
            }
        }

        /// <summary>
        /// Gets the Arguments of the Function
        /// </summary>
        public IEnumerable<ISparqlExpression> Arguments
        {
            get
            {
                if (this._length != null)
                {
                    return new ISparqlExpression[] { this._expr, this._start, this._length };
                }
                else
                {
                    return new ISparqlExpression[] { this._expr, this._start };
                }
            }
        }

        public ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            if (this._length != null)
            {
                return new SubStrFunction(transformer.Transform(this._expr), transformer.Transform(this._start), transformer.Transform(this._length));
            }
            else
            {
                return new SubStrFunction(transformer.Transform(this._expr), transformer.Transform(this._start));
            }
        }
    }

    /// <summary>
    /// Represents the SPARQL UCASE Function
    /// </summary>
    public class UCaseFunction : BaseUnaryXPathStringFunction
    {
        /// <summary>
        /// Creates a new UCASE() function
        /// </summary>
        /// <param name="expr">Argument Expression</param>
        public UCaseFunction(ISparqlExpression expr)
            : base(expr) { }

        /// <summary>
        /// Converts the given String Literal to upper case
        /// </summary>
        /// <param name="stringLit">String Literal</param>
        /// <returns></returns>
        protected override INode ValueInternal(ILiteralNode stringLit)
        {
            if (stringLit.DataType != null)
            {
                return new LiteralNode(null, stringLit.Value.ToUpper(), stringLit.DataType);
            }
            else
            {
                return new LiteralNode(null, stringLit.Value.ToUpper(), stringLit.Language);
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return SparqlSpecsHelper.SparqlKeywordUCase; 
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SparqlSpecsHelper.SparqlKeywordUCase + "(" + this._expr.ToString() + ")";
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new UCaseFunction(transformer.Transform(this._expr));
        }

    }
}
