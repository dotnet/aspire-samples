using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.Context.Propagation;

namespace AspireShop.ServiceDefaults
{
    public static class  SharedActivitySource
    {
        internal static readonly AssemblyName AssemblyName = typeof(SharedActivitySource).Assembly.GetName();
        public static readonly string ActivitySourceName = AssemblyName.Name!;
        public static readonly Version Version = AssemblyName.Version!;
        public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
        public static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    }
}
