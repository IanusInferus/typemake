using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public static class TopologicalSort
    {
        public static IEnumerable<TSource> PartialOrderBy<TSource>(this IEnumerable<TSource> Source, Func<TSource, IEnumerable<TSource>> PreconditionSelector)
        {
            var l = new List<TSource>();
            var TemporaryMark = new HashSet<TSource>();
            var PermanentMark = new HashSet<TSource>();

            void visit(TSource n)
            {
                if (PermanentMark.Contains(n)) { return; }
                if (TemporaryMark.Contains(n)) { throw new ArgumentException("Cyclic"); }
                TemporaryMark.Add(n);
                var Precondition = PreconditionSelector(n);
                if (Precondition != null)
                {
                    foreach (var m in Precondition)
                    {
                        visit(m);
                    }
                }
                PermanentMark.Add(n);
                l.Add(n);
            }

            foreach (var n in Source)
            {
                if (TemporaryMark.Contains(n) || PermanentMark.Contains(n)) { continue; }
                visit(n);
            }

            return l;
        }
    }
}
