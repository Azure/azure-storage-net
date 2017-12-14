using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    public static class GCWatcher
    {
        // NOTE: Be careful with Strings due to interning and MarshalByRefObject proxy objects
        private readonly static ConditionalWeakTable<Object, NotifyWhenGCd> s_cwt =
           new ConditionalWeakTable<Object, NotifyWhenGCd>();

        private sealed class NotifyWhenGCd
        {
            private Object m_object;

            internal NotifyWhenGCd(Object obj) { m_object = obj; }
            public override string ToString()
            {
                return String.Format("GC'd a {0} object (ToString={1})",
                   m_object.GetType(), m_object);
            }
            ~NotifyWhenGCd() { Console.WriteLine(this); }
        }

        public static T GCWatch<T>(this T obj) where T : class
        {
            s_cwt.Add(obj, new NotifyWhenGCd(obj));
            return obj;
        }

    }
}
