using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region Fields
        int _count = 0;
        #endregion

        /// <summary>
        /// Make me one.
        /// </summary>
        /// <param name="config"></param>
        public NullComm()
        {
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
        }

        public OpStatus Init(string config, IProgress<string> progress)
        {
            return OpStatus.Success;//, $"NullComm inited at {DateTime.Now}");
        }

        public OpStatus Run()
        {
            throw new NotImplementedException();
        }

        OpStatus IComm.Send(string req)
        {
            throw new NotImplementedException();
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public (OpStatus stat, string msg, string resp) Send(string req)
        {
            OpStatus stat = OpStatus.Success;
            string msg = "Nothing to say";
            string resp = $"NullComm receive {_count}";
            _count += 11;

            // Fake blocking.
            Thread.Sleep(1000);
            return (stat, msg, resp);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }
    }
}
