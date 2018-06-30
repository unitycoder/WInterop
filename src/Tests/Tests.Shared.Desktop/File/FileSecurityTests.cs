﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Tests.Support;
using WInterop.Authorization;
using WInterop.Authorization.Types;
using WInterop.Storage;
using WInterop.Storage.Types;
using WInterop.SystemInformation;
using Xunit;

namespace DesktopTests.File
{
    public class FileSecurityTests
    {
        [Fact]
        public void GetSidForCreatedFile()
        {
            using (var cleaner = new TestFileCleaner())
            {
                using (var handle = StorageMethods.CreateFile(cleaner.GetTestPath(), CreationDisposition.CreateNew))
                {
                    handle.IsInvalid.Should().BeFalse();
                    SID sid = StorageMethods.QueryOwner(handle);
                    sid.IdentifierAuthority.Should().Be(IdentifierAuthority.NT);
                    AccountSidInformation info = AuthorizationMethods.LookupAccountSidLocal(sid);
                    info.Usage.Should().Be(SidNameUse.User);
                    info.Name.Should().Be(SystemInformationMethods.GetUserName());
                }
            }
        }
    }
}