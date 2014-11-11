using NLog;

namespace Alanta.Common
{
#if SILVERLIGHT
    public class ServerLogger
    {
        private static readonly Logger logger = LogManager.GetLogger("ServerLogger");// it thread safety by documentation

        public static Logger Instance
        {
            get
            {
                return logger;
            }
        }
    }
#endif
}
