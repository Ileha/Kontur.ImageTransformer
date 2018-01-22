using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Exceptions
{
    public class StatusCode : Exception {
        private HttpStatusCode _status_code;
        private bool is_fatal;
        
        public HttpStatusCode GetStatusCode {
            get { return _status_code; }
        }
        public int ToInt {
            get { return (int)_status_code; }
        }
        public bool IsFatal {
            get { return is_fatal; }
        }

        public StatusCode(HttpStatusCode StatusCode, bool is_fatal) {
            _status_code = StatusCode;
            this.is_fatal = is_fatal;
        }
    }
}
