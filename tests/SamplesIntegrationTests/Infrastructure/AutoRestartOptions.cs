// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SamplesIntegrationTests.Infrastructure;

internal class AutoRestartOptions
{
    public List<string> LogMessages { get; set; } = [];

    public int Attempts { get; set; }
}
