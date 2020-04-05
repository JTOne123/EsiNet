﻿using System;
using System.Collections.Generic;
using System.Linq;
using EsiNet.Fragments;
using EsiNet.Fragments.Text;
using EsiNet.Pipeline;

namespace EsiNet
{
    public class EsiFragmentParser
    {
        private readonly IReadOnlyDictionary<string, IEsiFragmentParser> _parsers;
        private readonly IReadOnlyCollection<IFragmentParsePipeline> _pipelines;

        public EsiFragmentParser(
            IReadOnlyDictionary<string, IEsiFragmentParser> parsers,
            IEnumerable<IFragmentParsePipeline> pipelines)
        {
            _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
            _pipelines = pipelines?.Reverse().ToArray() ?? throw new ArgumentNullException(nameof(pipelines));
        }

        public IEsiFragment Parse(
            string tag, IReadOnlyDictionary<string, string> attributes, string tagBody, string outerBody)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            if (tagBody == null) throw new ArgumentNullException(nameof(tagBody));
            if (outerBody == null) throw new ArgumentNullException(nameof(outerBody));

            if (!_parsers.TryGetValue(tag, out var parser))
            {
                return new EsiTextFragment(outerBody);
            }

            IEsiFragment Parse(IReadOnlyDictionary<string, string> a, string b) => parser.Parse(a, b);

            return _pipelines
                .Aggregate(
                    (ParseDelegate) Parse,
                    (next, pipeline) => (a, b) => pipeline.Handle(a, b, next))(attributes, tagBody);
        }
    }
}