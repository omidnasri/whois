﻿using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tokens.Transformers;
using Tokens.Validators;
using Whois.Logging;
using Whois.Models;
using Whois.Visitors;

namespace Whois
{
    /// <summary>
    /// Looks up WHOIS information
    /// </summary>
    public class WhoisLookup : IWhoisLookup
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private PatternExtractorVisitor patternExtractorVisitor;

        public WhoisOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the visitors.
        /// </summary>
        /// <value>The visitors.</value>
        public IList<IWhoisVisitor> Visitors { get; }

        public WhoisLookup() : this(WhoisOptions.Defaults)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoisLookup"/> class.
        /// </summary>
        public WhoisLookup(WhoisOptions options) 
        {
            patternExtractorVisitor = new PatternExtractorVisitor();

            Options = options;

            Visitors = new List<IWhoisVisitor>
            {
                // Validate input
                new ValidateVisitor(),

                // Get initial WHOIS server URL
                new WhoisServerVisitor(),

                // Download from initial server
                new DownloadVisitor(),

                // Check to see if a secondard WHOIS server needs to be queried
                new RedirectVisitor(),

                // Populate Structured WHOIS object
                patternExtractorVisitor 
            };
        }

        public WhoisResponse Lookup(string domain)
        {
            return AsyncHelper.RunSync(() => LookupAsync(domain));
        }

        public WhoisResponse Lookup(string domain, Encoding encoding)
        {
            return AsyncHelper.RunSync(() => LookupAsync(domain, encoding));
        }

        public async Task<WhoisResponse> LookupAsync(string domain)
        {
            return await LookupAsync(domain, Options.DefaultEncoding);
        }

        public async Task<WhoisResponse> LookupAsync(string domain, Encoding encoding)
        {
            Log.Debug("Looking up WHOIS response for: {0}", domain);

            var state = new LookupState
            {
                Options = Options.Clone(),
                Domain = domain
            };

            state.Options.DefaultEncoding = encoding;

            foreach (var visitor in Visitors)
            {
                state = await visitor.Visit(state);
            }

            return state.Response;
        }

        public void AddPattern(string content, string name)
        {
            patternExtractorVisitor.AddPattern(content, name);
        }

        public void ClearPatterns()
        {
            patternExtractorVisitor.ClearPatterns();
        }

        public void RegisterPatternTransformer<T>() where T : ITokenTransformer
        {
            patternExtractorVisitor.RegisterPatternTransformer<T>();
        }

        public void RegisterPatternValidator<T>() where T : ITokenValidator
        {
            patternExtractorVisitor.RegisterPatternValidator<T>();
        }
    }
}