﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceBus.Management.AcceptanceTests
{
    using NServiceBus.AcceptanceTesting.Customization;
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    public class NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetUp()
        {
            Conventions.EndpointNamingConvention = t =>
            {
                var baseNs = typeof(NServiceBusAcceptanceTest).Namespace;
                var testName = GetType().Name;
                return t.FullName.Replace(baseNs + ".", "").Replace(testName + "+", "");
            };
        }
    }
}