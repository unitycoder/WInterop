﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using WInterop.Support;
using Xunit;

namespace Tests.Support
{
    public class EnvironmentTests
    {
        [Fact]
        public void IsWindowsStore()
        {
            bool isWindowsStore = Environment.IsWindowsStoreApplication();
#if WINRT
            isWindowsStore.Should().BeTrue();
#else
            isWindowsStore.Should().BeFalse();
#endif
        }
    }
}
