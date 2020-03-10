using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Database
{
    public class AsyncUtils
    {
        public static TResult RunSync<TResult>(Func<Task<TResult>> task) => Task.Run(async () => await task()).Result;
    }
}
