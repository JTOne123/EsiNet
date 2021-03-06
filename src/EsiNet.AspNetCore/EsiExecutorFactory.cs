﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EsiNet.Caching;
using EsiNet.Fragments;
using EsiNet.Fragments.Choose;
using EsiNet.Fragments.Composite;
using EsiNet.Fragments.Ignore;
using EsiNet.Fragments.Include;
using EsiNet.Fragments.Text;
using EsiNet.Fragments.Try;
using EsiNet.Fragments.Vars;
using EsiNet.Http;
using EsiNet.Logging;

namespace EsiNet.AspNetCore
{
    public static class EsiExecutorFactory
    {
        public static EsiFragmentExecutor Create(
            EsiFragmentCacheFacade cache,
            IHttpLoader httpLoader,
            EsiBodyParser parser,
            Log log,
            ServiceFactory serviceFactory,
            IncludeUriParser includeUriParser)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (httpLoader == null) throw new ArgumentNullException(nameof(httpLoader));
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));
            if (includeUriParser == null) throw new ArgumentNullException(nameof(includeUriParser));

            var executors = new Dictionary<Type, Func<IEsiFragment, EsiExecutionContext, Task<IEnumerable<string>>>>();

            var fragmentExecutor = new EsiFragmentExecutor(executors, serviceFactory);
            var includeExecutor = new EsiIncludeFragmentExecutor(includeUriParser, cache, httpLoader, parser, fragmentExecutor);
            var ignoreExecutor = new EsiIgnoreFragmentExecutor();
            var textExecutor = new EsiTextFragmentExecutor();
            var compositeExecutor = new EsiCompositeFragmentExecutor(fragmentExecutor);
            var tryExecutor = new EsiTryFragmentExecutor(fragmentExecutor, log);
            var chooseExecutor = new EsiChooseFragmentExecutor(fragmentExecutor);
            var varsExecutor = new EsiVarsFragmentExecutor();

            executors[typeof(EsiIncludeFragment)] = (f, ec) => includeExecutor.Execute((EsiIncludeFragment) f, ec);
            executors[typeof(EsiIgnoreFragment)] = (f, ec) => ignoreExecutor.Execute((EsiIgnoreFragment) f, ec);
            executors[typeof(EsiTextFragment)] = (f, ec) => textExecutor.Execute((EsiTextFragment) f, ec);
            executors[typeof(EsiCompositeFragment)] = (f, ec) => compositeExecutor.Execute((EsiCompositeFragment) f, ec);
            executors[typeof(EsiTryFragment)] = (f, ec) => tryExecutor.Execute((EsiTryFragment) f, ec);
            executors[typeof(EsiChooseFragment)] = (f, ec) => chooseExecutor.Execute((EsiChooseFragment) f, ec);
            executors[typeof(EsiVarsFragment)] = (f, ec) => varsExecutor.Execute((EsiVarsFragment) f, ec);

            return fragmentExecutor;
        }
    }
}