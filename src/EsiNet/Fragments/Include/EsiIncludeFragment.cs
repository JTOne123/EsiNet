﻿using System;

namespace EsiNet.Fragments.Include
{
    [Serializable]
    public class EsiIncludeFragment : IEsiFragment
    {
        public EsiIncludeFragment(Uri uri)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public Uri Uri { get; }
    }
}