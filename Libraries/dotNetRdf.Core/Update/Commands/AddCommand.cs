/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2023 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

using System;

namespace VDS.RDF.Update.Commands
{
    /// <summary>
    /// Represents the SPARQL Update ADD Command.
    /// </summary>
    public class AddCommand 
        : BaseTransferCommand
    {
        /// <summary>
        /// Creates a command which merges the data from the source graph into the destination graph.
        /// </summary>
        /// <param name="sourceGraphName">Name of the source graph.</param>
        /// <param name="destinationGraphName">Name of the destination graph.</param>
        /// <param name="silent">Whether errors should be suppressed.</param>
        public AddCommand(IRefNode sourceGraphName, IRefNode destinationGraphName, bool silent = false):base(SparqlUpdateCommandType.Add, sourceGraphName, destinationGraphName, silent){}

        /// <summary>
        /// Creates a Command which merges the data from the Source Graph into the Destination Graph.
        /// </summary>
        /// <param name="sourceUri">Source Graph URI.</param>
        /// <param name="destUri">Destination Graph URI.</param>
        /// <param name="silent">Whether errors should be suppressed.</param>
        [Obsolete("Replaced by AddCommand(IRefNode, IRefNode, bool)")]
        public AddCommand(Uri sourceUri, Uri destUri, bool silent)
            : base(SparqlUpdateCommandType.Add, sourceUri, destUri, silent) { }

        /// <summary>
        /// Creates a Command which merges the data from the Source Graph into the Destination Graph.
        /// </summary>
        /// <param name="sourceUri">Source Graph URI.</param>
        /// <param name="destUri">Destination Graph URI.</param>
        [Obsolete("Replaced by AddCommand(IRefNode, IRefNode, bool)")]
        public AddCommand(Uri sourceUri, Uri destUri)
            : base(SparqlUpdateCommandType.Add, sourceUri, destUri) { }


        /// <summary>
        /// Processes the Command using the given Update Processor.
        /// </summary>
        /// <param name="processor">SPARQL Update Processor.</param>
        public override void Process(ISparqlUpdateProcessor processor)
        {
            processor.ProcessAddCommand(this);
        }
    }
}
